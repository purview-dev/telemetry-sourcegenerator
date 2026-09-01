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
		var loggerTargetsPredicate = IncrementalPipeline.ForAttributeWithMetadataName(
			context,
			TemplateLibrary.Logging.LoggerAttribute.TypeInfo,
			transform: static (context, cancellationToken) =>
				PipelineHelpers.BuildLoggerTransform(context, null, cancellationToken),
			predicate: static (node, token) => PipelineHelpers.HasLoggerTargetAttribute(node, token),
			trackingName: $"{nameof(TelemetrySourceGenerator)}_Logging"
		);

		var outputContexts = loggerTargetsPredicate
			.Where(static m => m.ShouldProcess)
			.Combine(generationContext)
			.Select(static (pair, _) => new LoggerOutputContext(pair.Left.Value!, pair.Right))
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_LoggerOutputs");

		// Register with the source generator.
		context.RegisterSourceOutput(
			source: outputContexts,
			action: static (spc, output) =>
			{
				output.Context.Logger?.Debug($"Logger generation target: {output.Target.FullyQualifiedName}");

				if (output.Target.UseMSLoggingTelemetryBasedGeneration)
					RunSafely(spc, () => LoggerGenTargetClassEmitter.GenerateImplementation(output, spc));
				else
					RunSafely(spc, () => LoggerTargetClassEmitter.GenerateImplementation(output, spc));
			}
		);
	}
}
