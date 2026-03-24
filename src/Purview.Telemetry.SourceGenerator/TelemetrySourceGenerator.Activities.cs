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
		GenerationLogger? logger
	)
	{
		context.RegisterImplementationSourceOutput(
			source: activityTargets.Collect(),
			action: (spc, source) => GenerateActivitiesTargets(source, spc, logger)
		);
	}

	static void GenerateActivitiesTargets(
		ImmutableArray<ActivitySourceTarget?> targets,
		SourceProductionContext spc,
		GenerationLogger? logger
	)
	{
		if (targets.Length == 0)
			return;

		foreach (var target in targets)
		{
			logger?.Debug($"Activity generation target: {target!.FullyQualifiedName}");

			ActivitySourceTargetClassEmitter.GenerateImplementation(target!, spc, logger);
		}
	}
}
