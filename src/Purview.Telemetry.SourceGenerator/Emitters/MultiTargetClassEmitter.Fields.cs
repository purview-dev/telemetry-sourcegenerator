using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class MultiTargetClassEmitter
{
	static int EmitFields(
		MultiTargetGenerationTarget target,
		StringBuilder builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		indent++;

		// Emit fields based on what telemetry types are needed
		if (target.GenerationType.HasFlag(GenerationType.Activities))
		{
			logger?.Debug("Emitting ActivitySource field");
			builder
				.Append(indent, "readonly ", withNewLine: false)
				.Append(Constants.Activities.SystemDiagnostics.ActivitySource)
				.Append(' ')
				.Append(Constants.VariableNames.ActivitySourceFieldName)
				.AppendLine(";");
		}

		if (target.GenerationType.HasFlag(GenerationType.Logging))
		{
			logger?.Debug("Emitting ILogger field");
			builder
				.Append(indent, "readonly ", withNewLine: false)
				.Append(Constants.Logging.MicrosoftExtensions.ILogger)
				.Append(' ')
				.Append(Constants.VariableNames.LoggerFieldName)
				.AppendLine(";");
		}

		if (target.GenerationType.HasFlag(GenerationType.Metrics))
		{
			logger?.Debug("Emitting Meter field");
			builder
				.Append(indent, "readonly ", withNewLine: false)
				.Append(Constants.Metrics.SystemDiagnostics.Meter)
				.Append(' ')
				.Append(Constants.VariableNames.MeterFieldName)
				.AppendLine(";");
		}

		builder.AppendLine();

		return --indent;
	}
}
