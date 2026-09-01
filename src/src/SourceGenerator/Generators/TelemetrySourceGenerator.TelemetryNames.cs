using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Emitters;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

partial class TelemetrySourceGenerator
{
	static void RegisterTelemetryNamesGeneration(
		IncrementalGeneratorInitializationContext context,
		IncrementalValuesProvider<GeneratorResult<ActivitySourceTarget?>> activityTargets,
		IncrementalValuesProvider<GeneratorResult<MeterTarget?>> meterTargets,
		IncrementalValueProvider<GenerationContext<TelemetryCapabilities>> generationContext
	)
	{
		// Extract only AssemblyName from Compilation — a stable string that rarely changes —
		// so we don't re-run TelemetryNames generation on every compilation change.
		var assemblyNameProvider = context.CompilationProvider.Select(static (c, _) => c.AssemblyName ?? string.Empty);

		var combined = assemblyNameProvider
			.Combine(meterTargets.Collect())
			.Combine(activityTargets.Collect())
			.Combine(generationContext);

		context.RegisterSourceOutput(
			source: combined,
			action: (spc, source) =>
				GenerateTelemetryNames(
					source.Left.Left.Left,
					source.Left.Left.Right,
					source.Left.Right,
					source.Right.Capabilities.SupportsNullableAnnotations,
					spc,
					source.Right.Logger
				)
		);
	}

	static void GenerateTelemetryNames(
		string assemblyName,
		ImmutableArray<GeneratorResult<MeterTarget?>> meterTargets,
		ImmutableArray<GeneratorResult<ActivitySourceTarget?>> activityTargets,
		bool emitNullable,
		SourceProductionContext spc,
		ISourceGenLogger? logger
	)
	{
		// Only generate if we have at least one target
		if (meterTargets.Length == 0 && activityTargets.Length == 0)
		{
			return;
		}

		// Only consider targets that are being processed (no interface-level errors).
		var processedMeters = meterTargets
			.Where(m => m.ShouldProcess && m.Value is { })
			.Select(m => m.Value!)
			.ToImmutableArray();
		var processedActivities = activityTargets
			.Where(m => m.ShouldProcess && m.Value is { })
			.Select(m => m.Value!)
			.ToImmutableArray();

		// Check if any target has GenerateTelemetryNamesClass set to false
		var generateClass = true;
		string? customClassName = null;

		// Check meter targets for TelemetryGeneration settings
		foreach (var target in processedMeters)
		{
			if (target!.TelemetryGeneration.GenerateTelemetryNamesClass == false)
			{
				generateClass = false;
				return;
			}

			if (target.TelemetryGeneration.TelemetryNamesClassName != null && customClassName == null)
			{
				customClassName = target.TelemetryGeneration.TelemetryNamesClassName;
			}
		}

		// Check activity targets for TelemetryGeneration settings
		foreach (var target in processedActivities)
		{
			if (target!.TelemetryGeneration.GenerateTelemetryNamesClass == false)
			{
				generateClass = false;
				return;
			}

			if (target.TelemetryGeneration.TelemetryNamesClassName != null && customClassName == null)
			{
				customClassName = target.TelemetryGeneration.TelemetryNamesClassName;
			}
		}

		if (!generateClass)
		{
			return;
		}

		// Collect unique meter names
		var meterNames = processedMeters
			.Where(t => !string.IsNullOrEmpty(t.MeterName))
			.Select(t => t!.MeterName!)
			.Distinct(StringComparer.Ordinal)
			.OrderBy(n => n, StringComparer.Ordinal)
			.ToImmutableArray();

		// Collect unique activity source names
		var activitySourceNames = processedActivities
			.Where(t => !string.IsNullOrEmpty(t.ActivitySourceName))
			.Select(t => t!.ActivitySourceName!)
			.Distinct(StringComparer.Ordinal)
			.OrderBy(n => n, StringComparer.Ordinal)
			.ToImmutableArray();

		// Use custom class name if provided, otherwise default to "TelemetryNames"
		var className = string.IsNullOrWhiteSpace(customClassName) ? "TelemetryNames" : customClassName;

		RunSafely(
			spc,
			() =>
				TelemetryNamesEmitter.GenerateClass(
					meterNames,
					activitySourceNames,
					className!,
					assemblyName,
					emitNullable,
					spc,
					logger
				)
		);
	}
}
