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

		// The assembly-level [TelemetryGeneration] has to be read independently of the targets: an
		// assembly that only consumes telemetry from its references has no target to carry it.
		// Projected down to the names-related settings so unrelated changes leave the result cached.
		var assemblySettings = context
			.CompilationProvider.Select(static (compilation, _) => ResolveAssemblySettings(compilation))
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_TelemetryNamesAssemblySettings");

		// Depends on the target providers and those settings alone, so an unrelated edit leaves it cached.
		var resolvedNames = meterTargets
			.Collect()
			.Combine(activityTargets.Collect())
			.Combine(assemblySettings)
			.Select(static (tuple, token) => ResolveNames(tuple.Left.Left, tuple.Left.Right, tuple.Right, token))
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

		// The names referenced assemblies recorded, projected to strings before they reach a cached node.
		// Combining the Compilation itself here meant this stage could never report cached, because a
		// Compilation is compared by reference and is a new instance on every keystroke (PSGFR33).
		var referencedNames = context
			.CompilationProvider.Select(static (compilation, token) => CollectReferencedNames(compilation, token))
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_TelemetryNamesReferenced");

		var classOutput = resolvedNames
			.Combine(referencedNames)
			.Combine(generationContext)
			.Select(
				static (tuple, token) =>
					Aggregate(names: tuple.Left.Left, referenced: tuple.Left.Right, context: tuple.Right, token: token)
			)
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_TelemetryNamesOutput");

		context.RegisterSourceOutput(
			source: classOutput,
			action: static (spc, output) => GenerateTelemetryNames(output, spc)
		);
	}

	/// <summary>
	/// Projects the assembly-level <c>[TelemetryGeneration]</c> down to the settings that govern the
	/// telemetry names.
	/// </summary>
	static AssemblyTelemetryNamesSettings ResolveAssemblySettings(Compilation compilation)
	{
		var generation = SharedHelpers.GetAssemblyTelemetryGenerationAttribute(compilation);

		return new(
			GenerateAttribute: generation.GenerateTelemetryNamesAttribute,
			GenerateClass: generation.GenerateTelemetryNamesClass,
			AggregateReferencedNames: generation.AggregateReferencedTelemetryNames,
			ClassName: generation.TelemetryNamesClassName,
			Namespace: generation.TelemetryNamesNamespace
		);
	}

	/// <summary>
	/// Resolves the distinct activity source and meter names generated for this compilation, along
	/// with the <c>[TelemetryGeneration]</c> settings that control the <c>TelemetryNames</c> class.
	/// </summary>
	static ResolvedTelemetryNames ResolveNames(
		ImmutableArray<GeneratorResult<MeterTarget?>> meterTargets,
		ImmutableArray<GeneratorResult<ActivitySourceTarget?>> activityTargets,
		AssemblyTelemetryNamesSettings assemblySettings,
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

		// The assembly-level attribute seeds the settings — it is the only source when there are no
		// targets at all. On top of that: the class is opt-in, so any target asking for it is enough;
		// the attribute and the aggregation are opt-out, so any target declining them turns them off.
		// The assembly's custom class name/namespace wins, otherwise the first target to name one does.
		var generateAttribute = assemblySettings.GenerateAttribute;
		var generateClass = assemblySettings.GenerateClass;
		var aggregateReferencedNames = assemblySettings.AggregateReferencedNames;
		var className = assemblySettings.ClassName;
		var classNamespace = assemblySettings.Namespace;

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
		ReferencedTelemetryNamesContext referenced,
		GenerationContext<TelemetryCapabilities> context,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		if (!names.GenerateClass || !names.AggregateReferencedNames)
			return new(referenced.AssemblyName, names, context);

		SortedSet<string> activitySourceNames = new(names.ActivitySourceNames, StringComparer.Ordinal);
		SortedSet<string> meterNames = new(names.MeterNames, StringComparer.Ordinal);

		activitySourceNames.UnionWith(referenced.ActivitySourceNames);
		meterNames.UnionWith(referenced.MeterNames);

		return new(
			referenced.AssemblyName,
			names with
			{
				ActivitySourceNames = new([.. activitySourceNames]),
				MeterNames = new([.. meterNames]),
			},
			context
		);
	}

	/// <summary>
	/// Reads the telemetry names every referenced assembly recorded, and this assembly's name, into a
	/// value-equatable context.
	/// </summary>
	/// <remarks>
	/// This runs for every compilation rather than only when the aggregated class is being generated,
	/// which is the cost of keeping the <c>Compilation</c> out of the pipeline's cached values. The work
	/// is a scan of referenced assembly attributes and its result caches on the names themselves, so a
	/// keystroke that changes no reference leaves every downstream stage cached — which the previous
	/// shape could never achieve, because the stage holding the <c>Compilation</c> was recomputed every
	/// time regardless.
	/// </remarks>
	static ReferencedTelemetryNamesContext CollectReferencedNames(Compilation compilation, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		SortedSet<string> activitySourceNames = new(StringComparer.Ordinal);
		SortedSet<string> meterNames = new(StringComparer.Ordinal);

		ReferencedTelemetryNames.Collect(compilation, activitySourceNames, meterNames, token);

		return new(compilation.AssemblyName ?? string.Empty, new([.. activitySourceNames]), new([.. meterNames]));
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
