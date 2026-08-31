using System.Composition;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.Telemetry.SourceGenerator.Refactorings;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(ConvertMetricsToTelemetryRefactoringProvider))]
[Shared]
public sealed class ConvertMetricsToTelemetryRefactoringProvider : CodeRefactoringProvider
{
	static readonly Regex WordSplitterRegex = new(@"[\s\-_./\\]+", RegexOptions.Compiled);
	static readonly Regex CamelCaseSplitRegex = new(
		@"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])",
		RegexOptions.Compiled
	);

	// Suffixes stripped when deriving method names from field names
	static readonly string[] InstrumentSuffixes = ["UpDownCounter", "Histogram", "Counter", "Gauge", "Meter"];

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

		var metricsFields = FindMetricsFields(classDecl, semanticModel, context.CancellationToken);
		if (metricsFields.Count == 0)
			return;

		var metricsCalls = FindMetricsCalls(classDecl, metricsFields, semanticModel, context.CancellationToken);
		if (metricsCalls.Count == 0)
			return;

		var className = classDecl.Identifier.ValueText;
		var doc = context.Document;
		context.RegisterRefactoring(
			CodeAction.Create(
				$"Convert Metrics to I{className}Metrics",
				nestedActions:
				[
					CodeAction.Create(
						"In this class",
						ct => ConvertAsync(doc, classDecl, metricsFields, metricsCalls, semanticModel, ct),
						equivalenceKey: "Purview.Telemetry.ConvertMetricsToTelemetry.Class"
					),
					CodeAction.Create(
						"In this document",
						ct => ConvertDocumentAsync(doc, ct),
						equivalenceKey: "Purview.Telemetry.ConvertMetricsToTelemetry.Document"
					),
					CodeAction.Create(
						"In this project",
						ct => ConvertProjectAsync(doc.Project, ct),
						equivalenceKey: "Purview.Telemetry.ConvertMetricsToTelemetry.Project"
					),
					CodeAction.Create(
						"In this solution",
						ct => ConvertSolutionAsync(doc.Project.Solution, ct),
						equivalenceKey: "Purview.Telemetry.ConvertMetricsToTelemetry.Solution"
					),
				],
				isInlinable: false
			)
		);
	}

	static async Task<Document> ConvertAsync(
		Document document,
		ClassDeclarationSyntax classDecl,
		List<MetricsFieldInfo> metricsFields,
		List<MetricsCallInfo> metricsCalls,
		SemanticModel semanticModel,
		CancellationToken cancellationToken
	)
	{
		var className = classDecl.Identifier.ValueText;
		var interfaceName = "I" + className + "Metrics";

		var callsWithMethods = AssignMethodNames(metricsCalls, metricsFields);

		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is null)
			return document;

		var interfaceCode = BuildInterfaceCode(interfaceName, callsWithMethods, metricsFields);
		var interfaceSyntax = ParseInterfaceNode(interfaceCode);

		var newClassDecl = RewriteClass(classDecl, metricsFields, callsWithMethods, interfaceName, semanticModel);

		var newRoot = root.ReplaceNode(classDecl, newClassDecl);
		var replacedClass = newRoot
			.DescendantNodes()
			.OfType<ClassDeclarationSyntax>()
			.First(c => c.Identifier.ValueText == className);

		newRoot = newRoot.InsertNodesBefore(
			replacedClass,
			[interfaceSyntax.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)]
		);

		var compilationRoot = (CompilationUnitSyntax)newRoot;
		if (!compilationRoot.Usings.Any(u => u.Name?.ToString() == Constants.PurviewTelemetryNamespace))
		{
			var newUsing = SyntaxFactory
				.UsingDirective(SyntaxFactory.ParseName(Constants.PurviewTelemetryNamespace))
				.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
			newRoot = compilationRoot.AddUsings(newUsing);
		}

		return document.WithSyntaxRoot(newRoot);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Document / project / solution scope helpers
	// ─────────────────────────────────────────────────────────────────────────

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
			List<MetricsFieldInfo>? fields = null;
			List<MetricsCallInfo>? calls = null;

			foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
			{
				var f = FindMetricsFields(classDecl, semanticModel, cancellationToken);
				if (f.Count == 0)
					continue;

				var c = FindMetricsCalls(classDecl, f, semanticModel, cancellationToken);
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

	// ─────────────────────────────────────────────────────────────────────────
	// Detection
	// ─────────────────────────────────────────────────────────────────────────

	internal static List<MetricsFieldInfo> FindMetricsFields(
		ClassDeclarationSyntax classDecl,
		SemanticModel semanticModel,
		CancellationToken cancellationToken
	)
	{
		var counterType = semanticModel.Compilation.GetTypeByMetadataName(
			Constants.Metrics.SystemDiagnostics.CounterMetadataName
		);
		var histogramType = semanticModel.Compilation.GetTypeByMetadataName(
			Constants.Metrics.SystemDiagnostics.HistogramMetadataName
		);
		var upDownCounterType = semanticModel.Compilation.GetTypeByMetadataName(
			Constants.Metrics.SystemDiagnostics.UpDownCounterMetadataName
		);

		var result = new List<MetricsFieldInfo>();

		foreach (var member in classDecl.Members.OfType<FieldDeclarationSyntax>())
		{
			foreach (var variable in member.Declaration.Variables)
			{
				cancellationToken.ThrowIfCancellationRequested();

				if (semanticModel.GetDeclaredSymbol(variable, cancellationToken) is not IFieldSymbol fieldSymbol)
					continue;

				var kind = ClassifyInstrumentType(fieldSymbol.Type, counterType, histogramType, upDownCounterType);
				if (kind is null)
					continue;

				result.Add(
					new MetricsFieldInfo(
						FieldName: fieldSymbol.Name,
						InstrumentKind: kind.Value,
						MeasurementTypeDisplayString: GetMeasurementType(fieldSymbol.Type),
						FieldDeclaration: member,
						PropertyDeclaration: null,
						TypeSymbol: fieldSymbol.Type
					)
				);
			}
		}

		// Properties
		foreach (var member in classDecl.Members.OfType<PropertyDeclarationSyntax>())
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (semanticModel.GetDeclaredSymbol(member, cancellationToken) is not IPropertySymbol propSymbol)
				continue;

			var kind = ClassifyInstrumentType(propSymbol.Type, counterType, histogramType, upDownCounterType);
			if (kind is null)
				continue;

			result.Add(
				new MetricsFieldInfo(
					FieldName: propSymbol.Name,
					InstrumentKind: kind.Value,
					MeasurementTypeDisplayString: GetMeasurementType(propSymbol.Type),
					FieldDeclaration: null,
					PropertyDeclaration: member,
					TypeSymbol: propSymbol.Type
				)
			);
		}

		return result;
	}

	static MetricsInstrumentKind? ClassifyInstrumentType(
		ITypeSymbol type,
		INamedTypeSymbol? counterType,
		INamedTypeSymbol? histogramType,
		INamedTypeSymbol? upDownCounterType
	)
	{
		if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
			return null;

		var constructedFrom = namedType.ConstructedFrom;

		if (counterType is not null && SymbolEqualityComparer.Default.Equals(constructedFrom, counterType))
			return MetricsInstrumentKind.Counter;

		if (histogramType is not null && SymbolEqualityComparer.Default.Equals(constructedFrom, histogramType))
			return MetricsInstrumentKind.Histogram;

		return upDownCounterType is null || !SymbolEqualityComparer.Default.Equals(constructedFrom, upDownCounterType)
			? null
			: MetricsInstrumentKind.UpDownCounter;
	}

	static string GetMeasurementType(ITypeSymbol type)
	{
		if (type is INamedTypeSymbol { IsGenericType: true } named && named.TypeArguments.Length > 0)
			return named.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

		return "long";
	}

	internal static List<MetricsCallInfo> FindMetricsCalls(
		ClassDeclarationSyntax classDecl,
		List<MetricsFieldInfo> metricsFields,
		SemanticModel _,
		CancellationToken cancellationToken
	)
	{
		var fieldNames = metricsFields.ToDictionary(f => f.FieldName, StringComparer.Ordinal);
		var result = new List<MetricsCallInfo>();

		foreach (var invocation in classDecl.DescendantNodes().OfType<InvocationExpressionSyntax>())
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
				continue;

			var receiverName = GetSimpleIdentifier(memberAccess.Expression);
			if (receiverName is null || !fieldNames.TryGetValue(receiverName, out var fieldInfo))
				continue;

			var methodName = memberAccess.Name.Identifier.Text;
			var isAdd = methodName == "Add";
			var isRecord = methodName == "Record";

			if (!isAdd && !isRecord)
				continue;

			var args = invocation.ArgumentList.Arguments;
			bool isAutoCounter = false;

			// Determine if this is an auto-counter (literal 1 on Counter/UpDownCounter)
			if (isAdd && fieldInfo.InstrumentKind == MetricsInstrumentKind.Counter && args.Count >= 1)
			{
				var firstArg = args[0].Expression;
				if (firstArg is LiteralExpressionSyntax lit && lit.Token.ValueText == "1")
					isAutoCounter = true;
			}

			result.Add(
				new MetricsCallInfo(
					Invocation: invocation,
					ReceiverFieldName: receiverName,
					InstrumentKind: isAutoCounter ? MetricsInstrumentKind.AutoCounter : fieldInfo.InstrumentKind,
					MeasurementTypeDisplayString: fieldInfo.MeasurementTypeDisplayString,
					MeasurementArgument: args.Count >= 1 ? args[0].Expression : null,
					IsAutoIncrement: isAutoCounter
				)
			);
		}

		return result;
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Method name assignment
	// ─────────────────────────────────────────────────────────────────────────

	static List<(MetricsCallInfo Call, string MethodName)> AssignMethodNames(
		List<MetricsCallInfo> calls,
		List<MetricsFieldInfo> fields
	)
	{
		// Assign a base method name per field, then deduplicate
		var fieldNameMap = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (var field in fields)
		{
			if (!fieldNameMap.ContainsKey(field.FieldName))
				fieldNameMap[field.FieldName] = DeriveMethodName(field);
		}

		return
		[
			.. calls.Select(c =>
				(c, fieldNameMap.TryGetValue(c.ReceiverFieldName, out var n) ? n : c.ReceiverFieldName)
			),
		];
	}

	internal static string DeriveMethodName(MetricsFieldInfo field)
	{
		// Strip leading underscores
		var raw = field.FieldName.TrimStart('_');

		// Strip recognised suffix (case-insensitive match at end of word)
		foreach (var suffix in InstrumentSuffixes)
		{
			if (raw.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) && raw.Length > suffix.Length)
			{
				raw = raw.Substring(0, raw.Length - suffix.Length);
				break;
			}
		}

		if (string.IsNullOrEmpty(raw))
			return field.FieldName.TrimStart('_');

		// PascalCase the resulting words
		var words = SplitIntoWords(raw);
		return words.Length > 0 ? string.Concat(words.Select(ToPascalCaseWord)) : ToPascalCaseWord(raw);
	}

	static string[] SplitIntoWords(string name)
	{
		var parts = WordSplitterRegex.Split(name);
		var words = new List<string>();
		foreach (var part in parts)
		{
			if (string.IsNullOrEmpty(part))
				continue;
			var subParts = CamelCaseSplitRegex.Split(part);
			words.AddRange(subParts.Where(w => !string.IsNullOrEmpty(w)));
		}

		return [.. words];
	}

	static string ToPascalCaseWord(string word)
	{
		if (string.IsNullOrEmpty(word))
			return word;

		if (word.Length > 1 && word.All(char.IsUpper))
#pragma warning disable CA1308
			return char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant();
#pragma warning restore CA1308

		return char.ToUpperInvariant(word[0]) + word.Substring(1);
	}

	internal static List<(MetricsCallInfo Call, string MethodName)> AssignMethodNamesInternal(
		List<MetricsCallInfo> calls,
		List<MetricsFieldInfo> fields
	) => AssignMethodNames(calls, fields);

	internal static string BuildMetricsAttributeInternal(MetricsCallInfo call) => BuildMetricsAttribute(call);

	internal static string BuildParamListInternal(MetricsCallInfo call) => BuildParamList(call);

	internal static ClassDeclarationSyntax RewriteClassInternal(
		ClassDeclarationSyntax classDecl,
		List<MetricsFieldInfo> metricsFields,
		List<(MetricsCallInfo Call, string MethodName)> callsWithMethods,
		string interfaceName,
		SemanticModel semanticModel
	) => RewriteClass(classDecl, metricsFields, callsWithMethods, interfaceName, semanticModel);

	internal static string BuildInterfaceMembers(List<(MetricsCallInfo Call, string MethodName)> callsWithMethods)
	{
		var sb = new StringBuilder();
		var emitted = new HashSet<string>(StringComparer.Ordinal);

		foreach (var (call, methodName) in callsWithMethods)
		{
			var sigKey = $"{methodName}|{call.IsAutoIncrement}";
			if (!emitted.Add(sigKey))
				continue;

			var attribute = BuildMetricsAttribute(call);
			var paramList = BuildParamList(call);

			sb.AppendLine($"\t[{attribute}]");
			sb.AppendLine($"\tvoid {methodName}({paramList});");
			sb.AppendLine();
		}

		return sb.ToString();
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Interface code generation
	// ─────────────────────────────────────────────────────────────────────────

	internal static string BuildInterfaceCode(
		string interfaceName,
		List<(MetricsCallInfo Call, string MethodName)> callsWithMethods,
		List<MetricsFieldInfo> _
	)
	{
		var sb = new StringBuilder();

		sb.AppendLine($"[{Constants.Metrics.MeterAttributeShortName}]");
		sb.AppendLine($"public interface {interfaceName}");
		sb.AppendLine("{");
		sb.Append(BuildInterfaceMembers(callsWithMethods));
		sb.AppendLine("}");

		return sb.ToString();
	}

	static string BuildMetricsAttribute(MetricsCallInfo call) =>
		call.InstrumentKind switch
		{
			MetricsInstrumentKind.AutoCounter => Constants.Metrics.AutoCounterAttributeShortName,
			MetricsInstrumentKind.Counter => Constants.Metrics.CounterAttributeShortName,
			MetricsInstrumentKind.Histogram => Constants.Metrics.HistogramAttributeShortName,
			MetricsInstrumentKind.UpDownCounter => Constants.Metrics.UpDownCounterAttributeShortName,
			_ => Constants.Metrics.CounterAttributeShortName,
		};

	static string BuildParamList(MetricsCallInfo call)
	{
		if (call.IsAutoIncrement)
			return string.Empty;

		var measurementType = call.MeasurementTypeDisplayString;
		var baseParam = $"{measurementType} value";

		// Collect tag parameters (args after the first value argument)
		var tagArgs = call.Invocation.ArgumentList.Arguments.Skip(1).ToList();
		if (tagArgs.Count == 0)
			return baseParam;

		var tagParams = tagArgs.Select((_, i) => $"string tag{i + 1}");
		return baseParam + ", " + string.Join(", ", tagParams);
	}

	static InterfaceDeclarationSyntax ParseInterfaceNode(string code)
	{
		var tree = CSharpSyntaxTree.ParseText(code);
		var root = tree.GetCompilationUnitRoot();

		return root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().FirstOrDefault()
			?? throw new InvalidOperationException("Could not parse generated interface from: " + code);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Class rewriting
	// ─────────────────────────────────────────────────────────────────────────

	static ClassDeclarationSyntax RewriteClass(
		ClassDeclarationSyntax classDecl,
		List<MetricsFieldInfo> metricsFields,
		List<(MetricsCallInfo Call, string MethodName)> callsWithMethods,
		string interfaceName,
		SemanticModel semanticModel
	)
	{
		// Map each invocation to its replacement
		var invocationMap = new Dictionary<InvocationExpressionSyntax, InvocationExpressionSyntax>(
			SyntaxNodeReferenceComparer<InvocationExpressionSyntax>.Instance
		);

		foreach (var (call, methodName) in callsWithMethods)
		{
			var newInvocation = RewriteInvocation(call, methodName);
			if (!invocationMap.ContainsKey(call.Invocation))
				invocationMap[call.Invocation] = newInvocation;
		}

		// Map field declarations
		var fieldMap = new Dictionary<FieldDeclarationSyntax, FieldDeclarationSyntax>(
			SyntaxNodeReferenceComparer<FieldDeclarationSyntax>.Instance
		);
		foreach (var field in metricsFields)
		{
			if (field.FieldDeclaration is null)
				continue;

			fieldMap[field.FieldDeclaration] = RewriteFieldDeclaration(field.FieldDeclaration, interfaceName);
		}

		// Map property declarations
		var propertyMap = new Dictionary<PropertyDeclarationSyntax, PropertyDeclarationSyntax>(
			SyntaxNodeReferenceComparer<PropertyDeclarationSyntax>.Instance
		);
		foreach (var field in metricsFields)
		{
			if (field.PropertyDeclaration is null)
				continue;

			propertyMap[field.PropertyDeclaration] = RewritePropertyDeclaration(
				field.PropertyDeclaration,
				interfaceName
			);
		}

		// Map constructor/method parameters
		var paramMap = BuildParamRewriteMap(classDecl, metricsFields, interfaceName, semanticModel);

		return classDecl.ReplaceNodes(
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
	}

	static InvocationExpressionSyntax RewriteInvocation(MetricsCallInfo call, string methodName)
	{
		var memberAccess = (MemberAccessExpressionSyntax)call.Invocation.Expression;
		var newMemberAccess = memberAccess.WithName(SyntaxFactory.IdentifierName(methodName));

		if (call.IsAutoIncrement)
		{
			// AutoCounter: remove all arguments
			return call
				.Invocation.WithExpression(newMemberAccess)
				.WithArgumentList(SyntaxFactory.ArgumentList().WithTriviaFrom(call.Invocation.ArgumentList));
		}

		// Keep all original arguments unchanged
		return call.Invocation.WithExpression(newMemberAccess);
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
		List<MetricsFieldInfo> metricsFields,
		string interfaceName,
		SemanticModel semanticModel
	)
	{
		var result = new Dictionary<ParameterSyntax, ParameterSyntax>(
			SyntaxNodeReferenceComparer<ParameterSyntax>.Instance
		);

		var fieldTypeNames = new HashSet<string>(
			metricsFields.Select(f => f.TypeSymbol.ToDisplayString()),
			StringComparer.Ordinal
		);
		var fieldTypeShortNames = new HashSet<string>(
			metricsFields.Select(f => f.TypeSymbol.Name),
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

			bool matched;
			try
			{
				var typeInfo = semanticModel.GetTypeInfo(param.Type);
				var type = typeInfo.Type;
				matched = type is not null && fieldTypeNames.Contains(type.ToDisplayString());
			}
			catch (ArgumentException)
			{
				var typeName = param.Type.ToString().TrimEnd('?');
				matched = fieldTypeShortNames.Contains(typeName) || fieldTypeNames.Contains(typeName);
			}

			if (!matched)
				continue;

			result[param] = param.WithType(SyntaxFactory.IdentifierName(interfaceName).WithTriviaFrom(param.Type));
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
