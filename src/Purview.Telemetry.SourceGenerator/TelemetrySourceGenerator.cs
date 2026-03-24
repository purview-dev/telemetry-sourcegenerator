using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;

namespace Purview.Telemetry.SourceGenerator;

[Generator]
public sealed partial class TelemetrySourceGenerator : IIncrementalGenerator, ILogSupport
{
	GenerationLogger? _logger;

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// Register all of the shared attributes we need.
		context.RegisterPostInitializationOutput(ctx =>
		{
			_logger?.Debug("--- Adding templates.");

			foreach (var template in Constants.GetAllTemplates())
			{
				_logger?.Debug($"Adding {template.Name} as {template.GetGeneratedFilename()}.");

				ctx.AddSource(template.GetGeneratedFilename(), template.TemplateData);
			}

			_logger?.Debug("--- Finished adding templates.");
		});

		// Create shared providers so Activities/Metrics pipelines are registered once
		// and reused by TelemetryNames instead of running ForAttributeWithMetadataName twice.
		var activityProvider = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				Constants.Activities.ActivitySourceAttribute.TypeInfo.FullyQualifiedName,
				static (node, token) => PipelineHelpers.HasActivityTargetAttribute(node, token),
				(ctx, cancellationToken) =>
					PipelineHelpers.BuildActivityTransform(ctx, _logger, cancellationToken)
			)
			.WhereNotNull()
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_Activities");

		var meterProvider = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				Constants.Metrics.MeterAttribute.TypeInfo.FullyQualifiedName,
				static (node, token) => PipelineHelpers.HasMeterTargetAttribute(node, token),
				(ctx, cancellationToken) =>
					PipelineHelpers.BuildMeterTransform(ctx, _logger, cancellationToken)
			)
			.WhereNotNull()
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_Meters");

		RegisterActivitiesGeneration(context, activityProvider, _logger);
		RegisterLoggerGeneration(context, _logger);
		RegisterMetricsGeneration(context, meterProvider, _logger);
		RegisterTelemetryNamesGeneration(context, activityProvider, meterProvider, _logger);
	}

	void ILogSupport.SetLogOutput(Action<string, OutputType> action) =>
		_logger = action == null ? null : new GenerationLogger(action);
}
