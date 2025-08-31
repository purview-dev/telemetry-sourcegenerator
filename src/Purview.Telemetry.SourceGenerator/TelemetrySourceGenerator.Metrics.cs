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
		GenerationLogger? logger
	)
	{
		// Transform
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

		// Register
		var meterTargetsPredicate = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				Constants.Metrics.MeterAttribute.FullyQualifiedName,
				static (node, token) => PipelineHelpers.HasMeterTargetAttribute(node, token),
				meterTargetTransform
			)
			.WhereNotNull()
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_Meters");

		// Build generation (static vs. non-static is for the logger).
		Action<
			SourceProductionContext,
			(Compilation Compilation, ImmutableArray<MeterTarget?> Targets)
		> generationMeterAction =
			logger == null
				? static (spc, source) => GenerateMeterTargets(source.Targets, spc, null)
				: (spc, source) => GenerateMeterTargets(source.Targets, spc, logger);

		// Register with the source generator.
		var meterTargets = context.CompilationProvider.Combine(meterTargetsPredicate.Collect());

		context.RegisterImplementationSourceOutput(
			source: meterTargets,
			action: generationMeterAction
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

		if (targets.Any(m => m!.Failures?.Length > 0))
		{
			foreach (var failure in targets.SelectMany(m => m!.Failures!.Value))
				TelemetryDiagnostics.Report(spc.ReportDiagnostic, failure.Item1, failure.Item1);
		}

		try
		{
			foreach (var target in targets)
			{
				if (
					target!.Failures?.Length > 0
					&& target.Failures.Value.Any(m => m.Item1.Severity == DiagnosticSeverity.Error)
				)
				{
					logger?.Debug(
						$"Skipping meter generation target due to error diagnostic: {target.FullyQualifiedName}"
					);

					continue;
				}

				logger?.Debug($"Meter generation target: {target!.FullyQualifiedName}");

				MeterTargetClassEmitter.GenerateImplementation(target!, spc, logger);
			}
		}
		catch (Exception ex)
		{
			logger?.Error(
				$"A fatal error occurred while executing the source generation stage: {ex}"
			);

			TelemetryDiagnostics.Report(
				spc.ReportDiagnostic,
				TelemetryDiagnostics.General.FatalExecutionDuringExecution,
				ex
			);
		}
	}
}
