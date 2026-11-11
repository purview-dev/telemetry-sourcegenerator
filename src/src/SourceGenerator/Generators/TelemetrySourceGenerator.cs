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
				ReportableDiagnostic.Create(
					DiagnosticLibrary.General.FatalExecutionDuringExecution.Descriptor,
					isBlocking: true,
					ex.ToString()
				)
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
		>(context, BuildCapabilities, PropertyLibrary.DisableTelemetryGenerator);

		// Create shared providers so Activities/Metrics pipelines aren't registered twice.
		var activityProvider = IncrementalPipeline
			.ForAttributeWithMetadataName(
				context,
				TypeLibrary.Purview.Telemetry.ActivitySourceAttribute,
				transform: static (ctx, cancellationToken) =>
					PipelineHelpers.BuildActivityTransform(ctx, cancellationToken),
				predicate: static (node, token) => PipelineHelpers.HasActivityTargetAttribute(node, token),
				trackingName: $"{nameof(TelemetrySourceGenerator)}_Activities"
			)
			.Where(static m => m.HasValue);

		var meterProvider = IncrementalPipeline
			.ForAttributeWithMetadataName(
				context,
				TypeLibrary.Purview.Telemetry.MeterAttribute,
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

		// Capabilities are probed on the compilation, never on the target framework: the
		// System.Diagnostics.DiagnosticSource and Microsoft.Extensions.Diagnostics.Abstractions packages bring these
		// types to net462 and netstandard2.0 consumers, and a net10.0 consumer that pins an older line may not have
		// them either. IMeterFactory in particular is not ".NET 8+" in practice — it is whatever the referenced
		// package provides, so a symbol probe is the only correct gate.
		return new(
			//SupportsNullableAnnotations: parseOptions is null
			//|| parseOptions.LanguageVersion >= LanguageVersion.CSharp8,
			SupportsIMeterFactory: SharedHelpers.IsMeterFactoryAvailable(compilation),
			UseActivitySourceFactory: SharedHelpers.UseActivitySourceFactory(compilation, token),
			SupportsGauge: SharedHelpers.IsTypeAvailable(compilation, TypeLibrary.System.Diagnostics.Metrics.Gauge),
			SupportsInstrumentAdvice: SharedHelpers.IsTypeAvailable(
				compilation,
				TypeLibrary.System.Diagnostics.Metrics.InstrumentAdvice
			),
			SupportsActivityAddException: SharedHelpers.IsActivityAddExceptionAvailable(compilation),
			// Resolved once per compilation and carried on the capabilities, so every meter and activity source in
			// the project reuses one value instead of reading the assembly identity per target.
			AssemblyVersion: SharedHelpers.GetAssemblyVersion(compilation)
		);
	}
}
