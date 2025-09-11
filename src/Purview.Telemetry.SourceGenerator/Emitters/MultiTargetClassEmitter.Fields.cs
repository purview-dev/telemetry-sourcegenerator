using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class MultiTargetClassEmitter
{
	static int EmitFields(
		MultiTargetGenerationTarget target,
		CodeWriter builder,
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

			// Emit logging action fields (scoped & non-scoped) for each logging multi-target method
			foreach (
				var m in target.Methods.Where(m =>
					m.Configuration.TargetTypes.HasFlag(GenerationType.Logging)
				)
			)
			{
				var isScoped = m.Configuration.UsesScopedLogging;
				var filteredParams = m
					.Parameters.Where(p =>
						!p.Exclusions.HasFlag(ParameterExclusions.Logging)
						&& !ShouldAutoExcludeFromTarget(p.TypeName, "Logging")
					)
					.ToArray();
				var nonExceptionParams = filteredParams.Where(p => !p.IsException).ToArray();
				var fieldName = $"_log{m.MethodName}Action";
				var messageTemplate =
					m.Configuration.LogMessage
					?? BuildDefaultLogMessage(m.MethodName, nonExceptionParams);
				var level = m.Configuration.LogLevel ?? "Information";
				var eventId = m.Configuration.LogEventId.HasValue
					? m.Configuration.LogEventId.Value.ToString()
					: SharedHelpers.GetNonRandomizedHashCode(m.MethodName).ToString();

				builder
					.AppendLine()
					.Append(indent, "static readonly ", withNewLine: false)
					.Append(isScoped ? Constants.System.Func : Constants.System.Action)
					.Append('<')
					.Append(Constants.Logging.MicrosoftExtensions.ILogger)
					.Append(',');

				// Non-exception parameter generic arguments
				var paramIndex = 0;
				foreach (var p in nonExceptionParams)
				{
					builder.Append(' ').Append(PurviewTypeFactory.Create(p.ParameterSymbol.Type));
					builder.Append(',');
					paramIndex++;
				}

				// Final generic argument (Exception? or IDisposable?)
				if (isScoped)
					builder.Append(' ').Append(Constants.System.IDisposable).Append("?> ");
				else
					builder.Append(' ').Append(Constants.System.Exception).Append("?> ");

				builder
					.Append(fieldName)
					.Append(" = ")
					.Append(Constants.Logging.MicrosoftExtensions.LoggerMessage)
					.Append(isScoped ? ".DefineScope" : ".Define")
					.Append('(');

				if (!isScoped)
				{
					builder
						.Append("global::Microsoft.Extensions.Logging.LogLevel.")
						.Append(level)
						.Append(", new ")
						.Append(Constants.Logging.MicrosoftExtensions.EventId)
						.Append('(')
						.Append(eventId)
						.Append(", \"")
						.Append(m.Configuration.LogName ?? m.MethodName)
						.Append("\"), ");
				}

				builder.Append('"').Append(messageTemplate).Append('"').AppendLine(");");
			}
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

			// Emit instrument fields per metrics multi-target method (lazy init)
			foreach (
				var m in target.Methods.Where(m =>
					m.Configuration.TargetTypes.HasFlag(GenerationType.Metrics)
				)
			)
			{
				var fieldName = $"_metric{m.MethodName}";
				var instrumentGeneric = "long"; // default measurement type
				var instrumentType = m.Configuration.MetricType switch
				{
					MetricType.Counter => "Counter",
					MetricType.UpDownCounter => "UpDownCounter",
					MetricType.Histogram => "Histogram",
					MetricType.Gauge => "ObservableGauge", // treat gauge as observable conceptually
					_ => "Counter",
				};
				builder
					.Append(
						indent,
						$"global::System.Diagnostics.Metrics.{instrumentType}<",
						withNewLine: false
					)
					.Append(instrumentGeneric)
					.Append(">? ")
					.Append(fieldName)
					.AppendLine(" = null;");
			}
		}

		builder.AppendLine();

		return --indent;
	}
}
