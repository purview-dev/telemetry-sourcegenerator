using System.Globalization;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class LoggerTargetClassEmitter
{
	static void EmitFields(
		LoggerTarget target,
		CodeWriter writer,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		writer
			.Write("readonly ")
			.Write(Constants.Logging.MicrosoftExtensions.ILogger)
			.Write('<')
			.Write(target.InterfaceType)
			.Write('>')
			.Write(' ')
			.Write(Constants.Logging.LoggerFieldName)
			.Write(';')
			.NewLine()
			.NewLine();

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

			EmitLogActionField(writer, methodTarget, emitNullable);
		}
	}

	internal static void EmitLogActionField(CodeWriter writer, LogMethodTarget methodTarget, bool emitNullable = true)
	{
		writer
			.Write("static readonly ")
			.Write(methodTarget.IsScoped ? Constants.System.Func : Constants.System.Action)
			.Write('<')
			.Write(Constants.Logging.MicrosoftExtensions.ILogger)
			.Write(", ");

		foreach (var parameter in methodTarget.ParametersSansException)
			writer.Write(parameter.ParameterType).Write(", ");

		if (methodTarget.IsScoped)
		{
			writer.Write(Constants.System.IDisposable);
			if (emitNullable)
				writer.Write('?');
			writer.Write("> ");
		}
		else
		{
			writer.Write(Constants.System.Exception);
			if (emitNullable)
				writer.Write('?');
			writer.Write("> ");
		}

		writer
			.Write(methodTarget.LoggerActionFieldName)
			.Write(" = ")
			.Write(Constants.Logging.MicrosoftExtensions.LoggerMessage)
			.Write(".Define");

		if (methodTarget.IsScoped)
			writer.Write("Scope");

		if (methodTarget.ParameterCountSansException > 0)
		{
			writer.Write('<');

			var i = 0;
			foreach (var parameter in methodTarget.ParametersSansException)
			{
				writer.Write(parameter.ParameterType);
				if (i < methodTarget.ParameterCountSansException - 1)
					writer.Write(", ");

				i++;
			}

			writer.Write('>');
		}

		writer.Write('(');

		if (!methodTarget.IsScoped)
		{
			writer.Write(methodTarget.MSLevel).Write(", ");

			var eventId = methodTarget.EventId ?? SharedHelpers.GetNonRandomizedHashCode(methodTarget.MethodName);
			writer
				.Write("new ")
				.Write(Constants.Logging.MicrosoftExtensions.EventId)
				.Write('(')
				.Write(eventId.ToString(CultureInfo.InvariantCulture))
				.Write(", \"")
				.Write(methodTarget.LogName)
				.Write("\"), ");
		}

		writer.Write('"').Write(methodTarget.MessageTemplate).Write('"').Write(");").NewLine();
	}
}
