using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Emitters;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

partial class TelemetrySourceGenerator
{
	static void RegisterTelemetryNamesGeneration(
		IncrementalGeneratorInitializationContext context,
		IncrementalValuesProvider<ActivitySourceTarget?> activityTargets,
		IncrementalValuesProvider<MeterTarget?> meterTargets,
		GenerationLogger? logger
	)
	{
		// Extract only AssemblyName from Compilation — a stable string that rarely changes —
		// so we don't re-run TelemetryNames generation on every compilation change.
		var assemblyNameProvider = context.CompilationProvider
			.Select(static (c, _) => c.AssemblyName ?? string.Empty);

		var combined = assemblyNameProvider
			.Combine(meterTargets.Collect())
			.Combine(activityTargets.Collect());

		context.RegisterImplementationSourceOutput(
			source: combined,
			action: (spc, source) =>
				GenerateTelemetryNames(
					source.Left.Left,
					source.Left.Right,
					source.Right,
					spc,
					logger
				)
		);
	}

	static void GenerateTelemetryNames(
		string assemblyName,
		ImmutableArray<MeterTarget?> meterTargets,
		ImmutableArray<ActivitySourceTarget?> activityTargets,
		SourceProductionContext spc,
		GenerationLogger? logger
	)
	{
		// Only generate if we have at least one target
		if (meterTargets.Length == 0 && activityTargets.Length == 0)
		{
			return;
		}

		// Check if any target has GenerateTelemetryNamesClass set to false
		bool generateClass = true;
		string? customClassName = null;

		// Check meter targets for TelemetryGeneration settings
		foreach (var target in meterTargets.Where(t => t != null))
		{
			if (target!.TelemetryGeneration?.GenerateTelemetryNamesClass.Value == false)
			{
				generateClass = false;
				return;
			}

			if (
				target.TelemetryGeneration?.TelemetryNamesClassName.Value != null
				&& customClassName == null
			)
			{
				customClassName = target.TelemetryGeneration.TelemetryNamesClassName.Value;
			}
		}

		// Check activity targets for TelemetryGeneration settings
		foreach (var target in activityTargets.Where(t => t != null))
		{
			if (target!.TelemetryGeneration?.GenerateTelemetryNamesClass.Value == false)
			{
				generateClass = false;
				return;
			}

			if (
				target.TelemetryGeneration?.TelemetryNamesClassName.Value != null
				&& customClassName == null
			)
			{
				customClassName = target.TelemetryGeneration.TelemetryNamesClassName.Value;
			}
		}

		if (!generateClass)
		{
			return;
		}

		// Collect unique meter names
		var meterNames = meterTargets
			.Where(t => t != null && !string.IsNullOrEmpty(t.MeterName))
			.Select(t => t!.MeterName!)
			.Distinct(StringComparer.Ordinal)
			.OrderBy(n => n, StringComparer.Ordinal)
			.ToImmutableArray();

		// Collect unique activity source names
		var activitySourceNames = activityTargets
			.Where(t => t != null && !string.IsNullOrEmpty(t.ActivitySourceName))
			.Select(t => t!.ActivitySourceName!)
			.Distinct(StringComparer.Ordinal)
			.OrderBy(n => n, StringComparer.Ordinal)
			.ToImmutableArray();

		// Use custom class name if provided, otherwise default to "TelemetryNames"
		var className = string.IsNullOrWhiteSpace(customClassName)
			? "TelemetryNames"
			: customClassName;

		TelemetryNamesEmitter.GenerateClass(
			meterNames,
			activitySourceNames,
			className!,
			assemblyName,
			spc,
			logger
		);
	}
}
