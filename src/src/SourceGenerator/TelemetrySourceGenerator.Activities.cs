using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Emitters;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

partial class TelemetrySourceGenerator
{
	static void RegisterActivitiesGeneration(
		IncrementalGeneratorInitializationContext context,
		IncrementalValuesProvider<GeneratorResult<ActivitySourceTarget?>> activityTargets,
		IncrementalValueProvider<bool> supportsNullableAnnotations,
		IncrementalValueProvider<GenerationContext<TelemetryCapabilities>> generationContext
	)
	{
		context.RegisterImplementationSourceOutput(
			source: activityTargets.Collect().Combine(supportsNullableAnnotations).Combine(generationContext),
			action: (spc, pair) => GenerateActivitiesTargets(pair.Left.Left, pair.Left.Right, pair.Right.Logger, spc)
		);
	}

	static void GenerateActivitiesTargets(
		ImmutableArray<GeneratorResult<ActivitySourceTarget?>> targets,
		bool emitNullable,
		ISourceGenLogger? logger,
		SourceProductionContext spc
	)
	{
		if (targets.Length == 0)
			return;

		foreach (var result in targets)
		{
			if (!result.ShouldProcess || result.Value is not { } target)
				continue;

			logger?.Debug($"Activity generation target: {target.FullyQualifiedName}");

			RunSafely(
				spc,
				() => ActivitySourceTargetClassEmitter.GenerateImplementation(target, spc, logger, emitNullable)
			);
		}
	}
}
