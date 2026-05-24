using System.Composition;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.Telemetry.SourceGenerator.Refactorings;

/// <summary>
/// Converts any combination of <c>ILogger</c>, <c>ActivitySource</c>, and metrics instruments
/// (<c>Counter&lt;T&gt;</c>, <c>Histogram&lt;T&gt;</c>, <c>UpDownCounter&lt;T&gt;</c>)
/// into a single <c>I{ClassName}Telemetry</c> interface decorated with
/// <c>[Logger]</c>, <c>[ActivitySource]</c>, and/or <c>[Meter]</c> as appropriate.
/// </summary>
[ExportCodeRefactoringProvider(
	LanguageNames.CSharp,
	Name = nameof(ConvertAllTelemetryToInterfaceRefactoringProvider)
)]
[Shared]
public sealed class ConvertAllTelemetryToInterfaceRefactoringProvider : CodeRefactoringProvider
{
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

		var loggerFields = ConvertILoggerToTelemetryRefactoringProvider.FindILoggerFields(
			classDecl,
			semanticModel,
			context.CancellationToken
		);
		var activityFields =
			ConvertActivitySourceToTelemetryRefactoringProvider.FindActivitySourceFields(
				classDecl,
				semanticModel,
				context.CancellationToken
			);
		var metricsFields = ConvertMetricsToTelemetryRefactoringProvider.FindMetricsFields(
			classDecl,
			semanticModel,
			context.CancellationToken
		);

		if (loggerFields.Count == 0 && activityFields.Count == 0 && metricsFields.Count == 0)
			return;

		var logCalls =
			loggerFields.Count > 0
				? ConvertILoggerToTelemetryRefactoringProvider.FindLogCalls(
					classDecl,
					loggerFields,
					semanticModel,
					context.CancellationToken
				)
				: [];

		var activityCalls =
			activityFields.Count > 0
				? ConvertActivitySourceToTelemetryRefactoringProvider.FindActivityCalls(
					classDecl,
					activityFields,
					semanticModel,
					context.CancellationToken
				)
				: [];

		var metricsCalls =
			metricsFields.Count > 0
				? ConvertMetricsToTelemetryRefactoringProvider.FindMetricsCalls(
					classDecl,
					metricsFields,
					semanticModel,
					context.CancellationToken
				)
				: [];

		if (logCalls.Count == 0 && activityCalls.Count == 0 && metricsCalls.Count == 0)
			return;

