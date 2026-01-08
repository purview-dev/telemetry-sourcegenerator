using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class ActivitySourceTargetClassEmitter
{
	static void EmitEventMethodBody(
		StringBuilder builder,
		int indent,
		ActivityBasedGenerationTarget methodTarget,
		SourceProductionContext context,
		GenerationLogger? logger
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
			activityParam?.ParameterName
			?? (Constants.Activities.SystemDiagnostics.Activity + ".Current");
		if (parentContextOrId != null)
		{
			logger?.Diagnostic("Parent context/ Id not allowed on event method, only activities.");

			TelemetryDiagnostics.Report(
				context.ReportDiagnostic,
				TelemetryDiagnostics.Activities.ParentContextOrIdParameterNotAllowed,
				parentContextOrId.Locations,
				parentContextOrId.ParameterName
			);

			return;
		}

		if (linksParam != null)
		{
			logger?.Diagnostic("Links parameter not allowed on event method, only activities.");

			TelemetryDiagnostics.Report(
				context.ReportDiagnostic,
				TelemetryDiagnostics.Activities.LinksParameterNotAllowed,
				linksParam.Locations,
				linksParam.ParameterName
			);

			return;
		}

		if (startTimeParam != null)
		{
			logger?.Diagnostic(
				"Start time parameter not allowed on event method, only activities."
			);

			TelemetryDiagnostics.Report(
				context.ReportDiagnostic,
				TelemetryDiagnostics.Activities.StartTimeParameterNotAllowed,
				startTimeParam.Locations,
				startTimeParam.ParameterName
			);

			return;
		}

		EmitHasListenersTest(builder, indent, methodTarget);

		builder
			.Append(indent, "if (", withNewLine: false)
			.Append(activityVariableName)
			.AppendLine(" != null)")
			.Append(indent, '{');

		indent++;

		var tagsParameterName = tagsParam?.ParameterName ?? "default";
		var exceptionParam =
			methodTarget.Parameters.FirstOrDefault(m => m.IsException)
			?? methodTarget.Tags.FirstOrDefault(m => m.IsException);
		if (methodTarget.Tags.Length > 0)
		{
			var tagsListVariableName = "tagsCollection" + methodTarget.MethodName;
			builder
				.Append(
					indent,
					Constants.Activities.SystemDiagnostics.ActivityTagsCollection,
					withNewLine: false
				)
				.Append(' ')
				.Append(tagsListVariableName)
				.Append(" = new(");

			if (tagsParam != null)
				builder.Append(tagsParam.ParameterName);

			builder.AppendLine(");");

			var useRecordedExceptionRules = Constants.Activities.UseRecordExceptionRulesDefault;
			var emitExceptionEscape =
				escapeParam != null || Constants.Activities.RecordExceptionEscapedDefault;
			if (methodTarget.EventAttribute?.UseRecordExceptionRules.IsSet == true)
			{
				useRecordedExceptionRules = methodTarget
					.EventAttribute
					.UseRecordExceptionRules
					.Value!
					.Value;
			}

			if (methodTarget.EventAttribute?.RecordExceptionEscape.IsSet == true)
			{
				emitExceptionEscape = methodTarget
					.EventAttribute
					.RecordExceptionEscape!
					.Value!
					.Value;
			}

			var escapeValue = escapeParam?.ParameterName ?? "true";
			foreach (var tagParam in methodTarget.Tags)
			{
				if (tagParam.SkipOnNullOrEmpty)
				{
					builder
						.Append(indent, "if (", withNewLine: false)
						.Append(tagParam.ParameterName)
						.AppendLine(" != default)")
						.Append(indent, "{");

					indent++;
				}

				if (tagParam.IsException)
				{
					if (
						methodTarget.ActivityOrEventName
						== Constants.Activities.Tag_ExceptionEventName
					)
					{
						builder
							.Append(indent, "if (", withNewLine: false)
							.Append(tagParam.ParameterName)
							.AppendLine(" != null)")
							.Append(indent, '{');

						// We want the details inside of the current event.
						EmitExceptionParam(
							builder,
							indent + 1,
							tagsListVariableName,
							escapeValue,
							tagParam.ParameterName
						);

						builder.Append(indent, '}');
					}
					else
					{
						if (useRecordedExceptionRules)
						{
							builder
								.AppendLine()
								.Append(
									indent,
									Constants.Activities.RecordExceptionMethodName,
									withNewLine: false
								)
								.Append("(activity: ")
								.Append(activityVariableName)
								.Append(", exception: ")
								.Append(tagParam.ParameterName)
								.Append(", escape: ")
								.Append(escapeValue)
								.AppendLine(");");
						}
						else
						{
							builder
								.Append(indent, tagsListVariableName, withNewLine: false)
								.Append(".Add(")
								.Append(tagParam.GeneratedName.Wrap())
								.Append(", ")
								.Append(tagParam.ParameterName)
								.AppendLine(".ToString());");
						}
					}
				}
				else
				{
					builder
						.Append(indent, tagsListVariableName, withNewLine: false)
						.Append(".Add(")
						.Append(tagParam.GeneratedName.Wrap())
						.Append(", ")
						.Append(tagParam.ParameterName)
						.AppendLine(");");
				}

				if (tagParam.SkipOnNullOrEmpty)
					builder.Append(--indent, "}");
			}

			tagsParameterName = tagsListVariableName;
		}

		var eventVariableName = "activityEvent" + methodTarget.MethodName;

		builder
			.AppendLine()
			.Append(
				indent,
				Constants.Activities.SystemDiagnostics.ActivityEvent,
				withNewLine: false
			)
			.Append(' ')
			.Append(eventVariableName)
			.Append(" = new")
			// name:
			.Append("(name: ")
			.Append(methodTarget.ActivityOrEventName.Wrap())
			// timestamp:
			.Append(", timestamp: ")
			.Append(timestampParam?.ParameterName ?? "default")
			// tags:
			.Append(", tags: ")
			.Append(tagsParameterName)
			.AppendLine(");");

		builder
			.AppendLine()
			.Append(indent, activityVariableName, withNewLine: false)
			.Append(".AddEvent(")
			.Append(eventVariableName)
			.AppendLine(");");

		if (methodTarget.Baggage.Length > 0)
		{
			builder.AppendLine();

			EmitTagsOrBaggageParameters(
				builder,
				indent,
				activityVariableName,
				false,
				methodTarget,
				false,
				context,
				logger
			);
		}

		var statusCode = methodTarget.EventAttribute?.StatusCode.Value ?? 0;
		if (statusCode != 0)
		{
			builder
				.AppendLine()
				.Append(indent, activityVariableName, withNewLine: false)
				.Append(".SetStatus(")
				.Append(Constants.Activities.ActivityStatusCodeMap[statusCode]);

			// Error
			if (statusCode == 2)
			{
				if (statusDescriptionParam != null)
				{
					builder.Append(", ").Append(statusDescriptionParam.ParameterName);
				}
				else if (methodTarget.EventAttribute!.StatusDescription.IsSet)
				{
					builder
						.Append(", ")
						.Append(methodTarget.EventAttribute!.StatusDescription.Value!.Wrap());
				}
				else if (exceptionParam != null)
				{
					builder.Append(", ").Append(exceptionParam.ParameterName).Append("?.Message");
				}
			}

			builder.AppendLine(");");
		}

		builder.Append(--indent, '}');

		context.CancellationToken.ThrowIfCancellationRequested();

		if (Constants.Activities.SystemDiagnostics.Activity.Equals(methodTarget.ReturnType))
		{
			builder
				.AppendLine()
				.Append(indent, "return ", withNewLine: false)
				.Append(activityVariableName)
				.AppendLine(';');
		}
	}
}
