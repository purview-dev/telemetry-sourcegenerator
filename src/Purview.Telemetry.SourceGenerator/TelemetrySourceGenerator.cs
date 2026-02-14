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

		RegisterActivitiesGeneration(context, _logger);
		RegisterLoggerGeneration(context, _logger);
		RegisterMetricsGeneration(context, _logger);
		RegisterTelemetryNamesGeneration(context, _logger);
	}

	void ILogSupport.SetLogOutput(Action<string, OutputType> action) =>
		_logger = action == null ? null : new GenerationLogger(action);
}
