using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class MultiTargetClassEmitter
{
	static int EmitConstructor(
		MultiTargetGenerationTarget target,
		StringBuilder builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		indent++;

		logger?.Debug($"Emitting constructor for {target.ClassNameToGenerate}");

		// Constructor signature
		builder
			.CodeGen(indent)
			.Append(indent, "public ", withNewLine: false)
			.Append(target.ClassNameToGenerate)
			.Append('(');

		// Constructor parameters
		var parameters = new List<string>();

		if (target.GenerationType.HasFlag(GenerationType.Activities))
		{
			parameters.Add($"{Constants.Activities.SystemDiagnostics.ActivitySource} activitySource");
		}

		if (target.GenerationType.HasFlag(GenerationType.Logging))
		{
			parameters.Add($"{Constants.Logging.MicrosoftExtensions.ILogger} logger");
		}

		if (target.GenerationType.HasFlag(GenerationType.Metrics))
		{
			parameters.Add($"{Constants.Metrics.SystemDiagnostics.Meter} meter");
		}

		builder.AppendLine(string.Join(", ", parameters) + ")")
			.Append(indent, "{");

		// Constructor body
		indent++;

		if (target.GenerationType.HasFlag(GenerationType.Activities))
		{
			builder
				.Append(indent, Constants.VariableNames.ActivitySourceFieldName, withNewLine: false)
				.AppendLine(" = activitySource;");
		}

		if (target.GenerationType.HasFlag(GenerationType.Logging))
		{
			builder
				.Append(indent, Constants.VariableNames.LoggerFieldName, withNewLine: false)
				.AppendLine(" = logger;");
		}

		if (target.GenerationType.HasFlag(GenerationType.Metrics))
		{
			builder
				.Append(indent, Constants.VariableNames.MeterFieldName, withNewLine: false)
				.AppendLine(" = meter;");
		}

		indent--;

		builder.Append(indent, "}").AppendLine();

		return --indent;
	}
}
