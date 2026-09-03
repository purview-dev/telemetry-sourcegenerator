using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Purview.Telemetry.SourceGenerator.Emitters;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

[Generator]
public sealed partial class TelemetrySourceGenerator : IIncrementalGenerator
{
	/// <summary>
	/// Executes a generation action, reporting <c>TSG1000</c> if an unexpected exception escapes. This is
	/// the one diagnostic the generator raises directly (an execution-failure safety net, not a validation
	/// diagnostic — the analyzer owns those).
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Design",
		"CA1031:Do not catch general exception types",
		Justification = "The generator must not fail the build; unexpected exceptions are surfaced as TSG1000."
	)]
	static void RunSafely(SourceProductionContext spc, Action generate)
	{
		try
		{
			generate();
		}
		catch (Exception ex)
		{
			spc.ReportDiagnostic(
				DiagnosticInfo.Create(DiagnosticLibrary.General.FatalExecutionDuringExecution.Descriptor, ex.ToString())
			);
		}
	}

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// RegisterPostInitializationOutput (not RegisterSourceOutput) ensures ForAttributeWithMetadataName
		// can resolve attribute types before it runs.
		context.RegisterPostInitializationOutput(GeneratedTypesEmitter.EmitAll);

		// The generation context carries the framework logging sink, settings and the
		// compilation-level capabilities (nullable annotations, IMeterFactory availability).
		var generationContext = IncrementalPipeline.GenerationContextValueProvider<
			TelemetryCapabilities,
			TelemetrySourceGenerator
		>(context, BuildCapabilities, null);

		// Create shared providers so Activities/Metrics pipelines aren't registered twice.
		var activityProvider = IncrementalPipeline
			.ForAttributeWithMetadataName(
				context,
				TypeLibrary.Activities.ActivitySourceAttribute,
				transform: static (ctx, cancellationToken) =>
					PipelineHelpers.BuildActivityTransform(ctx, cancellationToken),
				predicate: static (node, token) => PipelineHelpers.HasActivityTargetAttribute(node, token),
				trackingName: $"{nameof(TelemetrySourceGenerator)}_Activities"
			)
			.Where(static m => m.HasValue);

		var meterProvider = IncrementalPipeline
			.ForAttributeWithMetadataName(
				context,
				TypeLibrary.Metrics.MeterAttribute,
				transform: static (ctx, cancellationToken) =>
					PipelineHelpers.BuildMeterTransform(ctx, cancellationToken),
				predicate: static (node, token) => PipelineHelpers.HasMeterTargetAttribute(node, token),
				trackingName: $"{nameof(TelemetrySourceGenerator)}_Meters"
			)
			.Where(static m => m.HasValue);

		RegisterActivitiesGeneration(context, activityProvider, generationContext);
		RegisterLoggerGeneration(context, generationContext);
		RegisterMetricsGeneration(context, meterProvider, generationContext);
		RegisterTelemetryNamesGeneration(context, activityProvider, meterProvider, generationContext);
	}

	static TelemetryCapabilities BuildCapabilities(
		Compilation compilation,
		GenerationSettings settings,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		// C# 8+ feature detection: controls nullable annotations and null-forgiving operators.
		// IMeterFactory is .NET 8+ only — never available on .NET Framework 4.8.
		var parseOptions = compilation.SyntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions;
		return new(
			//SupportsNullableAnnotations: parseOptions is null
			//|| parseOptions.LanguageVersion >= LanguageVersion.CSharp8,
			SupportsIMeterFactory: parseOptions is null
				|| !parseOptions.PreprocessorSymbolNames.Contains("NET48_OR_GREATER")
		);
	}
}
