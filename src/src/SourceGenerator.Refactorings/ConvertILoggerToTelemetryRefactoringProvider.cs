using System.Composition;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.Telemetry.SourceGenerator.Refactorings;

[Shared]
[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(ConvertILoggerToTelemetryRefactoringProvider))]
[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1506:Avoid excessive class coupling",
	Justification = "Legacy code-refactoring provider that inspects many Roslyn/telemetry types."
)]
public sealed class ConvertILoggerToTelemetryRefactoringProvider : CodeRefactoringProvider
{
	// Matches structured-logging template placeholders: {Name}, {@Name} (destructure), {$Name} (stringify).
	// The @/$ prefixes follow Serilog conventions and are also used in Microsoft.Extensions.Logging
	// structured logging. Alignment and format specifiers are recognised but stripped.
	static readonly Regex TemplatePlaceholderRegex = new(
		@"\{(?:@|\$)?(?<name>[A-Za-z_]\w*)(?:,[-\d]+)?(?::[^}]+)?\}",
		RegexOptions.Compiled | RegexOptions.ExplicitCapture
	);

	static readonly Regex TemplateWordRegex = new(@"\b[A-Za-z][A-Za-z0-9]*", RegexOptions.Compiled);

	// Uses keyword aliases (string, int, bool) and short type names without global:: prefix.
	// Suitable for generated interface code that lives in the same file as the class.
	static readonly SymbolDisplayFormat ParamTypeFormat = new(
		typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
		miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
	);

	public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
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

		var logCalls = FindLogCalls(classDecl, loggerFields, semanticModel, context.CancellationToken);
		if (logCalls.Count == 0)
			return;

		var doc = context.Document;
		context.RegisterRefactoring(
			CodeAction.Create(
				$"Convert ILogger to I{classDecl.Identifier.ValueText}Logs",
				nestedActions:
				[
					CodeAction.Create(
						"In this class",
						ct => ConvertAsync(doc, classDecl, loggerFields, logCalls, semanticModel, ct),
						equivalenceKey: "Purview.Telemetry.ConvertILoggerToTelemetry.Class"
					),
					CodeAction.Create(
						"In this document",
						ct => ConvertDocumentAsync(doc, ct),
						equivalenceKey: "Purview.Telemetry.ConvertILoggerToTelemetry.Document"
					),
					CodeAction.Create(
						"In this project",
						ct => ConvertProjectAsync(doc.Project, ct),
						equivalenceKey: "Purview.Telemetry.ConvertILoggerToTelemetry.Project"
					),
					CodeAction.Create(
						"In this solution",
						ct => ConvertSolutionAsync(doc.Project.Solution, ct),
						equivalenceKey: "Purview.Telemetry.ConvertILoggerToTelemetry.Solution"
					),
				],
				isInlinable: false
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
		var interfaceName = "I" + className + "Logs";

		var callsWithMethods = AssignMethodNames(logCalls);

		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is null)
			return document;

		var interfaceCode = BuildInterfaceCode(interfaceName, callsWithMethods);
		var interfaceSyntax = ParseInterfaceNode(interfaceCode);

		var newClassDecl = RewriteClass(classDecl, loggerFields, callsWithMethods, interfaceName, semanticModel);

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

		// Add using Purview.Telemetry; to the file if not already present.
		var compilationRoot = (CompilationUnitSyntax)newRoot;
		if (!compilationRoot.Usings.Any(u => u.Name?.ToString() == TelemetryAttributeNames.PurviewTelemetryNamespace))
		{
			var newUsing = SyntaxFactory
				.UsingDirective(SyntaxFactory.ParseName(TelemetryAttributeNames.PurviewTelemetryNamespace))
				.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
			newRoot = compilationRoot.AddUsings(newUsing);
		}

		return document.WithSyntaxRoot(newRoot);
	}

	// -------------------------------------------------------------------------
	// Finding fields
	// -------------------------------------------------------------------------

