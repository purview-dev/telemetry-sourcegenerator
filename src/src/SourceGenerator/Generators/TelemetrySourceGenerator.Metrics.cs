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
		context.RegisterSourceOutput(
			source: meterTargets.Combine(generationContext),
			action: (spc, pair) =>
			{
				var (result, genContext) = pair;

				if (!result.ShouldProcess || result.Value is not { } target)
					return;

				var logger = genContext.Logger;
				logger?.Debug($"Meter generation target: {target.FullyQualifiedName}");

				RunSafely(
					spc,
					() =>
						MeterTargetClassEmitter.GenerateImplementation(
							target,
							spc,
							logger,
							genContext.Capabilities.SupportsNullableAnnotations,
							genContext.Capabilities.SupportsIMeterFactory
						)
				);
			}
		);
	}
}
