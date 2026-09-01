using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Emitters;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

partial class TelemetrySourceGenerator
{
	static void RegisterLoggerGeneration(
		IncrementalGeneratorInitializationContext context,
		IncrementalValueProvider<GenerationContext<TelemetryCapabilities>> generationContext
	)
	{
		// Register
		var loggerTargetsPredicate = IncrementalPipeline
			.ForAttributeWithMetadataName(
				context,
				TemplateLibrary.Logging.LoggerAttribute.TypeInfo,
				transform: static (context, cancellationToken) =>
					PipelineHelpers.BuildLoggerTransform(context, null, cancellationToken),
				predicate: static (node, token) => PipelineHelpers.HasLoggerTargetAttribute(node, token),
				trackingName: $"{nameof(TelemetrySourceGenerator)}_Logging"
			)
			.Where(static m => m.HasValue);

		// Register with the source generator.
		context.RegisterSourceOutput(
			source: loggerTargetsPredicate.Combine(generationContext),
			action: (spc, pair) =>
			{
				var (result, genContext) = pair;

				if (!result.ShouldProcess || result.Value is not { } target)
					return;

				var logger = genContext.Logger;
				logger?.Debug($"Logger generation target: {target.FullyQualifiedName}");

				var emitNullable = genContext.Capabilities.SupportsNullableAnnotations;
				var supportsIMeterFactory = genContext.Capabilities.SupportsIMeterFactory;

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
		);
	}
}
