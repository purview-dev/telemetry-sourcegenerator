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
	Name = nameof(ConvertActivitySourceToTelemetryRefactoringProvider)
)]
[Shared]
public sealed class ConvertActivitySourceToTelemetryRefactoringProvider : CodeRefactoringProvider
{
	static readonly Regex WordSplitterRegex = new(@"[\s\-_./\\]+", RegexOptions.Compiled);
	static readonly Regex CamelCaseSplitRegex = new(
		@"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])",
		RegexOptions.Compiled
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

		var activitySourceFields = FindActivitySourceFields(
			classDecl,
			semanticModel,
			context.CancellationToken
		);
		if (activitySourceFields.Count == 0)
			return;

		var activityCalls = FindActivityCalls(
			classDecl,
			activitySourceFields,
			semanticModel,
			context.CancellationToken
		);
		if (activityCalls.Count == 0)
			return;

		var className = classDecl.Identifier.ValueText;
		var doc = context.Document;
		context.RegisterRefactoring(
			CodeAction.Create(
				$"Convert ActivitySource to I{className}Tracing",
				nestedActions:
				[
					CodeAction.Create(
									"In this class",
									ct => ConvertAsync(doc, classDecl, activitySourceFields, activityCalls, semanticModel, ct),
									equivalenceKey: "Purview.Telemetry.ConvertActivitySourceToTelemetry.Class"
								),
					CodeAction.Create(
						"In this document",
						ct => ConvertDocumentAsync(doc, ct),
						equivalenceKey: "Purview.Telemetry.ConvertActivitySourceToTelemetry.Document"
					),
					CodeAction.Create(
						"In this project",
						ct => ConvertProjectAsync(doc.Project, ct),
						equivalenceKey: "Purview.Telemetry.ConvertActivitySourceToTelemetry.Project"
					),
					CodeAction.Create(
						"In this solution",
						ct => ConvertSolutionAsync(doc.Project.Solution, ct),
						equivalenceKey: "Purview.Telemetry.ConvertActivitySourceToTelemetry.Solution"
					)
,
				],
				isInlinable: false
			)
		);
	}

	static async Task<Document> ConvertAsync(
		Document document,
		ClassDeclarationSyntax classDecl,
		List<ActivitySourceFieldInfo> activitySourceFields,
		List<ActivitySourceCallInfo> activityCalls,
		SemanticModel semanticModel,
		CancellationToken cancellationToken
	)
	{
		var className = classDecl.Identifier.ValueText;
		var interfaceName = "I" + className + "Tracing";

		var callsWithMethods = AssignMethodNames(activityCalls);

		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is null)
			return document;

		var interfaceCode = BuildInterfaceCode(interfaceName, callsWithMethods);
		var interfaceSyntax = ParseInterfaceNode(interfaceCode);

		var newClassDecl = RewriteClass(
			classDecl,
			activitySourceFields,
			callsWithMethods,
			interfaceName,
			semanticModel
		);

		var newRoot = root.ReplaceNode(classDecl, newClassDecl);
		var replacedClass = newRoot
			.DescendantNodes()
			.OfType<ClassDeclarationSyntax>()
			.First(c => c.Identifier.ValueText == className);

