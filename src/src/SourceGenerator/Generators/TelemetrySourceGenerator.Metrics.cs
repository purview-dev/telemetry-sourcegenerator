using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Emitters;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

partial class TelemetrySourceGenerator
{
	static void RegisterMetricsGeneration(
		IncrementalGeneratorInitializationContext context,
		IncrementalValuesProvider<GeneratorResult<MeterTarget?>> meterTargets,
		IncrementalValueProvider<GenerationContext<TelemetryCapabilities>> generationContext
	)
	{
		var outputContexts = meterTargets
			.Where(static m => m.ShouldProcess)
			.Combine(generationContext)
			.Select(static (pair, _) => new MeterOutputContext(pair.Left.Value!, pair.Right))
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_MeterOutputs");

		context.RegisterSourceOutput(
			source: outputContexts,
			action: static (spc, output) =>
			{
				output.Context.Debug($"Meter generation target: {output.Target.FullyQualifiedName}");
				RunSafely(spc, () => MeterTargetClassEmitter.GenerateImplementation(output, spc));
			}
		);
	}
}
