using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Emitters;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

partial class TelemetrySourceGenerator
{
	static void RegisterLoggerGeneration(
		IncrementalGeneratorInitializationContext context,
		GenerationLogger? logger
	)
	{
		// Transform
		Func<
			GeneratorAttributeSyntaxContext,
			CancellationToken,
			LoggerTarget?
		> loggerTargetTransform =
			logger == null
				? static (context, cancellationToken) =>
					PipelineHelpers.BuildLoggerTransform(context, null, cancellationToken)
				: (context, cancellationToken) =>
					PipelineHelpers.BuildLoggerTransform(context, logger, cancellationToken);

		// Register
		var loggerTargetsPredicate = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				Constants.Logging.LoggerAttribute.FullyQualifiedName,
				static (node, token) => PipelineHelpers.HasLoggerTargetAttribute(node, token),
				loggerTargetTransform
			)
			.WhereNotNull()
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_Logging");

		// Build generation (static vs. non-static is for the logger).
		Action<
			SourceProductionContext,
			(Compilation Compilation, ImmutableArray<LoggerTarget?> Targets)
		> generationLoggerAction =
			logger == null
				? static (spc, source) => GenerateLoggerTargets(source.Targets, spc, null)
				: (spc, source) => GenerateLoggerTargets(source.Targets, spc, logger);

		// Register with the source generator.
		var loggerTargets = context.CompilationProvider.Combine(loggerTargetsPredicate.Collect());

		context.RegisterImplementationSourceOutput(
			source: loggerTargets,
			action: generationLoggerAction
		);
	}

	static void GenerateLoggerTargets(
		ImmutableArray<LoggerTarget?> targets,
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
						$"Skipping logger generation target due to error diagnostic: {target.FullyQualifiedName}"
					);

					continue;
				}

				logger?.Debug($"Logger generation target: {target!.FullyQualifiedName}");

				if (target!.UseMSLoggingTelemetryBasedGeneration)
					LoggerGenTargetClassEmitter.GenerateImplementation(target, spc, logger);
				else
					LoggerTargetClassEmitter.GenerateImplementation(target, spc, logger);
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
