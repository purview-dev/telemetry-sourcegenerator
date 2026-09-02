using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Emitters;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

partial class TelemetrySourceGenerator
{
	static void RegisterActivitiesGeneration(
		IncrementalGeneratorInitializationContext context,
		IncrementalValuesProvider<GeneratorResult<ActivitySourceTarget?>> activityTargets,
		IncrementalValueProvider<GenerationContext<TelemetryCapabilities>> generationContext
	)
	{
		var outputContexts = activityTargets
			.Where(static m => m.ShouldProcess)
			.Combine(generationContext)
			.Select(static (pair, _) => new ActivityOutputContext(pair.Left.Value!, pair.Right))
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_ActivityOutputs");

		context.RegisterSourceOutput(
			source: outputContexts,
			action: static (spc, output) =>
			{
				output.Context.Debug($"Activity generation target: {output.Target.FullyQualifiedName}");

				RunSafely(spc, () => ActivitySourceTargetClassEmitter.GenerateImplementation(output, spc));
			}
		);
	}
}
