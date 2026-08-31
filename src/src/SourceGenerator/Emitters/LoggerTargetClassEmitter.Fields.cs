using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class LoggerTargetClassEmitter
{
	static int EmitFields(
		LoggerTarget target,
		StringBuilder builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		indent++;

		builder
			.Append(indent, "readonly ", withNewLine: false)
			.Append(Constants.Logging.MicrosoftExtensions.ILogger)
			.Append('<')
			.Append(target.InterfaceType)
			.Append('>')
			.Append(' ')
			.Append(Constants.Logging.LoggerFieldName)
			.Append(';')
			.AppendLine()
			.AppendLine();

		foreach (var methodTarget in target.LogMethods)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			if (!methodTarget.TargetGenerationState.IsValid)
			{
				if (methodTarget.TargetGenerationState.RaiseMultiGenerationTargetsNotSupported)
				{
					logger?.Debug(
						$"Identified {target.InterfaceType.TypeName}.{methodTarget.MethodName} as problematic as it has another target types."
					);
				}
				else if (methodTarget.TargetGenerationState.RaiseInferenceNotSupportedWithMultiTargeting)
				{
					logger?.Debug(
						$"Identified {target.InterfaceType.TypeName}.{methodTarget.MethodName} as problematic as it is inferred."
					);
				}

				continue;
			}

			// Report warning for Activity parameter without Activity target
			if (methodTarget.TargetGenerationState.ActivityParameterWithoutTarget != null)
			{
				logger?.Debug(
					$"Activity parameter '{methodTarget.TargetGenerationState.ActivityParameterWithoutTarget}' on {methodTarget.MethodName} has no Activity target."
				);
			}

			if (methodTarget.UnknownReturnType)
			{
				TelemetryDiagnostics.Report(
					context.ReportDiagnostic,
					TelemetryDiagnostics.Logging.LogMustReturnVoidOrAsync
				);
				continue;
			}

			if (methodTarget.HasMultipleExceptions)
			{
				logger?.Diagnostic("Method has multiple exception parameters, only a single one is permitted.");
				TelemetryDiagnostics.Report(
					context.ReportDiagnostic,
					TelemetryDiagnostics.Logging.MultipleExceptionsDefined
				);

				continue;
			}

			if (methodTarget.ParameterCountSansException > Constants.Logging.MaxNonExceptionParameters)
			{
				logger?.Diagnostic("Method has more than 6 parameters.");
				TelemetryDiagnostics.Report(
					context.ReportDiagnostic,
					TelemetryDiagnostics.Logging.MaximumLogEntryParametersExceeded
				);

				continue;
			}

			if (methodTarget.InferredErrorLevel)
			{
				logger?.Diagnostic("Inferring error log level.");
				TelemetryDiagnostics.Report(
					context.ReportDiagnostic,
					TelemetryDiagnostics.Logging.InferringErrorLogLevel
				);
			}

			EmitLogActionField(builder, indent, methodTarget, emitNullable);
		}

		return --indent;
	}

	internal static void EmitLogActionField(
		StringBuilder builder,
		int indent,
		LogMethodTarget methodTarget,
		bool emitNullable = true
	)
	{
		builder
			.Append(indent, "static readonly ", withNewLine: false)
			.Append(methodTarget.IsScoped ? Constants.System.Func : Constants.System.Action)
			.Append('<')
			.Append(Constants.Logging.MicrosoftExtensions.ILogger)
			.Append(", ");

		foreach (var parameter in methodTarget.ParametersSansException)
			builder.Append(parameter.ParameterType).Append(", ");

		if (methodTarget.IsScoped)
		{
			builder.Append(Constants.System.IDisposable);
			if (emitNullable)
				builder.Append('?');
			builder.Append("> ");
		}
		else
		{
			builder.Append(Constants.System.Exception);
			if (emitNullable)
				builder.Append('?');
			builder.Append("> ");
		}

		builder
			.Append(methodTarget.LoggerActionFieldName)
			.Append(" = ")
			.Append(Constants.Logging.MicrosoftExtensions.LoggerMessage)
			.Append(".Define");

		if (methodTarget.IsScoped)
			builder.Append("Scope");

		if (methodTarget.ParameterCountSansException > 0)
		{
			builder.Append('<');

			var i = 0;
			foreach (var parameter in methodTarget.ParametersSansException)
			{
				builder.Append(parameter.ParameterType);
				if (i < methodTarget.ParameterCountSansException - 1)
					builder.Append(", ");

				i++;
			}

			builder.Append('>');
		}

		builder.Append('(');

		if (!methodTarget.IsScoped)
		{
			builder.Append(methodTarget.MSLevel).Append(", ");

			var eventId = methodTarget.EventId ?? SharedHelpers.GetNonRandomizedHashCode(methodTarget.MethodName);
			builder
				.Append("new ")
				.Append(Constants.Logging.MicrosoftExtensions.EventId)
				.Append('(')
				.Append(eventId)
				.Append(", \"")
				.Append(methodTarget.LogName)
				.Append("\"), ");
		}

		builder.Append('"').Append(methodTarget.MessageTemplate).Append('"').AppendLine(");");
	}
}
