using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Templates;

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
			_logger?.Debug("--- Adding shared types.");

			foreach (var template in Constants.GetEmbeddedFileNames())
			{
				var generatedFilename = $"{template}.g.cs";

				_logger?.Debug($"  Adding {template} as {generatedFilename}.");

				var templateData = EmbeddedResources.Instance.LoadEmbeddedResource(template);
				ctx.AddSource(generatedFilename, templateData);
			}

			_logger?.Debug("--- Finished adding shared types.");
		});

		RegisterActivitiesGeneration(context, _logger);
		RegisterLoggerGeneration(context, _logger);
		RegisterMetricsGeneration(context, _logger);
		// TODO: Re-enable after debugging - RegisterMultiTargetGeneration(context, _logger);
	}

	void ILogSupport.SetLogOutput(Action<string, OutputType> action) =>
		_logger = action == null ? null : new GenerationLogger(action);
}
