using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Emitters;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

partial class TelemetrySourceGenerator
{
	static void RegisterMetricsGeneration(
		IncrementalGeneratorInitializationContext context,
		IncrementalValuesProvider<MeterTarget?> meterTargets,
		GenerationLogger? logger
	)
	{
		context.RegisterImplementationSourceOutput(
			source: meterTargets.Collect(),
			action: (spc, source) => GenerateMeterTargets(source, spc, logger)
		);
	}

	static void GenerateMeterTargets(
		ImmutableArray<MeterTarget?> targets,
		SourceProductionContext spc,
		GenerationLogger? logger
	)
	{
		if (targets.Length == 0)
			return;

		foreach (var target in targets)
		{
			logger?.Debug($"Meter generation target: {target!.FullyQualifiedName}");

			MeterTargetClassEmitter.GenerateImplementation(target!, spc, logger);
		}
	}
}
