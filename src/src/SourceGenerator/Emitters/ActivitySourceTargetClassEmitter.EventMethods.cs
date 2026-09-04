using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class ActivitySourceTargetClassEmitter
{
	static void EmitEventMethodBody(
		ActivityOutputContext output,
		ActivityBasedGenerationTarget methodTarget,
		CodeWriter writer,
		SourceProductionContext context
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		if (
			!GuardParameters(
				methodTarget,
				output,
				out var activityParam,
				out var parentContextOrId,
				out var tagsParam,
				out var linksParam,
				out var startTimeParam,
				out var timestampParam,
				out var escapeParam,
				out var statusDescriptionParam
			)
		)
		{
			return;
		}

		var activityVariableName =
			activityParam?.ParameterName ?? (TypeLibrary.Activities.SystemDiagnostics.Activity + ".Current");
		if (parentContextOrId != null)
		{
			output.Context.Diagnostic("Parent context/ Id not allowed on event method, only activities.");

			return;
		}

		if (linksParam != null)
		{
			output.Context.Diagnostic("Links parameter not allowed on event method, only activities.");

			return;
		}

		if (startTimeParam != null)
		{
			output.Context.Diagnostic("Start time parameter not allowed on event method, only activities.");

			return;
		}

		EmitHasListenersTest(writer, methodTarget);

		writer.IfBlock(
			activityVariableName + " != null",
			body =>
			{
				var exceptionParam =
					methodTarget.Parameters.FirstOrDefault(m => m.IsException)
					?? methodTarget.Tags.FirstOrDefault(m => m.IsException);
				var tagsParameterName = EmitEventTags(
					writer,
					methodTarget,
					activityVariableName,
					tagsParam,
					escapeParam
				);

				var eventVariableName = "activityEvent" + methodTarget.MethodName;

				writer
					.NewLine()
					.Write(TypeLibrary.Activities.SystemDiagnostics.ActivityEvent)
					.Write(' ')
					.Write(eventVariableName)
					.Write(" = new ")
					// Use explicit type for C# 7.3 compatibility (target-typed new() requires C# 9+)
					.Write(TypeLibrary.Activities.SystemDiagnostics.ActivityEvent)
					.Write("(name: ")
					.Write(methodTarget.ActivityOrEventName.Wrap())
					// timestamp:
					.Write(", timestamp: ")
					.Write(timestampParam?.ParameterName ?? "default")
					// tags:
					.Write(", tags: ")
					.Write(tagsParameterName)
					.Line(");");

				writer.NewLine().Write(activityVariableName).Write(".AddEvent(").Write(eventVariableName).Line(");");

				if (methodTarget.Baggage.Count > 0)
				{
					writer.NewLine();

					EmitTagsOrBaggageParameters(writer, activityVariableName, false, methodTarget, false, output);
				}

				EmitSetStatus(writer, methodTarget, activityVariableName, statusDescriptionParam, exceptionParam);
			}
		);

		context.CancellationToken.ThrowIfCancellationRequested();

		if (methodTarget.ReturnType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.Activity))
		{
			writer.NewLine().Write("return ").Write(activityVariableName).Write(";").NewLine();
		}
	}

	static string EmitEventTags(
		CodeWriter writer,
		ActivityBasedGenerationTarget methodTarget,
		string activityVariableName,
		ActivityBasedParameterTarget? tagsParam,
		ActivityBasedParameterTarget? escapeParam
	)
	{
		var tagsParameterName = tagsParam?.ParameterName ?? "default";
		if (methodTarget.Tags.Count == 0)
			return tagsParameterName;

		var tagsListVariableName = "tagsCollection" + methodTarget.MethodName;
		writer
			.Write(TypeLibrary.Activities.SystemDiagnostics.ActivityTagsCollection)
			.Write(' ')
			.Write(tagsListVariableName)
			.Write(
				" = new(" // : $" = new {TypeLibrary.Activities.SystemDiagnostics.ActivityTagsCollection}("
			);

		if (tagsParam != null)
			writer.Write(tagsParam.ParameterName);

		writer.Line(");");

		var useRecordedExceptionRules =
			methodTarget.EventAttribute?.UseRecordExceptionRules
			?? PropertyLibrary.Activities.UseRecordExceptionRulesDefault;

		var escapeValue = escapeParam?.ParameterName ?? "true";
		foreach (var tagParam in methodTarget.Tags)
		{
			var emitTag =
				tagParam.IsException
				&& methodTarget.ActivityOrEventName != PropertyLibrary.Activities.Tag_ExceptionEventName
				&& useRecordedExceptionRules;

			void EmitTag()
			{
				if (tagParam.IsException)
				{
					if (methodTarget.ActivityOrEventName == PropertyLibrary.Activities.Tag_ExceptionEventName)
					{
						writer.IfBlock(
							tagParam.ParameterName + " != null",
							body =>
								// We want the details inside of the current event.
								EmitExceptionParam(writer, tagsListVariableName, escapeValue, tagParam.ParameterName)
						);
					}
					else
					{
						if (useRecordedExceptionRules)
						{
							writer
								.NewLine()
								.Write(PropertyLibrary.Activities.RecordExceptionMethodName)
								.Write("(activity: ")
								.Write(activityVariableName)
								.Write(", exception: ")
								.Write(tagParam.ParameterName)
								.Write(", escape: ")
								.Write(escapeValue)
								.Line(");");
						}
						else
						{
							writer.MethodCallOn(
								tagsListVariableName,
								"Add",
								tagParam.GeneratedName.Wrap(),
								tagParam.ParameterName + ".ToString()"
							);
						}
					}
				}
				else
				{
					writer
						.Write(tagsListVariableName)
						.Write(".Add(")
						.Write(tagParam.GeneratedName.Wrap())
						.Write(", ")
						.Write(tagParam.ParameterName)
						.Line(");");
				}
			}

			if (tagParam.SkipOnNullOrEmpty)
			{
				writer.IfBlock(tagParam.ParameterName + " != default", _ => EmitTag());
			}
			else
			{
				EmitTag();
			}
		}

		return tagsListVariableName;
	}

	static void EmitSetStatus(
		CodeWriter writer,
		ActivityBasedGenerationTarget methodTarget,
		string activityVariableName,
		ActivityBasedParameterTarget? statusDescriptionParam,
		ActivityBasedParameterTarget? exceptionParam
	)
	{
		var statusCode = methodTarget.EventAttribute?.StatusCode ?? 0;
		if (statusCode == 0)
			return;

		writer
			.NewLine()
			.Write(activityVariableName)
			.Write(".SetStatus(")
			.Write(PropertyLibrary.Activities.ActivityStatusCodeMap[statusCode]);

		// Error
		if (statusCode == 2)
		{
			if (statusDescriptionParam != null)
			{
				writer.Write(", ").Write(statusDescriptionParam.ParameterName);
			}
			else if (!string.IsNullOrWhiteSpace(methodTarget.EventAttribute?.StatusDescription))
			{
				writer.Write(", ").Write(methodTarget.EventAttribute!.Value.StatusDescription!.Wrap());
			}
			else if (exceptionParam != null)
			{
				writer.Write(", ").Write(exceptionParam.ParameterName).Write("?.Message");
			}
		}

		writer.Line(");");
	}
}
