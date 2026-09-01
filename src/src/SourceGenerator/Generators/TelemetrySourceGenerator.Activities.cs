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
		context.RegisterSourceOutput(
			source: activityTargets.Combine(generationContext),
			action: (spc, pair) =>
			{
				var (result, genContext) = pair;

				if (!result.ShouldProcess || result.Value is not { } target)
					return;

				var logger = genContext.Logger;
				logger?.Debug($"Activity generation target: {target.FullyQualifiedName}");

				RunSafely(
					spc,
					() =>
						ActivitySourceTargetClassEmitter.GenerateImplementation(
							target,
							spc,
							logger,
							genContext.Capabilities.SupportsNullableAnnotations
						)
				);
			}
		);
	}
}
