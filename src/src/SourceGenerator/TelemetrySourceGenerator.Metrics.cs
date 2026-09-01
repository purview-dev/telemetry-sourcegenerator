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
		IncrementalValuesProvider<GeneratorResult<MeterTarget?>> meterTargets,
		IncrementalValueProvider<bool> supportsNullableAnnotations,
		IncrementalValueProvider<bool> supportsIMeterFactory,
		IncrementalValueProvider<GenerationContext<TelemetryCapabilities>> generationContext
	)
	{
		context.RegisterImplementationSourceOutput(
			source: meterTargets
				.Collect()
				.Combine(supportsNullableAnnotations.Combine(supportsIMeterFactory))
				.Combine(generationContext),
			action: (spc, pair) =>
				GenerateMeterTargets(
					pair.Left.Left,
					pair.Left.Right.Left,
					pair.Left.Right.Right,
					pair.Right.Logger,
					spc
				)
		);
	}

	static void GenerateMeterTargets(
		ImmutableArray<GeneratorResult<MeterTarget?>> targets,
		bool emitNullable,
		bool supportsIMeterFactory,
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

			logger?.Debug($"Meter generation target: {target.FullyQualifiedName}");

			RunSafely(
				spc,
				() =>
					MeterTargetClassEmitter.GenerateImplementation(
						target,
						spc,
						logger,
						emitNullable,
						supportsIMeterFactory
					)
			);
		}
	}
}
