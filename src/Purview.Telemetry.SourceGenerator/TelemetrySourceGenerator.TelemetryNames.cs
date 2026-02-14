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
		GenerationLogger? logger
	)
	{
		// Get meter targets
		Func<
			GeneratorAttributeSyntaxContext,
			CancellationToken,
			MeterTarget?
		> meterTargetTransform =
			logger == null
				? static (context, cancellationToken) =>
					PipelineHelpers.BuildMeterTransform(context, null, cancellationToken)
				: (context, cancellationToken) =>
					PipelineHelpers.BuildMeterTransform(context, logger, cancellationToken);

		var meterTargetsPredicate = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				Constants.Metrics.MeterAttribute.TypeInfo.FullyQualifiedName,
				static (node, token) => PipelineHelpers.HasMeterTargetAttribute(node, token),
				meterTargetTransform
			)
			.WhereNotNull()
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_TelemetryNames_Meters");

		// Get activity targets
		Func<
			GeneratorAttributeSyntaxContext,
			CancellationToken,
			ActivitySourceTarget?
		> activityTargetTransform =
			logger == null
				? static (context, cancellationToken) =>
					PipelineHelpers.BuildActivityTransform(context, null, cancellationToken)
				: (context, cancellationToken) =>
					PipelineHelpers.BuildActivityTransform(context, logger, cancellationToken);

		var activityTargetsPredicate = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				Constants.Activities.ActivitySourceAttribute.TypeInfo.FullyQualifiedName,
				static (node, token) => PipelineHelpers.HasActivityTargetAttribute(node, token),
				activityTargetTransform
			)
			.WhereNotNull()
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_TelemetryNames_Activities");

		// Combine compilation with both target types
		var combined = context
			.CompilationProvider.Combine(meterTargetsPredicate.Collect())
			.Combine(activityTargetsPredicate.Collect());

		// Register generation
		Action<
			SourceProductionContext,
			(
				(Compilation Compilation, ImmutableArray<MeterTarget?> MeterTargets) Left,
				ImmutableArray<ActivitySourceTarget?> ActivityTargets
			)
		> generationAction =
			logger == null
				? static (spc, source) =>
					GenerateTelemetryNames(
						source.Left.Compilation,
						source.Left.MeterTargets,
						source.ActivityTargets,
						spc,
						null
					)
				: (spc, source) =>
					GenerateTelemetryNames(
						source.Left.Compilation,
						source.Left.MeterTargets,
						source.ActivityTargets,
						spc,
						logger
					);

		context.RegisterImplementationSourceOutput(source: combined, action: generationAction);
	}

	static void GenerateTelemetryNames(
		Compilation compilation,
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
			.Where(t =>
				t != null
				&& (t.Failures == null || t.Failures.Value.Length == 0)
				&& !string.IsNullOrEmpty(t.MeterName)
			)
			.Select(t => t!.MeterName!)
			.Distinct(StringComparer.Ordinal)
			.OrderBy(n => n, StringComparer.Ordinal)
			.ToImmutableArray();

		// Collect unique activity source names
		var activitySourceNames = activityTargets
			.Where(t =>
				t != null
				&& (t.Failures == null || t.Failures.Value.Length == 0)
				&& !string.IsNullOrEmpty(t.ActivitySourceName)
			)
			.Select(t => t!.ActivitySourceName!)
			.Distinct(StringComparer.Ordinal)
			.OrderBy(n => n, StringComparer.Ordinal)
			.ToImmutableArray();

		// Use assembly name as namespace (no RootNamespace available in source generators)
		string? rootNamespace = compilation.AssemblyName;

		// Use custom class name if provided, otherwise default to "TelemetryNames"
		var className = string.IsNullOrWhiteSpace(customClassName)
			? "TelemetryNames"
			: customClassName;

		TelemetryNamesEmitter.GenerateClass(
			meterNames,
			activitySourceNames,
			className!,
			rootNamespace,
			spc,
			logger
		);
	}
}
