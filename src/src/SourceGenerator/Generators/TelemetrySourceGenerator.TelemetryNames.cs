using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Emitters;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

partial class TelemetrySourceGenerator
{
	/// <summary>
	/// Registers the two telemetry-names outputs:
	/// <list type="number">
	/// <item>
	/// <c>[assembly: GeneratedTelemetryNames]</c>, recording this assembly's names in its metadata.
	/// Always emitted, so every assembly publishes what it generated.
	/// </item>
	/// <item>
	/// The <c>TelemetryNames</c> class, opt-in via <c>GenerateTelemetryNamesClass</c>, aggregating this
	/// assembly's names with those recorded by every referenced assembly. Typically only the project
	/// that registers the names with OpenTelemetry needs it.
	/// </item>
	/// </list>
	/// </summary>
	static void RegisterTelemetryNamesGeneration(
		IncrementalGeneratorInitializationContext context,
		IncrementalValuesProvider<GeneratorResult<ActivitySourceTarget?>> activityTargets,
		IncrementalValuesProvider<GeneratorResult<MeterTarget?>> meterTargets,
		IncrementalValueProvider<GenerationContext<TelemetryCapabilities>> generationContext
	)
	{
		// Extract only AssemblyName from Compilation — a stable string that rarely changes —
		// so we don't re-run the attribute generation on every compilation change.
		var assemblyNameProvider = context.CompilationProvider.Select(static (c, _) => c.AssemblyName ?? string.Empty);

		// Depends on the target providers alone, so an unrelated edit leaves it cached.
		var resolvedNames = meterTargets
			.Collect()
			.Combine(activityTargets.Collect())
			.Select(static (targets, token) => ResolveNames(targets.Left, targets.Right, token))
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_TelemetryNamesResolved");

		var attributeOutput = resolvedNames
			.Combine(assemblyNameProvider)
			.Combine(generationContext)
			.Select(
				static (tuple, _) => new TelemetryNamesOutputContext(tuple.Left.Right, tuple.Left.Left, tuple.Right)
			)
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_TelemetryNamesAttributeOutput");

		context.RegisterSourceOutput(
			source: attributeOutput,
			action: static (spc, output) => GenerateTelemetryNamesAttribute(output, spc)
		);

		var classOutput = resolvedNames
			.Combine(context.CompilationProvider)
			.Combine(generationContext)
			.Select(
				static (tuple, token) =>
					Aggregate(names: tuple.Left.Left, compilation: tuple.Left.Right, context: tuple.Right, token: token)
			)
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_TelemetryNamesOutput");

		context.RegisterSourceOutput(
			source: classOutput,
			action: static (spc, output) => GenerateTelemetryNames(output, spc)
		);
	}