	internal static List<ILoggerFieldInfo> FindILoggerFields(
		ClassDeclarationSyntax classDecl,
		SemanticModel semanticModel,
		CancellationToken cancellationToken
	)
	{
		var iLoggerOpen = semanticModel.Compilation.GetTypeByMetadataName(
			TelemetryAttributeNames.Logging.ILoggerOfT.MetadataFullName
		);
		var iLoggerNonGeneric = semanticModel.Compilation.GetTypeByMetadataName(
			TelemetryAttributeNames.Logging.ILogger.MetadataFullName
		);

		var result = new List<ILoggerFieldInfo>();

		foreach (var member in classDecl.Members)
		{
			if (member is not FieldDeclarationSyntax fieldDecl)
				continue;

			foreach (var variable in fieldDecl.Declaration.Variables)
			{
				cancellationToken.ThrowIfCancellationRequested();

				if (semanticModel.GetDeclaredSymbol(variable, cancellationToken) is not IFieldSymbol fieldSymbol)
					continue;

				if (fieldSymbol.Type is not INamedTypeSymbol namedType)
					continue;

				if (!IsILoggerType(namedType, iLoggerOpen, iLoggerNonGeneric))
					continue;

				result.Add(
					new ILoggerFieldInfo(
						FieldName: fieldSymbol.Name,
						FieldDeclaration: fieldDecl,
						PropertyDeclaration: null,
						TypeSymbol: namedType
					)
				);
			}
		}

		// Also check primary constructor parameters (C# 12+).
		if (classDecl.ParameterList is { } primaryCtorParams)
		{
			foreach (var param in primaryCtorParams.Parameters)
			{
				cancellationToken.ThrowIfCancellationRequested();

				if (semanticModel.GetDeclaredSymbol(param, cancellationToken) is not IParameterSymbol paramSymbol)
					continue;

				if (paramSymbol.Type is not INamedTypeSymbol namedType)
					continue;

				if (!IsILoggerType(namedType, iLoggerOpen, iLoggerNonGeneric))
					continue;

				result.Add(
					new ILoggerFieldInfo(
						FieldName: param.Identifier.Text,
						FieldDeclaration: null,
						PropertyDeclaration: null,
						TypeSymbol: namedType
					)
				);
			}
		}

		// Scan properties.
		foreach (var member in classDecl.Members.OfType<PropertyDeclarationSyntax>())
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (semanticModel.GetDeclaredSymbol(member, cancellationToken) is not IPropertySymbol propSymbol)
				continue;

			if (propSymbol.Type is not INamedTypeSymbol namedType)
				continue;

			if (!IsILoggerType(namedType, iLoggerOpen, iLoggerNonGeneric))
				continue;

			result.Add(
				new ILoggerFieldInfo(
					FieldName: propSymbol.Name,
					FieldDeclaration: null,
					PropertyDeclaration: member,
					TypeSymbol: namedType
				)
			);
		}

		// Scan regular method parameters.
		foreach (var method in classDecl.Members.OfType<MethodDeclarationSyntax>())
		{
			foreach (var param in method.ParameterList.Parameters)
			{
				cancellationToken.ThrowIfCancellationRequested();

				if (semanticModel.GetDeclaredSymbol(param, cancellationToken) is not IParameterSymbol paramSymbol)
					continue;

				if (paramSymbol.Type is not INamedTypeSymbol namedType)
					continue;

				if (!IsILoggerType(namedType, iLoggerOpen, iLoggerNonGeneric))
					continue;

				result.Add(
					new ILoggerFieldInfo(
						FieldName: param.Identifier.Text,
						FieldDeclaration: null,
						PropertyDeclaration: null,
						TypeSymbol: namedType
					)
				);
			}
		}

