using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Emitters;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

partial class TelemetrySourceGenerator
{
	static void RegisterActivitiesGeneration(
		IncrementalGeneratorInitializationContext context,
		IncrementalValuesProvider<ActivitySourceTarget?> activityTargets,
		IncrementalValueProvider<bool> supportsNullableAnnotations,
		GenerationLogger? logger
	)
	{
		context.RegisterImplementationSourceOutput(
			source: activityTargets.Collect().Combine(supportsNullableAnnotations),
			action: (spc, pair) => GenerateActivitiesTargets(pair.Left, pair.Right, spc, logger)
		);
	}

	static void GenerateActivitiesTargets(
		ImmutableArray<ActivitySourceTarget?> targets,
		bool emitNullable,
		SourceProductionContext spc,
		GenerationLogger? logger
	)
	{
		if (targets.Length == 0)
			return;

		foreach (var target in targets)
		{
			logger?.Debug($"Activity generation target: {target!.FullyQualifiedName}");

			ActivitySourceTargetClassEmitter.GenerateImplementation(target!, spc, logger, emitNullable);
		}
	}
}
