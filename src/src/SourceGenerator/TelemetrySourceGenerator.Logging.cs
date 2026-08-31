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
		IncrementalValueProvider<bool> supportsNullableAnnotations,
		IncrementalValueProvider<bool> supportsIMeterFactory,
		GenerationLogger? logger
	)
	{
		// Register
		var loggerTargetsPredicate = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				Constants.Logging.LoggerAttribute.TypeInfo.FullyQualifiedName,
				static (node, token) => PipelineHelpers.HasLoggerTargetAttribute(node, token),
				(context, cancellationToken) => PipelineHelpers.BuildLoggerTransform(context, logger, cancellationToken)
			)
			.WhereNotNull()
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_Logging");

		// Register with the source generator.
		context.RegisterImplementationSourceOutput(
			source: loggerTargetsPredicate
				.Collect()
				.Combine(supportsNullableAnnotations.Combine(supportsIMeterFactory)),
			action: (spc, pair) => GenerateLoggerTargets(pair.Left, pair.Right.Left, pair.Right.Right, spc, logger)
		);
	}

	static void GenerateLoggerTargets(
		ImmutableArray<LoggerTarget?> targets,
		bool emitNullable,
		bool supportsIMeterFactory,
		SourceProductionContext spc,
		GenerationLogger? logger
	)
	{
		if (targets.Length == 0)
			return;

		foreach (var target in targets)
		{
			logger?.Debug($"Logger generation target: {target!.FullyQualifiedName}");

			if (target!.UseMSLoggingTelemetryBasedGeneration)
				LoggerGenTargetClassEmitter.GenerateImplementation(
					target,
					spc,
					logger,
					emitNullable,
					supportsIMeterFactory
				);
			else
				LoggerTargetClassEmitter.GenerateImplementation(
					target,
					spc,
					logger,
					emitNullable,
					supportsIMeterFactory
				);
		}
	}
}
