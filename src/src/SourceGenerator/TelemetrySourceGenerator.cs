using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Purview.Telemetry.SourceGenerator.Helpers;

namespace Purview.Telemetry.SourceGenerator;

[Generator]
public sealed partial class TelemetrySourceGenerator : IIncrementalGenerator
{
	/// <summary>
	/// Executes a generation action, reporting <c>TSG1000</c> if an unexpected exception escapes. This is
	/// the one diagnostic the generator raises directly (an execution-failure safety net, not a validation
	/// diagnostic — the analyzer owns those).
	/// </summary>
	static void RunSafely(SourceProductionContext spc, Action generate)
	{
		try
		{
			generate();
		}
		catch (Exception ex)
		{
			spc.ReportDiagnostic(
				DiagnosticInfo.Create(
					TelemetryRules.ToDescriptor(DiagnosticLibrary.General.FatalExecutionDuringExecution),
					ex.ToString()
				)
			);
		}
	}

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// C# 8+ feature detection: controls nullable annotations and null-forgiving operators.
		var supportsNullableAnnotations = context.ParseOptionsProvider.Select(
			static (opts, _) =>
				opts is not CSharpParseOptions csOpts || csOpts.LanguageVersion >= LanguageVersion.CSharp8
		);

		// IMeterFactory is .NET 8+ only — never available on .NET Framework 4.8.
		var supportsIMeterFactory = context.ParseOptionsProvider.Select(
			static (opts, _) =>
				opts is not CSharpParseOptions csOpts || !csOpts.PreprocessorSymbolNames.Contains("NET48_OR_GREATER")
		);

		// Only .NET 8+ and .NET Framework 4.8+ are supported; fail fast for anything else.
		// A compilation with no target-framework symbols at all (e.g. an in-memory test
		// compilation) is treated as .NET 8+. The diagnostic is raised by the analyzer.

		// RegisterPostInitializationOutput (not RegisterSourceOutput) ensures ForAttributeWithMetadataName
		// can resolve attribute types before it runs.
		context.RegisterPostInitializationOutput(ctx =>
		{
			// Adds Microsoft.CodeAnalysis.EmbeddedAttribute to the compilation so generated marker
			// types (decorated with [Microsoft.CodeAnalysis.Embedded]) are invisible to downstream
			// assemblies, preventing CS0436 conflicts when multiple projects reference this generator.
			ctx.AddEmbeddedAttributeDefinition();

			foreach (var template in TemplateLibrary.GetAllTemplates())
				ctx.AddSource(template.GetGeneratedFilename(), template.TemplateData);
		});

		// The generation context carries the framework logging sink, settings and capabilities.
		var generationContext = IncrementalPipeline.GenerationContextValueProvider<
			TelemetryCapabilities,
			TelemetrySourceGenerator
		>(context, static (_, _, _, _) => TelemetryCapabilities.Instance, null);

		// Create shared providers so Activities/Metrics pipelines aren't registered twice.
		var activityProvider = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				TemplateLibrary.Activities.ActivitySourceAttribute.TypeInfo.MetadataFullName,
				static (node, token) => PipelineHelpers.HasActivityTargetAttribute(node, token),
				static (ctx, cancellationToken) => PipelineHelpers.BuildActivityTransform(ctx, null, cancellationToken)
			)
			.Where(static m => m.HasValue)
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_Activities");

		var meterProvider = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				TemplateLibrary.Metrics.MeterAttribute.TypeInfo.MetadataFullName,
				static (node, token) => PipelineHelpers.HasMeterTargetAttribute(node, token),
				static (ctx, cancellationToken) => PipelineHelpers.BuildMeterTransform(ctx, null, cancellationToken)
			)
			.Where(static m => m.HasValue)
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_Meters");

		RegisterActivitiesGeneration(context, activityProvider, supportsNullableAnnotations, generationContext);
		RegisterLoggerGeneration(context, supportsNullableAnnotations, supportsIMeterFactory, generationContext);
		RegisterMetricsGeneration(
			context,
			meterProvider,
			supportsNullableAnnotations,
			supportsIMeterFactory,
			generationContext
		);
		RegisterTelemetryNamesGeneration(
			context,
			activityProvider,
			meterProvider,
			supportsNullableAnnotations,
			generationContext
		);
	}
}
