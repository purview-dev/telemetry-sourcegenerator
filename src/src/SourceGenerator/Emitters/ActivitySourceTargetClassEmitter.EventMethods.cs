using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class ActivitySourceTargetClassEmitter
{
	static void EmitEventMethodBody(
		CodeWriter writer,
		ActivityBasedGenerationTarget methodTarget,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable = true
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		if (
			!GuardParameters(
				methodTarget,
				context,
				logger,
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
			activityParam?.ParameterName ?? (Constants.Activities.SystemDiagnostics.Activity + ".Current");
		if (parentContextOrId != null)
		{
			logger?.Diagnostic("Parent context/ Id not allowed on event method, only activities.");

			return;
		}

		if (linksParam != null)
		{
			logger?.Diagnostic("Links parameter not allowed on event method, only activities.");

			return;
		}

		if (startTimeParam != null)
		{
			logger?.Diagnostic("Start time parameter not allowed on event method, only activities.");

			return;
		}

		EmitHasListenersTest(writer, methodTarget, emitNullable);

		writer.Write("if (").Write(activityVariableName).WriteLine(" != null)");

		using (writer.OpenBlockScope())
		{
			var tagsParameterName = tagsParam?.ParameterName ?? "default";
			var exceptionParam =
				methodTarget.Parameters.FirstOrDefault(m => m.IsException)
				?? methodTarget.Tags.FirstOrDefault(m => m.IsException);
			if (methodTarget.Tags.Count > 0)
			{
				var tagsListVariableName = "tagsCollection" + methodTarget.MethodName;
				writer
					.Write(Constants.Activities.SystemDiagnostics.ActivityTagsCollection)
					.Write(' ')
					.Write(tagsListVariableName)
					.Write(
						emitNullable
							? " = new("
							: $" = new {Constants.Activities.SystemDiagnostics.ActivityTagsCollection}("
					);

				if (tagsParam != null)
					writer.Write(tagsParam.ParameterName);

				writer.WriteLine(");");

				var useRecordedExceptionRules = Constants.Activities.UseRecordExceptionRulesDefault;
				var emitExceptionEscape = escapeParam != null || Constants.Activities.RecordExceptionEscapedDefault;
				if (methodTarget.EventAttribute?.UseRecordExceptionRules.IsSet == true)
				{
					useRecordedExceptionRules = methodTarget.EventAttribute.UseRecordExceptionRules.Value!.Value;
				}

				if (methodTarget.EventAttribute?.RecordExceptionEscape.IsSet == true)
				{
					emitExceptionEscape = methodTarget.EventAttribute.RecordExceptionEscape!.Value!.Value;
				}

				var escapeValue = escapeParam?.ParameterName ?? "true";
				foreach (var tagParam in methodTarget.Tags)
				{
					var emitTag =
						tagParam.IsException
						&& methodTarget.ActivityOrEventName != Constants.Activities.Tag_ExceptionEventName
						&& useRecordedExceptionRules;

					void EmitTag()
					{
						if (tagParam.IsException)
						{
							if (methodTarget.ActivityOrEventName == Constants.Activities.Tag_ExceptionEventName)
							{
								writer.Write("if (").Write(tagParam.ParameterName).WriteLine(" != null)");
								using (writer.OpenBlockScope())
								{
									// We want the details inside of the current event.
									EmitExceptionParam(
										writer,
										tagsListVariableName,
										escapeValue,
										tagParam.ParameterName
									);
								}
							}
							else
							{
								if (useRecordedExceptionRules)
								{
									writer
										.NewLine()
										.Write(Constants.Activities.RecordExceptionMethodName)
										.Write("(activity: ")
										.Write(activityVariableName)
										.Write(", exception: ")
										.Write(tagParam.ParameterName)
										.Write(", escape: ")
										.Write(escapeValue)
										.WriteLine(");");
								}
								else
								{
									writer
										.Write(tagsListVariableName)
										.Write(".Add(")
										.Write(tagParam.GeneratedName.Wrap())
										.Write(", ")
										.Write(tagParam.ParameterName)
										.WriteLine(".ToString());");
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
								.WriteLine(");");
						}
					}

					if (tagParam.SkipOnNullOrEmpty)
					{
						writer.Write("if (").Write(tagParam.ParameterName).WriteLine(" != default)");
						using (writer.OpenBlockScope())
							EmitTag();
					}
					else
					{
						EmitTag();
					}
				}

				tagsParameterName = tagsListVariableName;
			}

			var eventVariableName = "activityEvent" + methodTarget.MethodName;

			writer
				.NewLine()
				.Write(Constants.Activities.SystemDiagnostics.ActivityEvent)
				.Write(' ')
				.Write(eventVariableName)
				.Write(" = new ")
				// Use explicit type for C# 7.3 compatibility (target-typed new() requires C# 9+)
				.Write(Constants.Activities.SystemDiagnostics.ActivityEvent)
				.Write("(name: ")
				.Write(methodTarget.ActivityOrEventName.Wrap())
				// timestamp:
				.Write(", timestamp: ")
				.Write(timestampParam?.ParameterName ?? "default")
				// tags:
				.Write(", tags: ")
				.Write(tagsParameterName)
				.WriteLine(");");

			writer.NewLine().Write(activityVariableName).Write(".AddEvent(").Write(eventVariableName).WriteLine(");");

			if (methodTarget.Baggage.Count > 0)
			{
				writer.NewLine();

				EmitTagsOrBaggageParameters(writer, activityVariableName, false, methodTarget, false, context, logger);
			}

			var statusCode = methodTarget.EventAttribute?.StatusCode.Value ?? 0;
			if (statusCode != 0)
			{
				writer
					.NewLine()
					.Write(activityVariableName)
					.Write(".SetStatus(")
					.Write(Constants.Activities.ActivityStatusCodeMap[statusCode]);

				// Error
				if (statusCode == 2)
				{
					if (statusDescriptionParam != null)
					{
						writer.Write(", ").Write(statusDescriptionParam.ParameterName);
					}
					else if (methodTarget.EventAttribute!.StatusDescription.IsSet)
					{
						writer.Write(", ").Write(methodTarget.EventAttribute!.StatusDescription.Value!.Wrap());
					}
					else if (exceptionParam != null)
					{
						writer.Write(", ").Write(exceptionParam.ParameterName).Write("?.Message");
					}
				}

				writer.WriteLine(");");
			}
		}

		context.CancellationToken.ThrowIfCancellationRequested();

		if (Constants.Activities.SystemDiagnostics.Activity.Equals(methodTarget.ReturnType))
		{
			writer.NewLine().Write("return ").Write(activityVariableName).Write(";").NewLine();
		}
	}
}