	/// <summary>
	/// Resolves the distinct activity source and meter names generated for this compilation, along
	/// with the <c>[TelemetryGeneration]</c> settings that control the <c>TelemetryNames</c> class.
	/// </summary>
	static ResolvedTelemetryNames ResolveNames(
		ImmutableArray<GeneratorResult<MeterTarget?>> meterTargets,
		ImmutableArray<GeneratorResult<ActivitySourceTarget?>> activityTargets,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		// Only consider targets that are being processed (no interface-level errors).
		var processedMeters = meterTargets
			.Where(static m => m.ShouldProcess && m.Value is { })
			.Select(static m => m.Value!)
			.ToArray();
		var processedActivities = activityTargets
			.Where(static m => m.ShouldProcess && m.Value is { })
			.Select(static m => m.Value!)
			.ToArray();

		// The class is opt-in, so any target asking for it is enough; the attribute and the
		// aggregation are opt-out, so any target declining them turns them off. The first custom
		// class name/ namespace wins.
		var generateAttribute = true;
		var generateClass = false;
		var aggregateReferencedNames = true;
		string? className = null;
		string? classNamespace = null;

		foreach (
			var generation in processedMeters
				.Select(static target => target.TelemetryGeneration)
				.Concat(processedActivities.Select(static target => target.TelemetryGeneration))
		)
		{
			if (!generation.GenerateTelemetryNamesAttribute)
				generateAttribute = false;

			if (generation.GenerateTelemetryNamesClass)
				generateClass = true;

			if (!generation.AggregateReferencedTelemetryNames)
				aggregateReferencedNames = false;

			className ??= generation.TelemetryNamesClassName;
			classNamespace ??= generation.TelemetryNamesNamespace;
		}

		var meterNames = processedMeters
			.Where(static target => !string.IsNullOrEmpty(target.MeterName))
			.Select(static target => target.MeterName!)
			.Distinct(StringComparer.Ordinal)
			.OrderBy(static name => name, StringComparer.Ordinal)
			.ToImmutableArray();

		var activitySourceNames = processedActivities
			.Where(static target => !string.IsNullOrEmpty(target.ActivitySourceName))
			.Select(static target => target.ActivitySourceName!)
			.Distinct(StringComparer.Ordinal)
			.OrderBy(static name => name, StringComparer.Ordinal)
			.ToImmutableArray();

		return new(
			ActivitySourceNames: new(activitySourceNames),
			MeterNames: new(meterNames),
			GenerateAttribute: generateAttribute,
			GenerateClass: generateClass,
			AggregateReferencedNames: aggregateReferencedNames,
			ClassName: className,
			Namespace: classNamespace
		);
	}

	/// <summary>
	/// Merges the names recorded by referenced assemblies into this compilation's own names. The
	/// referenced assemblies are only scanned when the aggregated class is actually being generated.
	/// </summary>
	static TelemetryNamesOutputContext Aggregate(
		ResolvedTelemetryNames names,
		Compilation compilation,
		GenerationContext<TelemetryCapabilities> context,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		var assemblyName = compilation.AssemblyName ?? string.Empty;
		if (!names.GenerateClass || !names.AggregateReferencedNames)
			return new(assemblyName, names, context);

		SortedSet<string> activitySourceNames = new(names.ActivitySourceNames, StringComparer.Ordinal);
		SortedSet<string> meterNames = new(names.MeterNames, StringComparer.Ordinal);

		ReferencedTelemetryNames.Collect(compilation, activitySourceNames, meterNames, token);

		return new(
			assemblyName,
			names with
			{
				ActivitySourceNames = new(activitySourceNames.ToImmutableArray()),
				MeterNames = new(meterNames.ToImmutableArray()),
			},
			context
		);
	}

	static void GenerateTelemetryNamesAttribute(TelemetryNamesOutputContext output, SourceProductionContext spc)
	{
		if (output.Context.Settings.IsSourceGeneratorDisabled)
		{
			output.Context.Debug(
				$"Telemetry names attribute generation skipped for {output.AssemblyName} because the generator is disabled"
			);
			return;
		}

		if (!output.Names.GenerateAttribute || output.Names.IsEmpty)
			return;

		RunSafely(spc, () => TelemetryNamesEmitter.GenerateAssemblyAttribute(output, spc));
	}

	static void GenerateTelemetryNames(TelemetryNamesOutputContext output, SourceProductionContext spc)
	{
		if (output.Context.Settings.IsSourceGeneratorDisabled)
		{
			output.Context.Debug(
				$"Telemetry names generation skipped for {output.AssemblyName} because the generator is disabled"
			);
			return;
		}

		if (!output.Names.GenerateClass || output.Names.IsEmpty)
			return;

		// Use custom class name if provided, otherwise default to "TelemetryNames"
		var className = string.IsNullOrWhiteSpace(output.Names.ClassName) ? "TelemetryNames" : output.Names.ClassName!;

		RunSafely(
			spc,
			() =>
				TelemetryNamesEmitter.GenerateClass(
					output,
					className,
					output.Names.Namespace ?? output.AssemblyName,
					spc
				)
		);
	}
}
