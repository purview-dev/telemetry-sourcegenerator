using System.Globalization;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class LoggerTargetClassEmitter
{
	static void EmitFields(LoggerOutputContext output, CodeWriter writer, SourceProductionContext context)
	{
		var target = output.Target;

		context.CancellationToken.ThrowIfCancellationRequested();

		writer
			.WriteField(
				new FieldDeclarationOptions(
					PropertyLibrary.Logging.LoggerFieldName,
					TypeLibrary.Logging.MicrosoftExtensions.ILogger.MakeGeneric(target.InterfaceType).AsTypeReference()
				)
				{
					IsReadOnly = true,
					IncludeGeneratedAttributes = false,
				}
			)
			.NewLine()
			.NewLine();

		foreach (var methodTarget in target.LogMethods)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			if (!methodTarget.TargetGenerationState.IsValid)
			{
				if (methodTarget.TargetGenerationState.RaiseMultiGenerationTargetsNotSupported)
				{
					output.Context.Debug(
						$"Identified {target.InterfaceType.Identity.Name}.{methodTarget.MethodName} as problematic as it has another target types."
					);
				}
				else if (methodTarget.TargetGenerationState.RaiseInferenceNotSupportedWithMultiTargeting)
				{
					output.Context.Debug(
						$"Identified {target.InterfaceType.Identity.Name}.{methodTarget.MethodName} as problematic as it is inferred."
					);
				}

				continue;
			}

			// Report warning for Activity parameter without Activity target
			if (methodTarget.TargetGenerationState.ActivityParameterWithoutTarget != null)
			{
				output.Context.Debug(
					$"Activity parameter '{methodTarget.TargetGenerationState.ActivityParameterWithoutTarget}' on {methodTarget.MethodName} has no Activity target."
				);
			}

			if (methodTarget.UnknownReturnType)
			{
				continue;
			}

			if (methodTarget.HasMultipleExceptions)
			{
				output.Context.Diagnostic("Method has multiple exception parameters, only a single one is permitted.");

				continue;
			}

			if (methodTarget.ParameterCountSansException > PropertyLibrary.Logging.MaxNonExceptionParameters)
			{
				output.Context.Diagnostic("Method has more than 6 parameters.");

				continue;
			}

			if (methodTarget.InferredErrorLevel)
			{
				output.Context.Diagnostic("Inferring error log level.");
			}

			EmitLogActionField(writer, methodTarget);
		}
	}

	internal static void EmitLogActionField(CodeWriter writer, LogMethodTarget methodTarget)
	{
		var useNullable = writer.IsNullableContextEnabled is null or true;

		var typeName =
			(methodTarget.IsScoped ? "global::System.Func<" : "global::System.Action<")
			+ TypeLibrary.Logging.MicrosoftExtensions.ILogger.RenderFullNameForNullable(useNullable)
			+ string.Concat(
				methodTarget.ParametersSansException.Select(p =>
					", " + p.ParameterType.RenderFullNameForNullable(useNullable)
				)
			)
			+ ", "
			+ (
				methodTarget.IsScoped
					? TypeLibrary.System.IDisposable.MakeNullable(writer).RenderFullNameForNullable(useNullable)
					: TypeLibrary.System.Exception.MakeNullable(writer).RenderFullNameForNullable(useNullable)
			)
			+ ">";

		var genericArguments =
			methodTarget.ParameterCountSansException > 0
				? "<"
					+ string.Join(
						", ",
						methodTarget.ParametersSansException.Select(p =>
							p.ParameterType.RenderFullNameForNullable(useNullable)
						)
					)
					+ ">"
				: "";

		var eventId = methodTarget.EventId ?? SharedHelpers.GetNonRandomizedHashCode(methodTarget.MethodName);
		var arguments = methodTarget.IsScoped
			? $"\"{methodTarget.MessageTemplate}\""
			: $"{methodTarget.MSLevel}, new global::Microsoft.Extensions.Logging.EventId({eventId.ToString(CultureInfo.InvariantCulture)}, \"{methodTarget.LogName}\"), \"{methodTarget.MessageTemplate}\"";

		var initializer =
			$"global::Microsoft.Extensions.Logging.LoggerMessage.Define"
			+ (methodTarget.IsScoped ? "Scope" : "")
			+ genericArguments
			+ $"({arguments})";

		writer.WriteField(
			new FieldDeclarationOptions(
				methodTarget.LoggerActionFieldName,
				new TypeReference(new TypeIdentity(typeName, null))
			)
			{
				IsStatic = true,
				IsReadOnly = true,
				Initializer = initializer,
				IncludeGeneratedAttributes = false,
			}
		);
	}
}
