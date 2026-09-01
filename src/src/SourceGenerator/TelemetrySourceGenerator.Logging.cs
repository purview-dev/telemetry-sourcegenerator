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
		IncrementalValueProvider<GenerationContext<TelemetryCapabilities>> generationContext
	)
	{
		// Register
		var loggerTargetsPredicate = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				TemplateLibrary.Logging.LoggerAttribute.TypeInfo.MetadataFullName,
				static (node, token) => PipelineHelpers.HasLoggerTargetAttribute(node, token),
				static (context, cancellationToken) =>
					PipelineHelpers.BuildLoggerTransform(context, null, cancellationToken)
			)
			.Where(static m => m.HasValue)
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_Logging");

		// Register with the source generator.
		context.RegisterImplementationSourceOutput(
			source: loggerTargetsPredicate
				.Collect()
				.Combine(supportsNullableAnnotations.Combine(supportsIMeterFactory))
				.Combine(generationContext),
			action: (spc, pair) =>
				GenerateLoggerTargets(
					pair.Left.Left,
					pair.Left.Right.Left,
					pair.Left.Right.Right,
					pair.Right.Logger,
					spc
				)
		);
	}

	static void GenerateLoggerTargets(
		ImmutableArray<GeneratorResult<LoggerTarget?>> targets,
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

			logger?.Debug($"Logger generation target: {target.FullyQualifiedName}");

			if (target.UseMSLoggingTelemetryBasedGeneration)
				RunSafely(
					spc,
					() =>
						LoggerGenTargetClassEmitter.GenerateImplementation(
							target,
							spc,
							logger,
							emitNullable,
							supportsIMeterFactory
						)
				);
			else
				RunSafely(
					spc,
					() =>
						LoggerTargetClassEmitter.GenerateImplementation(
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