		newRoot = newRoot.InsertNodesBefore(
			replacedClass,
			[interfaceSyntax.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)]
		);

		// Add using Purview.Telemetry; and using System.Diagnostics; if not already present.
		var compilationRoot = (CompilationUnitSyntax)newRoot;
		if (!compilationRoot.Usings.Any(u => u.Name?.ToString() == Constants.PurviewTelemetryNamespace))
		{
			var newUsing = SyntaxFactory
				.UsingDirective(SyntaxFactory.ParseName(Constants.PurviewTelemetryNamespace))
				.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
			newRoot = compilationRoot.AddUsings(newUsing);
			compilationRoot = (CompilationUnitSyntax)newRoot;
		}

		if (!compilationRoot.Usings.Any(u => u.Name?.ToString() == Constants.SystemDiagnosticsNamespace))
		{
			var newUsing = SyntaxFactory
				.UsingDirective(SyntaxFactory.ParseName(Constants.SystemDiagnosticsNamespace))
				.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
			newRoot = compilationRoot.AddUsings(newUsing);
		}

		return document.WithSyntaxRoot(newRoot);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Document / project / solution scope helpers
	// ─────────────────────────────────────────────────────────────────────────

	static async Task<Document> ConvertDocumentAsync(
		Document document,
		CancellationToken cancellationToken
	)
	{
		while (true)
		{
			var root = await document
				.GetSyntaxRootAsync(cancellationToken)
				.ConfigureAwait(false);
			if (root is null)
				break;

			var semanticModel = await document
				.GetSemanticModelAsync(cancellationToken)
				.ConfigureAwait(false);
			if (semanticModel is null)
				break;

			ClassDeclarationSyntax? targetClass = null;
			List<ActivitySourceFieldInfo>? fields = null;
			List<ActivitySourceCallInfo>? calls = null;

			foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
			{
				var f = FindActivitySourceFields(classDecl, semanticModel, cancellationToken);
				if (f.Count == 0)
					continue;

				var c = FindActivityCalls(classDecl, f, semanticModel, cancellationToken);
				if (c.Count == 0)
					continue;

				targetClass = classDecl;
				fields = f;
				calls = c;
				break;
			}

			if (targetClass is null)
				break;

			document = await ConvertAsync(
					document,
					targetClass,
					fields!,
					calls!,
					semanticModel,
					cancellationToken
				)
				.ConfigureAwait(false);
		}

		return document;
	}

	static async Task<Solution> ConvertProjectAsync(
		Project project,
		CancellationToken cancellationToken
	)
	{
		var solution = project.Solution;
		foreach (var documentId in project.DocumentIds)
		{
			var document = solution.GetDocument(documentId);
			if (document is null)
				continue;

			var updated = await ConvertDocumentAsync(document, cancellationToken)
				.ConfigureAwait(false);
			solution = updated.Project.Solution;
		}

		return solution;
	}

	static async Task<Solution> ConvertSolutionAsync(
		Solution solution,
		CancellationToken cancellationToken
	)
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

	// ─────────────────────────────────────────────────────────────────────────
	// Detection
	// ─────────────────────────────────────────────────────────────────────────

	internal static List<ActivitySourceFieldInfo> FindActivitySourceFields(
		ClassDeclarationSyntax classDecl,
		SemanticModel semanticModel,
		CancellationToken cancellationToken
	)
	{
		var activitySourceType = semanticModel.Compilation.GetTypeByMetadataName(
			Constants.Activities.SystemDiagnostics.ActivitySource.FullyQualifiedName
		);
		if (activitySourceType is null)
			return [];

		var result = new List<ActivitySourceFieldInfo>();

		// Fields
		foreach (var member in classDecl.Members.OfType<FieldDeclarationSyntax>())
		{
			foreach (var variable in member.Declaration.Variables)
			{
				cancellationToken.ThrowIfCancellationRequested();

				if (
					semanticModel.GetDeclaredSymbol(variable, cancellationToken)
					is not IFieldSymbol fieldSymbol
				)
					continue;

				if (
					!SymbolEqualityComparer.Default.Equals(fieldSymbol.Type, activitySourceType)
				)
					continue;

				result.Add(
					new ActivitySourceFieldInfo(
						FieldName: fieldSymbol.Name,
						FieldDeclaration: member,
						PropertyDeclaration: null,
						TypeSymbol: fieldSymbol.Type
					)
				);
			}
		}

		// Primary constructor parameters
		if (classDecl.ParameterList is { } primaryCtorParams)
		{
			foreach (var param in primaryCtorParams.Parameters)
			{
				cancellationToken.ThrowIfCancellationRequested();

				if (
					semanticModel.GetDeclaredSymbol(param, cancellationToken)
					is not IParameterSymbol paramSymbol
				)
					continue;

				if (
					!SymbolEqualityComparer.Default.Equals(
						paramSymbol.Type,
						activitySourceType
					)
				)
					continue;

				result.Add(
					new ActivitySourceFieldInfo(
						FieldName: param.Identifier.Text,
						FieldDeclaration: null,
						PropertyDeclaration: null,
						TypeSymbol: paramSymbol.Type
					)
				);
			}
		}

		// Properties
		foreach (var member in classDecl.Members.OfType<PropertyDeclarationSyntax>())
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (
				semanticModel.GetDeclaredSymbol(member, cancellationToken)
				is not IPropertySymbol propSymbol
			)
				continue;

			if (!SymbolEqualityComparer.Default.Equals(propSymbol.Type, activitySourceType))
				continue;

			result.Add(
				new ActivitySourceFieldInfo(
					FieldName: propSymbol.Name,
					FieldDeclaration: null,
					PropertyDeclaration: member,
					TypeSymbol: propSymbol.Type
				)
			);
		}

		return result;
	}

	internal static List<ActivitySourceCallInfo> FindActivityCalls(
		ClassDeclarationSyntax classDecl,
		List<ActivitySourceFieldInfo> sourceFields,
		SemanticModel semanticModel,
		CancellationToken cancellationToken
	)
	{
		var fieldNames = new HashSet<string>(
			sourceFields.Select(f => f.FieldName),
			StringComparer.Ordinal
		);
		var result = new List<ActivitySourceCallInfo>();

		foreach (
			var invocation in classDecl.DescendantNodes().OfType<InvocationExpressionSyntax>()
		)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
				continue;

			var receiverName = GetSimpleIdentifier(memberAccess.Expression);
			if (receiverName is null || !fieldNames.Contains(receiverName))
				continue;

			if (memberAccess.Name.Identifier.Text != "StartActivity")
				continue;

			var args = invocation.ArgumentList.Arguments;

			string? activityName = null;
			string? activityKind = null;

			// First arg: activity name (string literal or named arg)
			if (args.Count >= 1)
			{
				var nameArg = args[0];
				if (
					nameArg.NameColon is null
					&& nameArg.Expression is LiteralExpressionSyntax lit
				)
					activityName = lit.Token.ValueText;
				else if (nameArg.NameColon?.Name.Identifier.Text == "name"
					&& nameArg.Expression is LiteralExpressionSyntax namedLit)
					activityName = namedLit.Token.ValueText;
			}

			// Second arg or named "kind" arg: ActivityKind
			for (var i = 0; i < args.Count; i++)
			{
				var arg = args[i];
				var isKindArg =
					(i == 1 && arg.NameColon is null)
					|| arg.NameColon?.Name.Identifier.Text == "kind";
				if (!isKindArg)
					continue;

				var kindSymbol = semanticModel.GetSymbolInfo(arg.Expression, cancellationToken).Symbol;
				if (
					kindSymbol
					is IFieldSymbol
					{
						ContainingType.Name: "ActivityKind",
					} kindField
				)
					activityKind = kindField.Name;
				break;
			}

			result.Add(
				new ActivitySourceCallInfo(invocation, activityName, activityKind, receiverName)
			);
		}

		return result;
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Method name assignment
	// ─────────────────────────────────────────────────────────────────────────

	static List<(ActivitySourceCallInfo Call, string MethodName)> AssignMethodNames(
		List<ActivitySourceCallInfo> calls
	)
	{
		var signatureToName = new Dictionary<string, string>(StringComparer.Ordinal);
		var usedNames = new HashSet<string>(StringComparer.Ordinal);
		var result = new List<(ActivitySourceCallInfo, string)>(calls.Count);

		foreach (var call in calls)
		{
			var sigKey = GetCallSignatureKey(call);
			if (!signatureToName.TryGetValue(sigKey, out var name))
			{
				var baseName = DeriveActivityMethodName(call);
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

	static string GetCallSignatureKey(ActivitySourceCallInfo call)
	{
		var kind = call.ActivityKind ?? "Internal";
		return $"{call.ActivityName ?? ""}|{kind}";
	}

	internal static string DeriveActivityMethodName(ActivitySourceCallInfo call)
	{
		if (!string.IsNullOrEmpty(call.ActivityName))
		{
			// Split on separators and camel-case boundaries, then PascalCase each word
			var words = SplitIntoWords(call.ActivityName!);
			if (words.Length > 0)
				return string.Concat(words.Select(ToPascalCaseWord));
		}

		// Fallback: use a generic name based on ActivityKind if provided
		return call.ActivityKind is not null ? call.ActivityKind + "Activity" : "Activity";
	}

	static string[] SplitIntoWords(string name)
	{
		// First split on explicit separators
		var parts = WordSplitterRegex.Split(name);
		var words = new List<string>();
		foreach (var part in parts)
		{
			if (string.IsNullOrEmpty(part))
				continue;
			// Further split on camelCase boundaries
			var subParts = CamelCaseSplitRegex.Split(part);
			words.AddRange(subParts.Where(w => !string.IsNullOrEmpty(w)));
		}

		return [.. words];
	}

	static string ToPascalCaseWord(string word)
	{
		if (string.IsNullOrEmpty(word))
			return word;

		// ALL_CAPS (e.g. "HTTP") → capitalise only the first
		if (word.Length > 1 && word.All(char.IsUpper))
#pragma warning disable CA1308
			return char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant();
#pragma warning restore CA1308

		return char.ToUpperInvariant(word[0]) + word.Substring(1);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Interface code generation
	// ─────────────────────────────────────────────────────────────────────────

	internal static string BuildInterfaceCode(
		string interfaceName,
		List<(ActivitySourceCallInfo Call, string MethodName)> callsWithMethods
	)
	{
		var sb = new StringBuilder();

		sb.AppendLine($"[{Constants.Activities.ActivitySourceAttributeShortName}]");
		sb.AppendLine($"public interface {interfaceName}");
		sb.AppendLine("{");
		sb.Append(BuildInterfaceMembers(callsWithMethods));
		sb.AppendLine("}");

		return sb.ToString();
	}

	static string BuildActivityAttribute(ActivitySourceCallInfo call) =>
		call.ActivityKind is null or "Internal"
			? "Activity"
			: $"Activity(ActivityKind.{call.ActivityKind})";

	static InterfaceDeclarationSyntax ParseInterfaceNode(string code)
	{
		var tree = CSharpSyntaxTree.ParseText(code);
		var root = tree.GetCompilationUnitRoot();

		return root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().FirstOrDefault()
			?? throw new InvalidOperationException(
				"Could not parse generated interface from: " + code
			);
	}

	internal static List<(ActivitySourceCallInfo Call, string MethodName)> AssignMethodNamesInternal(
		List<ActivitySourceCallInfo> calls
	) => AssignMethodNames(calls);

	internal static string BuildActivityAttributeInternal(ActivitySourceCallInfo call) =>
		BuildActivityAttribute(call);

	internal static ClassDeclarationSyntax RewriteClassInternal(
		ClassDeclarationSyntax classDecl,
		List<ActivitySourceFieldInfo> activitySourceFields,
		List<(ActivitySourceCallInfo Call, string MethodName)> callsWithMethods,
		string interfaceName,
		SemanticModel semanticModel
	) => RewriteClass(classDecl, activitySourceFields, callsWithMethods, interfaceName, semanticModel);

	internal static string BuildInterfaceMembers(
		List<(ActivitySourceCallInfo Call, string MethodName)> callsWithMethods
	)
	{
		var sb = new StringBuilder();
		var emittedSignatures = new HashSet<string>(StringComparer.Ordinal);

		foreach (var (call, methodName) in callsWithMethods)
		{
			var attribute = BuildActivityAttribute(call);
			var signature = $"{attribute}|{methodName}";

			if (!emittedSignatures.Add(signature))
				continue;

			sb.AppendLine($"\t[{attribute}]");
			sb.AppendLine($"\tActivity? {methodName}();");
			sb.AppendLine();
		}

		return sb.ToString();
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Class rewriting
	// ─────────────────────────────────────────────────────────────────────────

	static ClassDeclarationSyntax RewriteClass(
		ClassDeclarationSyntax classDecl,
		List<ActivitySourceFieldInfo> activitySourceFields,
		List<(ActivitySourceCallInfo Call, string MethodName)> callsWithMethods,
		string interfaceName,
		SemanticModel semanticModel
	)
	{
		// Build invocation map: StartActivity("name") → interface method call
		var invocationMap = new Dictionary<
			InvocationExpressionSyntax,
			InvocationExpressionSyntax
		>(SyntaxNodeReferenceComparer<InvocationExpressionSyntax>.Instance);

		foreach (var (call, methodName) in callsWithMethods)
		{
			var newInvocation = RewriteInvocation(call, methodName);
			invocationMap[call.Invocation] = newInvocation;
		}

		// Build field map
		var fieldMap = new Dictionary<FieldDeclarationSyntax, FieldDeclarationSyntax>(
			SyntaxNodeReferenceComparer<FieldDeclarationSyntax>.Instance
		);
		foreach (var field in activitySourceFields)
		{
			if (field.FieldDeclaration is null)
				continue;

			fieldMap[field.FieldDeclaration] = RewriteFieldDeclaration(
				field.FieldDeclaration,
				interfaceName
			);
		}

		// Build property map
		var propertyMap = new Dictionary<PropertyDeclarationSyntax, PropertyDeclarationSyntax>(
			SyntaxNodeReferenceComparer<PropertyDeclarationSyntax>.Instance
		);
		foreach (var field in activitySourceFields)
		{
			if (field.PropertyDeclaration is null)
				continue;

			propertyMap[field.PropertyDeclaration] = RewritePropertyDeclaration(
				field.PropertyDeclaration,
				interfaceName
			);
		}

		// Build parameter map
		var paramMap = BuildParamRewriteMap(
			classDecl,
			activitySourceFields,
			interfaceName,
			semanticModel
		);

		return classDecl.ReplaceNodes(
			invocationMap
				.Keys.Cast<SyntaxNode>()
				.Concat(fieldMap.Keys.Cast<SyntaxNode>())
				.Concat(propertyMap.Keys.Cast<SyntaxNode>())
				.Concat(paramMap.Keys.Cast<SyntaxNode>()),
			(original, _) =>
				original is InvocationExpressionSyntax inv
				&& invocationMap.TryGetValue(inv, out var newInv)
					? newInv
				: original is FieldDeclarationSyntax fld
				&& fieldMap.TryGetValue(fld, out var newFld)
					? newFld
				: original is PropertyDeclarationSyntax prop
				&& propertyMap.TryGetValue(prop, out var newProp)
					? newProp
				: original is ParameterSyntax param && paramMap.TryGetValue(param, out var newParam)
					? newParam
				: original
		);
	}

	static InvocationExpressionSyntax RewriteInvocation(
		ActivitySourceCallInfo call,
		string newMethodName
	)
	{
		var memberAccess = (MemberAccessExpressionSyntax)call.Invocation.Expression;

		// Replace StartActivity(...) with {MethodName}()
		var newMemberAccess = memberAccess.WithName(
			SyntaxFactory.IdentifierName(newMethodName)
		);

		return call
			.Invocation.WithExpression(newMemberAccess)
			.WithArgumentList(
				SyntaxFactory
					.ArgumentList()
					.WithTriviaFrom(call.Invocation.ArgumentList)
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
		List<ActivitySourceFieldInfo> activitySourceFields,
		string interfaceName,
		SemanticModel semanticModel
	)
	{
		var result = new Dictionary<ParameterSyntax, ParameterSyntax>(
			SyntaxNodeReferenceComparer<ParameterSyntax>.Instance
		);

		// Build a set of qualified type names so we can match by name when
		// the classDecl has already been rewritten (nodes no longer in the
		// original semantic model's tree).
		var fieldTypeNames = new HashSet<string>(
			activitySourceFields.Select(f => f.TypeSymbol.ToDisplayString()),
			StringComparer.Ordinal
		);
		var fieldTypeShortNames = new HashSet<string>(
			activitySourceFields.Select(f => f.TypeSymbol.Name),
			StringComparer.Ordinal
		);

		foreach (var ctor in classDecl.Members.OfType<ConstructorDeclarationSyntax>())
			RewriteMatchingParams(
				ctor.ParameterList.Parameters,
				fieldTypeNames,
				fieldTypeShortNames,
				interfaceName,
				semanticModel,
				result
			);

		if (classDecl.ParameterList is { } primaryCtorParams)
			RewriteMatchingParams(
				primaryCtorParams.Parameters,
				fieldTypeNames,
				fieldTypeShortNames,
				interfaceName,
				semanticModel,
				result
			);

		foreach (var method in classDecl.Members.OfType<MethodDeclarationSyntax>())
			RewriteMatchingParams(
				method.ParameterList.Parameters,
				fieldTypeNames,
				fieldTypeShortNames,
				interfaceName,
				semanticModel,
				result
			);

		return result;
	}

	static void RewriteMatchingParams(
		SeparatedSyntaxList<ParameterSyntax> parameters,
		HashSet<string> fieldTypeNames,
		HashSet<string> fieldTypeShortNames,
		string interfaceName,
		SemanticModel semanticModel,
		Dictionary<ParameterSyntax, ParameterSyntax> result
	)
	{
		foreach (var param in parameters)
		{
			if (param.Type is null)
				continue;

			// Try semantic resolution first (accurate); fall back to syntax
			// name matching when the node has already been rewritten and is
			// no longer part of the semantic model's tree.
			bool matched;
			try
			{
				var typeInfo = semanticModel.GetTypeInfo(param.Type);
				var type = typeInfo.Type;
				matched = type is not null
					&& fieldTypeNames.Contains(type.ToDisplayString());
			}
			catch (ArgumentException)
			{
				// Node is not in the semantic tree (e.g., already rewritten).
				// Fall back to matching by short name from source text.
				var typeName = param.Type.ToString().TrimEnd('?');
				matched = fieldTypeShortNames.Contains(typeName)
					|| fieldTypeNames.Contains(typeName);
			}

			if (!matched)
				continue;

			result[param] = param.WithType(
				SyntaxFactory.IdentifierName(interfaceName).WithTriviaFrom(param.Type)
			);
		}
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Helpers
	// ─────────────────────────────────────────────────────────────────────────

	static string? GetSimpleIdentifier(ExpressionSyntax expression) =>
		expression switch
		{
			IdentifierNameSyntax id => id.Identifier.Text,
			_ => null,
		};
}
