using System.Composition;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.Telemetry.SourceGenerator.Refactorings;

[ExportCodeRefactoringProvider(
	LanguageNames.CSharp,
	Name = nameof(ConvertILoggerToTelemetryRefactoringProvider)
)]
[Shared]
public sealed class ConvertILoggerToTelemetryRefactoringProvider : CodeRefactoringProvider
{
	// Matches structured-logging template placeholders: {Name}, {@Name} (destructure), {$Name} (stringify).
	// The @/$ prefixes follow Serilog conventions and are also used in Microsoft.Extensions.Logging
	// structured logging. Alignment and format specifiers are recognised but stripped.
	static readonly Regex TemplatePlaceholderRegex = new(
		@"\{(?:@|\$)?(?<name>[A-Za-z_]\w*)(?:,[-\d]+)?(?::[^}]+)?\}",
		RegexOptions.Compiled | RegexOptions.ExplicitCapture
	);

	static readonly Dictionary<string, string> MethodToAttribute = new(StringComparer.Ordinal)
	{
		["LogTrace"] = "Trace",
		["LogDebug"] = "Debug",
		["LogInformation"] = "Info",
		["LogWarning"] = "Warning",
		["LogError"] = "Error",
		["LogCritical"] = "Critical",
	};

	// Uses keyword aliases (string, int, bool) and short type names without global:: prefix.
	// Suitable for generated interface code that lives in the same file as the class.
	static readonly SymbolDisplayFormat ParamTypeFormat = new(
		typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
		miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
	);

	public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
	{
		var root = await context
			.Document.GetSyntaxRootAsync(context.CancellationToken)
			.ConfigureAwait(false);
		if (root is null)
			return;

		var node = root.FindNode(context.Span);
		var classDecl = node.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
		if (classDecl is null)
			return;

		var semanticModel = await context
			.Document.GetSemanticModelAsync(context.CancellationToken)
			.ConfigureAwait(false);
		if (semanticModel is null)
			return;

		var loggerFields = FindILoggerFields(classDecl, semanticModel, context.CancellationToken);
		if (loggerFields.Count == 0)
			return;

		var logCalls = FindLogCalls(
			classDecl,
			loggerFields,
			semanticModel,
			context.CancellationToken
		);
		if (logCalls.Count == 0)
			return;

		context.RegisterRefactoring(
			CodeAction.Create(
				"Convert ILogger usage to Purview Telemetry interface",
				ct =>
					ConvertAsync(
						context.Document,
						classDecl,
						loggerFields,
						logCalls,
						semanticModel,
						ct
					),
				equivalenceKey: "Purview.Telemetry.ConvertILoggerToTelemetry"
			)
		);
	}

	static async Task<Document> ConvertAsync(
		Document document,
		ClassDeclarationSyntax classDecl,
		List<ILoggerFieldInfo> loggerFields,
		List<LogCallInfo> logCalls,
		SemanticModel semanticModel,
		CancellationToken cancellationToken
	)
	{
		var className = classDecl.Identifier.ValueText;
		var interfaceName = "I" + className + "Logger";

		var callsWithMethods = AssignMethodNames(logCalls);

		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is null)
			return document;

		var interfaceCode = BuildInterfaceCode(
			interfaceName,
			callsWithMethods,
			GetNamespaceOf(classDecl)
		);
		var interfaceSyntax = ParseInterfaceNode(interfaceCode);

		var newClassDecl = RewriteClass(
			classDecl,
			loggerFields,
			callsWithMethods,
			interfaceName,
			semanticModel
		);

		// Replace class in the tree first, then insert interface before it.
		var newRoot = root.ReplaceNode(classDecl, newClassDecl);
		var replacedClass = newRoot
			.DescendantNodes()
			.OfType<ClassDeclarationSyntax>()
			.First(c => c.Identifier.ValueText == className);