		return result;
	}

	// -------------------------------------------------------------------------
	// Finding log calls
	// -------------------------------------------------------------------------

	internal static List<LogCallInfo> FindLogCalls(
		ClassDeclarationSyntax classDecl,
		List<ILoggerFieldInfo> loggerFields,
		SemanticModel semanticModel,
		CancellationToken cancellationToken
	)
	{
		var loggerFieldNames = new HashSet<string>(loggerFields.Select(f => f.FieldName), StringComparer.Ordinal);

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
			if (methodName != "Log" && !IsLoggerConvenienceMethod(methodName))
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
			MemberAccessExpressionSyntax => null, // chained access – not a field reference
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

		var idx = 0;
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

		// Optional EventId (skip but capture literal integer value)
		int? explicitEventId = null;
		if (idx < args.Count && IsEventIdType(args[idx], semanticModel))
		{
			explicitEventId = TryExtractLiteralIntEventId(args[idx]);
			idx++;
		}

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
				TelemetryAttributeNames.System.Exception.MetadataFullName
			);
			var exTypeStr =
				exType?.ToDisplayString(ParamTypeFormat) ?? TelemetryAttributeNames.System.Exception.MetadataFullName;
			parameters.Add(new LogParameterInfo("exception", exTypeStr, exceptionExpression));
		}

		for (var i = 0; i < Math.Min(placeholders.Count, templateArgs.Count); i++)
		{
			var (_, typeStr) = GetNaturalType(semanticModel.GetTypeInfo(templateArgs[i], cancellationToken));

			var rawName = placeholders[i];
			var paramName = ToCamelCase(rawName);

			parameters.Add(new LogParameterInfo(paramName, typeStr, templateArgs[i]));
		}

		// If there are extra args without template placeholders, include them
		for (var i = placeholders.Count; i < templateArgs.Count; i++)
		{
			var (_, typeStr) = GetNaturalType(semanticModel.GetTypeInfo(templateArgs[i], cancellationToken));

			parameters.Add(new LogParameterInfo($"arg{i}", typeStr, templateArgs[i]));
		}

		return new LogCallInfo(
			Invocation: invocation,
			ILoggerMethodName: methodName,
			ExplicitLogLevel: explicitLogLevel,
			MessageTemplate: template,
			Parameters: parameters,
			ExceptionExpression: exceptionExpression,
			ExplicitEventId: explicitEventId
		);
	}

	static bool IsEventIdType(ArgumentSyntax arg, SemanticModel semanticModel)
	{
		var typeInfo = semanticModel.GetTypeInfo(arg.Expression);
		var type = typeInfo.ConvertedType ?? typeInfo.Type;
		return type is not null
			&& (
				type.ToDisplayString() == TelemetryAttributeNames.Logging.EventId.MetadataFullName
				|| type.SpecialType == SpecialType.System_Int32
			);
	}

	/// <summary>
	/// Returns the integer value when the EventId argument is a plain numeric literal
	/// (e.g. <c>42</c> in <c>LogInformation(42, "template", ...)</c>), otherwise null.
	/// <c>new EventId(42)</c> is intentionally not extracted because it requires
	/// evaluating a constructor call, which is out of scope for a refactoring.
	/// </summary>
	static int? TryExtractLiteralIntEventId(ArgumentSyntax arg) =>
		arg.Expression is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression } lit
		&& int.TryParse(
			lit.Token.ValueText,
			System.Globalization.NumberStyles.Integer,
			System.Globalization.CultureInfo.InvariantCulture,
			out var id
		)
			? id
			: null;

	static bool IsExceptionType(ArgumentSyntax arg, SemanticModel semanticModel)
	{
		var typeInfo = semanticModel.GetTypeInfo(arg.Expression);
		var type = typeInfo.ConvertedType ?? typeInfo.Type;
		if (type is null)
			return false;

		var exType = semanticModel.Compilation.GetTypeByMetadataName(
			TelemetryAttributeNames.System.Exception.MetadataFullName
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

	internal static List<(LogCallInfo Call, string MethodName)> AssignMethodNames(List<LogCallInfo> calls)
	{
		// Group calls that represent the same logical log operation (same attribute, template, params).
		// Identical calls share one interface method; distinct calls that happen to produce the same
		// base name from the template get a numeric suffix.
		var signatureToName = new Dictionary<string, string>(StringComparer.Ordinal);
		var usedNames = new HashSet<string>(StringComparer.Ordinal);
		var result = new List<(LogCallInfo, string)>(calls.Count);

		foreach (var call in calls)
		{
			var sigKey = GetCallSignatureKey(call);
			if (!signatureToName.TryGetValue(sigKey, out var name))
			{
				var baseName = GetBaseMethodName(call);
				name = baseName;
				var counter = 2;
				while (!usedNames.Add(name))
					name = baseName + counter++;

				signatureToName[sigKey] = name;
			}

			result.Add((call, name));
		}

		return result;
	}

	static string GetCallSignatureKey(LogCallInfo call)
	{
		// GetAttributeFor already embeds the MessageTemplate and ExplicitEventId, so the
		// attribute string alone is sufficient to uniquely identify the logging configuration.
		var attribute = GetAttributeFor(call);
		var paramTypes = string.Join(",", call.Parameters.Select(p => p.TypeDisplayString));
		return $"{attribute}|{paramTypes}";
	}

	static string GetBaseMethodName(LogCallInfo call)
	{
		if (!string.IsNullOrEmpty(call.MessageTemplate))
		{
			// Strip ALL {…} placeholder tokens, then extract words.
			var plainText = TemplatePlaceholderRegex.Replace(call.MessageTemplate, " ");
			var words = TemplateWordRegex
				.Matches(plainText)
				.Cast<Match>()
				.Select(m => ToPascalCaseWord(m.Value))
				.ToArray();

			if (words.Length > 0)
				return string.Join(string.Empty, words);
		}

		// Fall back: for Log(LogLevel.X, …) use the level name, else use the ILogger method name.
		return call.ILoggerMethodName == "Log" && call.ExplicitLogLevel is not null
			? "Log" + call.ExplicitLogLevel
			: call.ILoggerMethodName;
	}

	// -------------------------------------------------------------------------
	// Interface code generation
	// -------------------------------------------------------------------------

	static string BuildInterfaceCode(string interfaceName, List<(LogCallInfo Call, string MethodName)> callsWithMethods)
	{
		var sb = new StringBuilder();

		sb.AppendLine($"[{TelemetryAttributeNames.Logging.LoggerAttribute.RenderAttributeTypeName}]");
		sb.AppendLine($"public interface {interfaceName}");
		sb.AppendLine("{");
		sb.Append(BuildInterfaceMembers(callsWithMethods));
		sb.AppendLine("}");

		return sb.ToString();
	}

	/// <summary>
	/// Emits the interface method members (without the interface declaration header/footer).
	/// Used by the combined telemetry provider to merge log methods into a single interface.
	/// </summary>
	internal static string BuildInterfaceMembers(List<(LogCallInfo Call, string MethodName)> callsWithMethods)
	{
		var sb = new StringBuilder();
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

		return sb.ToString();
	}

	static string GetAttributeFor(LogCallInfo call)
	{
		if (call.ILoggerMethodName == "Log")
		{
			if (call.ExplicitLogLevel is null)
				return "Log";

			var mappedName = LogLevelToAttributeName(call.ExplicitLogLevel);
			if (mappedName is not null)
				return BuildAttributeArgs(mappedName, call, leadingArg: null);

			// Unmapped / None levels use [Log(LogLevel.X, …)]
			var levelArg = $"{TelemetryAttributeNames.Logging.LogLevel.RenderFullName}.{call.ExplicitLogLevel}";
			return BuildAttributeArgs("Log", call, leadingArg: levelArg);
		}

		// For LogTrace / LogDebug / LogInformation / … — derive attribute name from the level
		// suffix (strip "Log" prefix, then map the level name).
		var levelSuffix = call.ILoggerMethodName.StartsWith("Log", StringComparison.Ordinal)
			? call.ILoggerMethodName.Substring(3)
			: null;
		var attrName = levelSuffix is not null ? LogLevelToAttributeName(levelSuffix) ?? "Log" : "Log";
		return BuildAttributeArgs(attrName, call, leadingArg: null);
	}

	/// <summary>
	/// Maps a <code>Microsoft.Extensions.Logging.LogLevel</code> member name to the
	/// corresponding Purview Telemetry convenience-attribute name.
	/// The only non-trivial mapping is <c>Information</c> → <c>Info</c>;
	/// all other known levels match their own name.
	/// Returns <c>null</c> for unrecognised values (e.g. <c>None</c>).
	/// </summary>
	static string? LogLevelToAttributeName(string level) =>
		level == "Information" ? "Info"
		: level is "Trace" or "Debug" or "Warning" or "Error" or "Critical" ? level
		: null;

	/// <summary>
	/// Returns <see langword="true"/> when <paramref name="methodName"/> is one of the
	/// ILogger convenience methods (e.g. <c>LogTrace</c>, <c>LogInformation</c>).
	/// </summary>
	static bool IsLoggerConvenienceMethod(string methodName) =>
		methodName.StartsWith("Log", StringComparison.Ordinal)
		&& LogLevelToAttributeName(methodName.Substring(3)) is not null;

	/// <summary>
	/// Builds the full attribute expression string, incorporating an optional
	/// leading positional argument (e.g. a log-level), the call's literal EventId,
	/// and its message template.
	/// </summary>
	/// <example>
	/// BuildAttributeArgs("Info", call, null)
	///   → "Info" (no extra args)                           [plain log call with no literal template]
	///   → "Info(\"Getting weather for {City}\")"           [literal template, no EventId]
	///   → "Info(42, \"Getting weather for {City}\")"       [literal int EventId + template]
	/// BuildAttributeArgs("Log", call, "global::Microsoft.Extensions.Logging.LogLevel.None")
	///   → "Log(global::Microsoft.Extensions.Logging.LogLevel.None)"
	///   → "Log(global::Microsoft.Extensions.Logging.LogLevel.None, \"Diag: {Info}\")"
	///   → "Log(42, global::Microsoft.Extensions.Logging.LogLevel.None, \"Diag: {Info}\")"
	///      [eventId comes before level — matches LogAttribute(int eventId, LogLevel level, …)]
	/// </example>
	static string BuildAttributeArgs(string attrName, LogCallInfo call, string? leadingArg)
	{
		var args = new List<string>();
		var explicitEventId = call.ExplicitEventId.HasValue
			? call.ExplicitEventId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
			: null;

		// For [Log(…)], the constructor with both eventId and level is
		//   LogAttribute(int eventId, LogLevel level, …)
		// so eventId must come before the level argument.
		if (attrName == "Log" && leadingArg is not null && explicitEventId is not null)
		{
			args.Add(explicitEventId);
			args.Add(leadingArg);
		}
		else
		{
			if (leadingArg is not null)
				args.Add(leadingArg);

			if (explicitEventId is not null)
				args.Add(explicitEventId);
		}

		if (call.MessageTemplate is { Length: > 0 } template)
			args.Add(EscapeStringForAttribute(template));

		return args.Count == 0 ? attrName : $"{attrName}({string.Join(", ", args)})";
	}

	/// <summary>
	/// Wraps <paramref name="value"/> in a quoted C# string literal suitable for
	/// embedding in an attribute argument list, using Roslyn's
	/// <see cref="SymbolDisplay.FormatLiteral(string, bool)"/> to correctly escape all C# special characters.
	/// </summary>
	static string EscapeStringForAttribute(string value) => SymbolDisplay.FormatLiteral(value, quote: true);

	static string BuildParamList(IReadOnlyList<LogParameterInfo> parameters)
	{
		return parameters.Count == 0
			? string.Empty
			: string.Join(", ", parameters.Select(p => $"{p.TypeDisplayString} {p.Name}"));
	}

	// -------------------------------------------------------------------------
	// Document / project / solution scope helpers
	// -------------------------------------------------------------------------

	static async Task<Document> ConvertDocumentAsync(Document document, CancellationToken cancellationToken)
	{
		while (true)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			if (root is null)
				break;

			var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			if (semanticModel is null)
				break;

			ClassDeclarationSyntax? targetClass = null;
			List<ILoggerFieldInfo>? fields = null;
			List<LogCallInfo>? calls = null;

			foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
			{
				var f = FindILoggerFields(classDecl, semanticModel, cancellationToken);
				if (f.Count == 0)
					continue;

				var c = FindLogCalls(classDecl, f, semanticModel, cancellationToken);
				if (c.Count == 0)
					continue;

				targetClass = classDecl;
				fields = f;
				calls = c;
				break;
			}

			if (targetClass is null)
				break;

			document = await ConvertAsync(document, targetClass, fields!, calls!, semanticModel, cancellationToken)
				.ConfigureAwait(false);
		}

		return document;
	}

	static async Task<Solution> ConvertProjectAsync(Project project, CancellationToken cancellationToken)
	{
		var solution = project.Solution;
		foreach (var documentId in project.DocumentIds)
		{
			var document = solution.GetDocument(documentId);
			if (document is null)
				continue;

			var updated = await ConvertDocumentAsync(document, cancellationToken).ConfigureAwait(false);
			solution = updated.Project.Solution;
		}

		return solution;
	}

	static async Task<Solution> ConvertSolutionAsync(Solution solution, CancellationToken cancellationToken)
	{
		foreach (var projectId in solution.ProjectIds)
		{
			var project = solution.GetProject(projectId);
			if (project is null)
				continue;

			solution = await ConvertProjectAsync(project, cancellationToken).ConfigureAwait(false);
		}

		return solution;
	}

	static InterfaceDeclarationSyntax ParseInterfaceNode(string code)
	{
		var tree = CSharpSyntaxTree.ParseText(code);
		var root = tree.GetCompilationUnitRoot();

		// Try to find the interface inside a namespace or at top-level
		var interfaceDecl =
			root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().FirstOrDefault()
			?? throw new InvalidOperationException("Could not parse generated interface from: " + code);

		// Return as a standalone declaration with appropriate usings attached as leading trivia
		return interfaceDecl;
	}

	// -------------------------------------------------------------------------
	// Class rewriting
	// -------------------------------------------------------------------------

	internal static ClassDeclarationSyntax RewriteClass(
		ClassDeclarationSyntax classDecl,
		List<ILoggerFieldInfo> loggerFields,
		List<(LogCallInfo Call, string MethodName)> callsWithMethods,
		string interfaceName,
		SemanticModel semanticModel
	)
	{
		// Map non-canonical ctor logger param names → canonical name so duplicate injections
		// can be consolidated: e.g. (ILogger logger, ILogger logger2) → (IServicesLogger logger).
		var loggerVariableRemap = BuildLoggerVariableRemap(classDecl, loggerFields);

		// Build a map from invocation → new invocation
		var invocationMap = new Dictionary<InvocationExpressionSyntax, InvocationExpressionSyntax>(
			SyntaxNodeReferenceComparer<InvocationExpressionSyntax>.Instance
		);

		foreach (var (call, methodName) in callsWithMethods)
		{
			var receiverName = call.Invocation.Expression is MemberAccessExpressionSyntax ma
				? GetSimpleIdentifier(ma.Expression)
				: null;
			var canonicalReceiver =
				receiverName is not null && loggerVariableRemap.TryGetValue(receiverName, out var mapped)
					? mapped
					: null;
			var newInvocation = RewriteInvocation(call, methodName, canonicalReceiver);
			invocationMap[call.Invocation] = newInvocation;
		}

		// Build a map from field declarations → new field declarations
		var fieldMap = new Dictionary<FieldDeclarationSyntax, FieldDeclarationSyntax>(
			SyntaxNodeReferenceComparer<FieldDeclarationSyntax>.Instance
		);
		foreach (var field in loggerFields)
		{
			if (field.FieldDeclaration is null)
				continue;

			var newField = RewriteFieldDeclaration(field.FieldDeclaration, interfaceName);
			fieldMap[field.FieldDeclaration] = newField;
		}

		// Build a map from property declarations → new property declarations
		var propertyMap = new Dictionary<PropertyDeclarationSyntax, PropertyDeclarationSyntax>(
			SyntaxNodeReferenceComparer<PropertyDeclarationSyntax>.Instance
		);
		foreach (var field in loggerFields)
		{
			if (field.PropertyDeclaration is null)
				continue;

			var newProp = RewritePropertyDeclaration(field.PropertyDeclaration, interfaceName);
			propertyMap[field.PropertyDeclaration] = newProp;
		}

		// Find all constructor and method parameters that take an ILogger type
		var paramMap = BuildParamRewriteMap(classDecl, loggerFields, interfaceName, semanticModel);

		// Replace all nodes
		var newClassDecl = classDecl.ReplaceNodes(
			invocationMap
				.Keys.Cast<SyntaxNode>()
				.Concat(fieldMap.Keys.Cast<SyntaxNode>())
				.Concat(propertyMap.Keys.Cast<SyntaxNode>())
				.Concat(paramMap.Keys.Cast<SyntaxNode>()),
			(original, _) =>
				original is InvocationExpressionSyntax inv && invocationMap.TryGetValue(inv, out var newInv) ? newInv
				: original is FieldDeclarationSyntax fld && fieldMap.TryGetValue(fld, out var newFld) ? newFld
				: original is PropertyDeclarationSyntax prop && propertyMap.TryGetValue(prop, out var newProp) ? newProp
				: original is ParameterSyntax param && paramMap.TryGetValue(param, out var newParam) ? newParam
				: original
		);

		// Remove non-canonical logger parameters from constructor parameter lists.
		var paramsToRemove = new HashSet<string>(loggerVariableRemap.Keys, StringComparer.Ordinal);
		if (paramsToRemove.Count > 0)
		{
			// Primary constructor (C# 12+)
			if (newClassDecl.ParameterList is { } primaryCtorParams)
			{
				var filtered = primaryCtorParams.Parameters.Where(p => !paramsToRemove.Contains(p.Identifier.Text));
				newClassDecl = newClassDecl.WithParameterList(
					primaryCtorParams.WithParameters(SyntaxFactory.SeparatedList(filtered))
				);
			}

			// Regular constructors
			var ctors = newClassDecl.Members.OfType<ConstructorDeclarationSyntax>().ToList();
			if (ctors.Count > 0)
			{
				newClassDecl = newClassDecl.ReplaceNodes(
					ctors,
					(ctor, _) =>
					{
						var filtered = ctor.ParameterList.Parameters.Where(p =>
							!paramsToRemove.Contains(p.Identifier.Text)
						);
						return ctor.WithParameterList(
							ctor.ParameterList.WithParameters(SyntaxFactory.SeparatedList(filtered))
						);
					}
				);
			}
		}

		return newClassDecl;
	}

	static InvocationExpressionSyntax RewriteInvocation(
		LogCallInfo call,
		string newMethodName,
		string? canonicalReceiverName = null
	)
	{
		// Build new argument list: only the template parameter args (no template string, no EventId)
		var newArgs = SyntaxFactory.SeparatedList(
			call.Parameters.Select(p =>
				SyntaxFactory.Argument(p.ArgumentExpression).WithTriviaFrom(p.ArgumentExpression)
			)
		);

		var memberAccess = (MemberAccessExpressionSyntax)call.Invocation.Expression;

		var newMemberAccess = memberAccess.WithName(SyntaxFactory.IdentifierName(newMethodName));

		// If this invocation's receiver was a non-canonical logger, reroute to the canonical one.
		if (canonicalReceiverName is not null)
			newMemberAccess = newMemberAccess.WithExpression(
				SyntaxFactory.IdentifierName(canonicalReceiverName).WithTriviaFrom(memberAccess.Expression)
			);

		return call
			.Invocation.WithExpression(newMemberAccess)
			.WithArgumentList(SyntaxFactory.ArgumentList(newArgs).WithTriviaFrom(call.Invocation.ArgumentList));
	}

	static FieldDeclarationSyntax RewriteFieldDeclaration(FieldDeclarationSyntax fieldDecl, string interfaceName)
	{
		var newType = SyntaxFactory.IdentifierName(interfaceName).WithTriviaFrom(fieldDecl.Declaration.Type);

		return fieldDecl.WithDeclaration(fieldDecl.Declaration.WithType(newType));
	}

	static PropertyDeclarationSyntax RewritePropertyDeclaration(
		PropertyDeclarationSyntax propDecl,
		string interfaceName
	)
	{
		var newType = SyntaxFactory.IdentifierName(interfaceName).WithTriviaFrom(propDecl.Type);
		return propDecl.WithType(newType);
	}

	static Dictionary<ParameterSyntax, ParameterSyntax> BuildParamRewriteMap(
		ClassDeclarationSyntax classDecl,
		List<ILoggerFieldInfo> loggerFields,
		string interfaceName,
		SemanticModel semanticModel
	)
	{
		var result = new Dictionary<ParameterSyntax, ParameterSyntax>(
			SyntaxNodeReferenceComparer<ParameterSyntax>.Instance
		);

		// Match parameters by the exact types of the identified logger fields.
		// This is more precise than re-detecting all ILogger variants from scratch.
		var loggerFieldTypes = new HashSet<ITypeSymbol>(
			loggerFields.Select(f => f.TypeSymbol),
			SymbolEqualityComparer.Default
		);

		// Explicit constructors
		foreach (var ctor in classDecl.Members.OfType<ConstructorDeclarationSyntax>())
			RewriteMatchingParams(
				ctor.ParameterList.Parameters,
				loggerFieldTypes,
				interfaceName,
				semanticModel,
				result
			);

		// Primary constructor (C# 12+)
		if (classDecl.ParameterList is { } primaryCtorParams)
			RewriteMatchingParams(primaryCtorParams.Parameters, loggerFieldTypes, interfaceName, semanticModel, result);

		// Regular methods
		foreach (var method in classDecl.Members.OfType<MethodDeclarationSyntax>())
			RewriteMatchingParams(
				method.ParameterList.Parameters,
				loggerFieldTypes,
				interfaceName,
				semanticModel,
				result
			);

		return result;
	}

	static void RewriteMatchingParams(
		SeparatedSyntaxList<ParameterSyntax> parameters,
		HashSet<ITypeSymbol> loggerFieldTypes,
		string interfaceName,
		SemanticModel semanticModel,
		Dictionary<ParameterSyntax, ParameterSyntax> result
	)
	{
		foreach (var param in parameters)
		{
			if (param.Type is null)
				continue;

			var typeInfo = semanticModel.GetTypeInfo(param.Type);
			var type = typeInfo.Type;
			if (type is null || !loggerFieldTypes.Contains(type))
				continue;

			result[param] = param.WithType(SyntaxFactory.IdentifierName(interfaceName).WithTriviaFrom(param.Type));
		}
	}

	// -------------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------------

	/// <summary>
	/// Returns a map from non-canonical logger parameter name to the canonical name for each
	/// constructor parameter list that contains more than one ILogger parameter.
	/// E.g. given <c>(ILogger logger, ILogger logger2)</c>, returns <c>{logger2 → logger}</c>.
	/// Only primary constructors and regular constructors are considered; method parameters are
	/// intentionally excluded because deduplicating those would change the public API surface.
	/// </summary>
	static Dictionary<string, string> BuildLoggerVariableRemap(
		ClassDeclarationSyntax classDecl,
		List<ILoggerFieldInfo> loggerFields
	)
	{
		var loggerFieldNames = new HashSet<string>(loggerFields.Select(f => f.FieldName), StringComparer.Ordinal);
		var remap = new Dictionary<string, string>(StringComparer.Ordinal);

		if (classDecl.ParameterList is { } primaryCtorParams)
			AddParamListRemap(primaryCtorParams.Parameters, loggerFieldNames, remap);

		foreach (var ctor in classDecl.Members.OfType<ConstructorDeclarationSyntax>())
			AddParamListRemap(ctor.ParameterList.Parameters, loggerFieldNames, remap);

		return remap;
	}

	static void AddParamListRemap(
		SeparatedSyntaxList<ParameterSyntax> parameters,
		HashSet<string> loggerFieldNames,
		Dictionary<string, string> remap
	)
	{
		string? canonicalName = null;
		foreach (var param in parameters)
		{
			if (!loggerFieldNames.Contains(param.Identifier.Text))
				continue;

			if (canonicalName is null)
				canonicalName = param.Identifier.Text;
			else
				remap[param.Identifier.Text] = canonicalName;
		}
	}

	static bool IsILoggerType(
		INamedTypeSymbol type,
		INamedTypeSymbol? iLoggerOpen,
		INamedTypeSymbol? iLoggerNonGeneric
	) =>
		(
			type.IsGenericType
			&& iLoggerOpen is not null
			&& SymbolEqualityComparer.Default.Equals(type.ConstructedFrom, iLoggerOpen)
		) || (iLoggerNonGeneric is not null && SymbolEqualityComparer.Default.Equals(type, iLoggerNonGeneric));

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
		return string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name.Substring(1);
	}

	static string ToPascalCaseWord(string word)
	{
		if (string.IsNullOrEmpty(word))
			return word;

		// ALL_CAPS (e.g. "HELLO", "HTTP") → only capitalise the first letter.
		if (word.Length > 1 && word.All(char.IsUpper))
#pragma warning disable CA1308
			return char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant();
#pragma warning restore CA1308

		// Mixed / lowercase: just ensure the first character is uppercase.
		return char.ToUpperInvariant(word[0]) + word.Substring(1);
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

	public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
}