		var className = classDecl.Identifier.ValueText;
		var doc = context.Document;
		context.RegisterRefactoring(
			CodeAction.Create(
				$"Convert all telemetry to I{className}Telemetry",
				nestedActions:
				[
					CodeAction.Create(
						"In this class",
						ct =>
							ConvertAsync(
								doc,
								classDecl,
								loggerFields,
								logCalls,
								activityFields,
								activityCalls,
								metricsFields,
								metricsCalls,
								semanticModel,
								ct
							),
						equivalenceKey: "Purview.Telemetry.ConvertAllTelemetryToInterface.Class"
					),
					CodeAction.Create(
						"In this document",
						ct => ConvertDocumentAsync(doc, ct),
						equivalenceKey: "Purview.Telemetry.ConvertAllTelemetryToInterface.Document"
					),
					CodeAction.Create(
						"In this project",
						ct => ConvertProjectAsync(doc.Project, ct),
						equivalenceKey: "Purview.Telemetry.ConvertAllTelemetryToInterface.Project"
					),
					CodeAction.Create(
						"In this solution",
						ct => ConvertSolutionAsync(doc.Project.Solution, ct),
						equivalenceKey: "Purview.Telemetry.ConvertAllTelemetryToInterface.Solution"
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
		List<ActivitySourceFieldInfo> activityFields,
		List<ActivitySourceCallInfo> activityCalls,
		List<MetricsFieldInfo> metricsFields,
		List<MetricsCallInfo> metricsCalls,
		SemanticModel semanticModel,
		CancellationToken cancellationToken
	)
	{
		var className = classDecl.Identifier.ValueText;
		var interfaceName = "I" + className + "Telemetry";

		var logCallsWithMethods =
			logCalls.Count > 0
				? ConvertILoggerToTelemetryRefactoringProvider.AssignMethodNames(logCalls)
				: [];

		var activityCallsWithMethods =
			activityCalls.Count > 0
				? ConvertActivitySourceToTelemetryRefactoringProvider.AssignMethodNamesInternal(
					activityCalls
				)
				: [];

		var metricsCallsWithMethods =
			metricsCalls.Count > 0
				? ConvertMetricsToTelemetryRefactoringProvider.AssignMethodNamesInternal(
					metricsCalls,
					metricsFields
				)
				: [];

		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		if (root is null)
			return document;

		var interfaceCode = BuildCombinedInterfaceCode(
			interfaceName,
			logCallsWithMethods,
			activityCallsWithMethods,
			metricsCallsWithMethods
		);
		var interfaceSyntax = ParseInterfaceNode(interfaceCode);

		var rewrittenClass = classDecl;

		if (loggerFields.Count > 0 && logCallsWithMethods.Count > 0)
		{
			rewrittenClass = ConvertILoggerToTelemetryRefactoringProvider.RewriteClass(
				rewrittenClass,
				loggerFields,
				logCallsWithMethods,
				interfaceName,
				semanticModel
			);
		}

		if (activityFields.Count > 0 && activityCallsWithMethods.Count > 0)
		{
			rewrittenClass =
				ConvertActivitySourceToTelemetryRefactoringProvider.RewriteClassInternal(
					rewrittenClass,
					activityFields,
					activityCallsWithMethods,
					interfaceName,
					semanticModel
				);
		}

		if (metricsFields.Count > 0 && metricsCallsWithMethods.Count > 0)
		{
			rewrittenClass = ConvertMetricsToTelemetryRefactoringProvider.RewriteClassInternal(
				rewrittenClass,
				metricsFields,
				metricsCallsWithMethods,
				interfaceName,
				semanticModel
			);
		}

		var newRoot = root.ReplaceNode(classDecl, rewrittenClass);
		var replacedClass = newRoot
			.DescendantNodes()
			.OfType<ClassDeclarationSyntax>()
			.First(c => c.Identifier.ValueText == className);

		newRoot = newRoot.InsertNodesBefore(
			replacedClass,
			[interfaceSyntax.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)]
		);

		var compilationRoot = (CompilationUnitSyntax)newRoot;
		if (
			!compilationRoot.Usings.Any(u =>
				u.Name?.ToString() == Constants.PurviewTelemetryNamespace
			)
		)
		{
			var newUsing = SyntaxFactory
				.UsingDirective(SyntaxFactory.ParseName(Constants.PurviewTelemetryNamespace))
				.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
			newRoot = compilationRoot.AddUsings(newUsing);
			compilationRoot = (CompilationUnitSyntax)newRoot;
		}

		if (
			activityFields.Count > 0
			&& !compilationRoot.Usings.Any(u =>
				u.Name?.ToString() == Constants.SystemDiagnosticsNamespace
			)
		)
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
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			if (root is null)
				break;

			var semanticModel = await document
				.GetSemanticModelAsync(cancellationToken)
				.ConfigureAwait(false);
			if (semanticModel is null)
				break;

			ClassDeclarationSyntax? targetClass = null;
			List<ILoggerFieldInfo>? logFields = null;
			List<LogCallInfo>? logCalls = null;
			List<ActivitySourceFieldInfo>? actFields = null;
			List<ActivitySourceCallInfo>? actCalls = null;
			List<MetricsFieldInfo>? metFields = null;
			List<MetricsCallInfo>? metCalls = null;

			foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
			{
				var lf = ConvertILoggerToTelemetryRefactoringProvider.FindILoggerFields(
					classDecl,
					semanticModel,
					cancellationToken
				);
				var af =
					ConvertActivitySourceToTelemetryRefactoringProvider.FindActivitySourceFields(
						classDecl,
						semanticModel,
						cancellationToken
					);
				var mf = ConvertMetricsToTelemetryRefactoringProvider.FindMetricsFields(
					classDecl,
					semanticModel,
					cancellationToken
				);

				if (lf.Count == 0 && af.Count == 0 && mf.Count == 0)
					continue;

				var lc =
					lf.Count > 0
						? ConvertILoggerToTelemetryRefactoringProvider.FindLogCalls(
							classDecl,
							lf,
							semanticModel,
							cancellationToken
						)
						: [];
				var ac =
					af.Count > 0
						? ConvertActivitySourceToTelemetryRefactoringProvider.FindActivityCalls(
							classDecl,
							af,
							semanticModel,
							cancellationToken
						)
						: [];
				var mc =
					mf.Count > 0
						? ConvertMetricsToTelemetryRefactoringProvider.FindMetricsCalls(
							classDecl,
							mf,
							semanticModel,
							cancellationToken
						)
						: [];

				if (lc.Count == 0 && ac.Count == 0 && mc.Count == 0)
					continue;

				targetClass = classDecl;
				logFields = lf;
				logCalls = lc;
				actFields = af;
				actCalls = ac;
				metFields = mf;
				metCalls = mc;
				break;
			}

			if (targetClass is null)
				break;

			document = await ConvertAsync(
					document,
					targetClass,
					logFields!,
					logCalls!,
					actFields!,
					actCalls!,
					metFields!,
					metCalls!,
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

	static string BuildCombinedInterfaceCode(
		string interfaceName,
		List<(LogCallInfo Call, string MethodName)> logCallsWithMethods,
		List<(ActivitySourceCallInfo Call, string MethodName)> activityCallsWithMethods,
		List<(MetricsCallInfo Call, string MethodName)> metricsCallsWithMethods
	)
	{
		var sb = new StringBuilder();

		if (activityCallsWithMethods.Count > 0)
			sb.AppendLine($"[{Constants.Activities.ActivitySourceAttributeShortName}]");

		if (logCallsWithMethods.Count > 0)
			sb.AppendLine($"[{Constants.Logging.LoggerAttributeShortName}]");

		if (metricsCallsWithMethods.Count > 0)
			sb.AppendLine($"[{Constants.Metrics.MeterAttributeShortName}]");

		sb.AppendLine($"public interface {interfaceName}");
		sb.AppendLine("{");

		if (activityCallsWithMethods.Count > 0)
			sb.Append(
				ConvertActivitySourceToTelemetryRefactoringProvider.BuildInterfaceMembers(
					activityCallsWithMethods
				)
			);

		if (logCallsWithMethods.Count > 0)
			sb.Append(
				ConvertILoggerToTelemetryRefactoringProvider.BuildInterfaceMembers(
					logCallsWithMethods
				)
			);

		if (metricsCallsWithMethods.Count > 0)
			sb.Append(
				ConvertMetricsToTelemetryRefactoringProvider.BuildInterfaceMembers(
					metricsCallsWithMethods
				)
			);

		sb.AppendLine("}");

		return sb.ToString();
	}

	static InterfaceDeclarationSyntax ParseInterfaceNode(string code)
	{
		var tree = CSharpSyntaxTree.ParseText(code);
		var root = tree.GetCompilationUnitRoot();

		return root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().FirstOrDefault()
			?? throw new InvalidOperationException(
				"Could not parse generated interface from: " + code
			);
	}
}