		newRoot = newRoot.InsertNodesBefore(
			replacedClass,
			[interfaceSyntax.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)]
		);

		return document.WithSyntaxRoot(newRoot);
	}

	// -------------------------------------------------------------------------
	// Finding fields
	// -------------------------------------------------------------------------

	static List<ILoggerFieldInfo> FindILoggerFields(
		ClassDeclarationSyntax classDecl,
		SemanticModel semanticModel,
		CancellationToken cancellationToken
	)
	{
		var iLoggerOpen = semanticModel.Compilation.GetTypeByMetadataName(
			Constants.Logging.MicrosoftExtensions.ILoggerOfTMetadataName
		);
		var iLoggerNonGeneric = semanticModel.Compilation.GetTypeByMetadataName(
			Constants.Logging.MicrosoftExtensions.ILogger.FullyQualifiedName
		);

		var result = new List<ILoggerFieldInfo>();

		foreach (var member in classDecl.Members)
		{
			if (member is not FieldDeclarationSyntax fieldDecl)
				continue;

			foreach (var variable in fieldDecl.Declaration.Variables)
			{
				cancellationToken.ThrowIfCancellationRequested();

				if (
					semanticModel.GetDeclaredSymbol(variable, cancellationToken)
					is not IFieldSymbol fieldSymbol
				)
					continue;

				if (fieldSymbol.Type is not INamedTypeSymbol namedType)
					continue;

				bool isLogger =
					(
						namedType.IsGenericType
						&& iLoggerOpen is not null
						&& SymbolEqualityComparer.Default.Equals(
							namedType.ConstructedFrom,
							iLoggerOpen
						)
					)
					|| (
						iLoggerNonGeneric is not null
						&& SymbolEqualityComparer.Default.Equals(namedType, iLoggerNonGeneric)
					);

				if (!isLogger)
					continue;

				result.Add(
					new ILoggerFieldInfo(
						FieldName: fieldSymbol.Name,
						FieldDeclaration: fieldDecl,
						FieldSymbol: fieldSymbol
					)
				);
			}
		}

		return result;
	}

	// -------------------------------------------------------------------------
	// Finding log calls
	// -------------------------------------------------------------------------

	static List<LogCallInfo> FindLogCalls(
		ClassDeclarationSyntax classDecl,
		List<ILoggerFieldInfo> loggerFields,
		SemanticModel semanticModel,
		CancellationToken cancellationToken
	)
	{
		var loggerFieldNames = new HashSet<string>(
			loggerFields.Select(f => f.FieldName),
			StringComparer.Ordinal
		);

		var result = new List<LogCallInfo>();

		foreach (var invocation in classDecl.DescendantNodes().OfType<InvocationExpressionSyntax>())
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
				continue;

			// Receiver must be one of our logger fields
			var receiverIdentifier = GetSimpleIdentifier(memberAccess.Expression);
			if (receiverIdentifier is null || !loggerFieldNames.Contains(receiverIdentifier))
				continue;

			var methodName = memberAccess.Name.Identifier.Text;
			if (methodName != "Log" && !MethodToAttribute.ContainsKey(methodName))
				continue;

			var call = AnalyzeLogCall(invocation, methodName, semanticModel, cancellationToken);
			if (call is not null)
				result.Add(call);
		}

		return result;
	}

	static string? GetSimpleIdentifier(ExpressionSyntax expression) =>
		expression switch
		{
			IdentifierNameSyntax id => id.Identifier.Text,
			MemberAccessExpressionSyntax _ => null, // chained access – not a field reference
			_ => null,
		};

	static LogCallInfo? AnalyzeLogCall(
		InvocationExpressionSyntax invocation,
		string methodName,
		SemanticModel semanticModel,
		CancellationToken cancellationToken
	)
	{
		var args = invocation.ArgumentList.Arguments;
		if (args.Count == 0)
			return null;

		int idx = 0;
		string? explicitLogLevel = null;

		// For generic Log(LogLevel, ...) method, first arg is the level
		if (methodName == "Log")
		{
			if (idx >= args.Count)
				return null;

			explicitLogLevel = TryExtractLogLevel(args[idx], semanticModel);
			if (explicitLogLevel is null)
				return null;

			idx++;
		}

		// Optional EventId (skip)
		if (idx < args.Count && IsEventIdType(args[idx], semanticModel))
			idx++;

		// Optional Exception
		ExpressionSyntax? exceptionExpression = null;
		if (idx < args.Count && IsExceptionType(args[idx], semanticModel))
		{
			exceptionExpression = args[idx].Expression;
			idx++;
		}

		// Message template (required)
		if (idx >= args.Count)
			return null;

		string? template = null;
		if (args[idx].Expression is LiteralExpressionSyntax literal)
			template = literal.Token.ValueText;

		idx++;

		// Template arguments
		var templateArgs = new List<ExpressionSyntax>();
		while (idx < args.Count)
		{
			templateArgs.Add(args[idx].Expression);
			idx++;
		}

		// Match template placeholders to arguments
		var placeholders = ExtractPlaceholders(template);
		var parameters = new List<LogParameterInfo>();

		if (exceptionExpression is not null)
		{
			var exType = semanticModel.Compilation.GetTypeByMetadataName(
				Constants.System.Exception.FullyQualifiedName
			);
			var exTypeStr =
				exType?.ToDisplayString(ParamTypeFormat)
				?? Constants.System.Exception.FullyQualifiedName;
			parameters.Add(new LogParameterInfo("exception", exTypeStr, exceptionExpression));
		}

		for (var i = 0; i < Math.Min(placeholders.Count, templateArgs.Count); i++)
		{
			var (_, typeStr) = GetNaturalType(
				semanticModel.GetTypeInfo(templateArgs[i], cancellationToken)
			);

			var rawName = placeholders[i];
			var paramName = ToCamelCase(rawName);

			parameters.Add(new LogParameterInfo(paramName, typeStr, templateArgs[i]));
		}

		// If there are extra args without template placeholders, include them
		for (var i = placeholders.Count; i < templateArgs.Count; i++)
		{
			var (_, typeStr) = GetNaturalType(
				semanticModel.GetTypeInfo(templateArgs[i], cancellationToken)
			);

			parameters.Add(new LogParameterInfo($"arg{i}", typeStr, templateArgs[i]));
		}

		return new LogCallInfo(
			Invocation: invocation,
			ILoggerMethodName: methodName,
			ExplicitLogLevel: explicitLogLevel,
			MessageTemplate: template,
			Parameters: parameters,
			ExceptionExpression: exceptionExpression
		);
	}

	static bool IsEventIdType(ArgumentSyntax arg, SemanticModel semanticModel)
	{
		var typeInfo = semanticModel.GetTypeInfo(arg.Expression);
		var type = typeInfo.ConvertedType ?? typeInfo.Type;
		return type is not null
			&& (
				type.ToDisplayString() == Constants.Logging.MicrosoftExtensions.EventId.FullyQualifiedName
				|| type.SpecialType == SpecialType.System_Int32
			);
	}

	static bool IsExceptionType(ArgumentSyntax arg, SemanticModel semanticModel)
	{
		var typeInfo = semanticModel.GetTypeInfo(arg.Expression);
		var type = typeInfo.ConvertedType ?? typeInfo.Type;
		if (type is null)
			return false;

		var exType = semanticModel.Compilation.GetTypeByMetadataName(
			Constants.System.Exception.FullyQualifiedName
		);
		return exType is not null && IsOrDerivesFrom(type, exType);
	}

	static bool IsOrDerivesFrom(ITypeSymbol type, INamedTypeSymbol baseType)
	{
		var current = type;
		while (current is not null)
		{
			if (SymbolEqualityComparer.Default.Equals(current, baseType))
				return true;

			current = current.BaseType;
		}

		return false;
	}

	static string? TryExtractLogLevel(ArgumentSyntax arg, SemanticModel semanticModel)
	{
		// e.g. LogLevel.Warning  → "Warning"
		if (arg.Expression is MemberAccessExpressionSyntax ma)
		{
			var symbol = semanticModel.GetSymbolInfo(arg.Expression).Symbol;
			if (
				symbol is IFieldSymbol
				{
					ContainingType.Name: "LogLevel",
					ContainingType.ContainingNamespace.Name: "Logging"
				}
			)
				return ma.Name.Identifier.Text;
		}

		return null;
	}

	// -------------------------------------------------------------------------
	// Method name assignment
	// -------------------------------------------------------------------------

	static List<(LogCallInfo Call, string MethodName)> AssignMethodNames(List<LogCallInfo> calls)
	{
		var usedNames = new HashSet<string>(StringComparer.Ordinal);
		var result = new List<(LogCallInfo, string)>(calls.Count);

		foreach (var call in calls)
		{
			var baseName = GetBaseMethodName(call);
			var name = baseName;
			var counter = 2;
			while (!usedNames.Add(name))
				name = baseName + counter++;

			result.Add((call, name));
		}

		return result;
	}

	static string GetBaseMethodName(LogCallInfo call)
	{
		return call.ILoggerMethodName == "Log" && call.ExplicitLogLevel is not null
			? "Log" + call.ExplicitLogLevel
			: call.ILoggerMethodName;
	}

	// -------------------------------------------------------------------------
	// Interface code generation
	// -------------------------------------------------------------------------

	static string BuildInterfaceCode(
		string interfaceName,
		List<(LogCallInfo Call, string MethodName)> callsWithMethods,
		string? ns
	)
	{
		var sb = new StringBuilder();

		sb.AppendLine("using Purview.Telemetry;");
		sb.AppendLine("using Purview.Telemetry.Logging;");
		sb.AppendLine();

		bool hasNs = !string.IsNullOrEmpty(ns);
		if (hasNs)
		{
			sb.AppendLine($"namespace {ns};");
			sb.AppendLine();
		}

		sb.AppendLine("[Logger]");
		sb.AppendLine($"public interface {interfaceName}");
		sb.AppendLine("{");

		var emittedSignatures = new HashSet<string>(StringComparer.Ordinal);

		foreach (var (call, methodName) in callsWithMethods)
		{
			var attribute = GetAttributeFor(call);
			var paramList = BuildParamList(call.Parameters);
			var signature = $"{attribute}|{methodName}|{paramList}";

			if (!emittedSignatures.Add(signature))
				continue;

			sb.AppendLine($"\t[{attribute}]");
			sb.AppendLine($"\tvoid {methodName}({paramList});");
			sb.AppendLine();
		}

		sb.AppendLine("}");

		return sb.ToString();
	}

	static string GetAttributeFor(LogCallInfo call)
	{
		return call.ILoggerMethodName == "Log"
			? call.ExplicitLogLevel is null
				? "Log"
				: call.ExplicitLogLevel switch
				{
					"Trace" => "Trace",
					"Debug" => "Debug",
					"Information" => "Info",
					"Warning" => "Warning",
					"Error" => "Error",
					"Critical" => "Critical",
					_ =>
						$"Log({Constants.Logging.MicrosoftExtensions.LogLevel.ToString(includeGlobal: true)}.{call.ExplicitLogLevel})",
				}
			: MethodToAttribute.TryGetValue(call.ILoggerMethodName, out var attr)
				? attr
				: "Log";
	}

	static string BuildParamList(IReadOnlyList<LogParameterInfo> parameters)
	{
		return parameters.Count == 0
			? string.Empty
			: string.Join(", ", parameters.Select(p => $"{p.TypeDisplayString} {p.Name}"));
	}

	static InterfaceDeclarationSyntax ParseInterfaceNode(string code)
	{
		var tree = CSharpSyntaxTree.ParseText(code);
		var root = tree.GetCompilationUnitRoot();

		// Try to find the interface inside a namespace or at top-level
		var interfaceDecl =
			root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().FirstOrDefault()
			?? throw new InvalidOperationException(
				"Could not parse generated interface from: " + code
			);

		// Return as a standalone declaration with appropriate usings attached as leading trivia
		return interfaceDecl;
	}

	// -------------------------------------------------------------------------
	// Class rewriting
	// -------------------------------------------------------------------------

	static ClassDeclarationSyntax RewriteClass(
		ClassDeclarationSyntax classDecl,
		List<ILoggerFieldInfo> loggerFields,
		List<(LogCallInfo Call, string MethodName)> callsWithMethods,
		string interfaceName,
		SemanticModel semanticModel
	)
	{
		// Build a map from invocation → new invocation
		var invocationMap = new Dictionary<InvocationExpressionSyntax, InvocationExpressionSyntax>(
			SyntaxNodeReferenceComparer<InvocationExpressionSyntax>.Instance
		);

		foreach (var (call, methodName) in callsWithMethods)
		{
			var newInvocation = RewriteInvocation(call, methodName);
			invocationMap[call.Invocation] = newInvocation;
		}

		// Build a map from field declarations → new field declarations
		var fieldMap = new Dictionary<FieldDeclarationSyntax, FieldDeclarationSyntax>(
			SyntaxNodeReferenceComparer<FieldDeclarationSyntax>.Instance
		);
		foreach (var field in loggerFields)
		{
			var newField = RewriteFieldDeclaration(field.FieldDeclaration, interfaceName);
			fieldMap[field.FieldDeclaration] = newField;
		}

		// Also find constructor parameters that take ILogger<T>
		var ctorParamMap = BuildConstructorParamMap(
			classDecl,
			loggerFields,
			interfaceName,
			semanticModel
		);

		// Replace all nodes
		return classDecl.ReplaceNodes(
			invocationMap
				.Keys.Cast<SyntaxNode>()
				.Concat(fieldMap.Keys.Cast<SyntaxNode>())
				.Concat(ctorParamMap.Keys.Cast<SyntaxNode>()),
			(original, _) =>
				original is InvocationExpressionSyntax inv
				&& invocationMap.TryGetValue(inv, out var newInv)
					? newInv
				: original is FieldDeclarationSyntax fld
				&& fieldMap.TryGetValue(fld, out var newFld)
					? newFld
				: original is ParameterSyntax param
				&& ctorParamMap.TryGetValue(param, out var newParam)
					? newParam
				: original
		);
	}

	static InvocationExpressionSyntax RewriteInvocation(LogCallInfo call, string newMethodName)
	{
		// Build new argument list: only the template parameter args (no template string, no EventId)
		var newArgs = SyntaxFactory.SeparatedList(
			call.Parameters.Select(p =>
				SyntaxFactory.Argument(p.ArgumentExpression).WithTriviaFrom(p.ArgumentExpression)
			)
		);

		var memberAccess = (MemberAccessExpressionSyntax)call.Invocation.Expression;

		var newMemberAccess = memberAccess.WithName(SyntaxFactory.IdentifierName(newMethodName));

		return call
			.Invocation.WithExpression(newMemberAccess)
			.WithArgumentList(
				SyntaxFactory.ArgumentList(newArgs).WithTriviaFrom(call.Invocation.ArgumentList)
			);
	}

	static FieldDeclarationSyntax RewriteFieldDeclaration(
		FieldDeclarationSyntax fieldDecl,
		string interfaceName
	)
	{
		var newType = SyntaxFactory
			.IdentifierName(interfaceName)
			.WithTriviaFrom(fieldDecl.Declaration.Type);

		return fieldDecl.WithDeclaration(fieldDecl.Declaration.WithType(newType));
	}

	static Dictionary<ParameterSyntax, ParameterSyntax> BuildConstructorParamMap(
		ClassDeclarationSyntax classDecl,
		List<ILoggerFieldInfo> loggerFields,
		string interfaceName,
		SemanticModel semanticModel
	)
	{
		var result = new Dictionary<ParameterSyntax, ParameterSyntax>(
			SyntaxNodeReferenceComparer<ParameterSyntax>.Instance
		);

		// Match constructor parameters by the exact types of the identified logger fields.
		// This is more precise than re-detecting all ILogger variants from scratch.
		var loggerFieldTypes = new HashSet<ITypeSymbol>(
			loggerFields.Select(f => f.FieldSymbol.Type),
			SymbolEqualityComparer.Default
		);

		foreach (var ctor in classDecl.Members.OfType<ConstructorDeclarationSyntax>())
		{
			foreach (var param in ctor.ParameterList.Parameters)
			{
				if (param.Type is null)
					continue;

				var typeInfo = semanticModel.GetTypeInfo(param.Type);
				var type = typeInfo.Type;
				if (type is null || !loggerFieldTypes.Contains(type))
					continue;

				result[param] = param.WithType(
					SyntaxFactory.IdentifierName(interfaceName).WithTriviaFrom(param.Type)
				);
			}
		}

		return result;
	}

	// -------------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------------

	static List<string> ExtractPlaceholders(string? template)
	{
		if (string.IsNullOrEmpty(template))
			return [];

		var result = new List<string>();
		foreach (Match match in TemplatePlaceholderRegex.Matches(template))
		{
			var name = match.Groups["name"].Value;
			if (!string.IsNullOrEmpty(name))
				result.Add(name);
		}

		return result;
	}

	static string ToCamelCase(string name)
	{
		return string.IsNullOrEmpty(name)
			? name
			: char.ToLowerInvariant(name[0]) + name.Substring(1);
	}

	/// <summary>
	/// Returns the natural (pre-conversion) type of an expression and its display string.
	/// Uses <see cref="TypeInfo.Type"/> (the declared type) rather than
	/// <see cref="TypeInfo.ConvertedType"/>, which may be <c>object</c> when the
	/// argument is passed to a <c>params object[]</c> parameter.
	/// </summary>
	static (ITypeSymbol? Type, string TypeStr) GetNaturalType(TypeInfo typeInfo)
	{
		var type = typeInfo.Type ?? typeInfo.ConvertedType;
		var typeStr = type?.ToDisplayString(ParamTypeFormat) ?? "object";
		return (type, typeStr);
	}

	static string? GetNamespaceOf(ClassDeclarationSyntax classDecl)
	{
		foreach (var ancestor in classDecl.Ancestors())
		{
			if (ancestor is NamespaceDeclarationSyntax ns)
				return ns.Name.ToString();

			if (ancestor is FileScopedNamespaceDeclarationSyntax fns)
				return fns.Name.ToString();
		}

		return null;
	}
}

/// <summary>
/// Reference equality comparer for SyntaxNode-derived types (netstandard2.0 compatible).
/// </summary>
sealed class SyntaxNodeReferenceComparer<T> : IEqualityComparer<T>
	where T : class
{
	public static readonly SyntaxNodeReferenceComparer<T> Instance = new();

	SyntaxNodeReferenceComparer() { }

	public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

	public int GetHashCode(T obj) =>
		System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
}
