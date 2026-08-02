using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class ActivitySourceTargetClassEmitter
{
	static void EmitTagsOrBaggageParameters(
		StringBuilder builder,
		int indent,
		string activityVariableName,
		bool populateTags,
		ActivityBasedGenerationTarget method,
		bool checkForNullableActivity,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		var parameters = populateTags ? method.Tags : method.Baggage;
		if (parameters.Length == 0)
			return;

		if (checkForNullableActivity)
		{
			builder
				.AppendLine()
				.Append(indent, "if (", withNewLine: false)
				.Append(activityVariableName)
				.AppendLine(" != null)")
				.Append(indent, '{');

			indent++;
		}

		var useRecordedExceptionRules = Constants.Activities.UseRecordExceptionRulesDefault;
		if (method.EventAttribute?.UseRecordExceptionRules.IsSet == true)
			useRecordedExceptionRules = method.EventAttribute.UseRecordExceptionRules.Value!.Value;

		foreach (var param in parameters)
		{
			if (populateTags && param.IsException && useRecordedExceptionRules)
				continue;

			if (param.SkipOnNullOrEmpty)
			{
				builder
					.Append(indent, "if (", withNewLine: false)
					.Append(param.ParameterName)
					.AppendLine(" != default)")
					.Append(indent, "{");

				indent++;
			}

			builder
				.Append(indent, activityVariableName, withNewLine: false)
				.Append('.')
				.Append(populateTags ? "SetTag" : "SetBaggage")
				.Append('(')
				.Append(param.GeneratedName.Wrap())
				.Append(", ")
				.Append(param.ParameterName);

			if (!populateTags && param.ParameterType.SpecialType != SpecialType.System_String)
			{
				logger?.Diagnostic("Found a baggage parameter type that is not a string.");
				TelemetryDiagnostics.Report(
					context.ReportDiagnostic,
					TelemetryDiagnostics.Activities.BaggageParameterShouldBeString
				);

				if (param.ParameterType.IsNullable)
					builder.Append('?');

				builder.Append(".ToString()");
			}

			builder.AppendLine(");");

			if (param.SkipOnNullOrEmpty)
				builder.Append(--indent, "}");
		}

		if (checkForNullableActivity)
			builder.Append(--indent, '}');
	}

	static bool GuardParameters(
		ActivityBasedGenerationTarget methodTarget,
		SourceProductionContext _,
		GenerationLogger? logger,
		out ActivityBasedParameterTarget? activityParam,
		out ActivityBasedParameterTarget? parentContextOrId,
		out ActivityBasedParameterTarget? tagsParam,
		out ActivityBasedParameterTarget? linksParam,
		out ActivityBasedParameterTarget? startTimeParam,
		out ActivityBasedParameterTarget? timestampParam,
		out ActivityBasedParameterTarget? escapeParam,
		out ActivityBasedParameterTarget? statusDescriptionParam
	)
	{
		activityParam = null;
		parentContextOrId = null;
		tagsParam = null;
		linksParam = null;
		startTimeParam = null;
		timestampParam = null;
		escapeParam = null;
		statusDescriptionParam = null;

		var activityParams = methodTarget
			.Parameters.Where(m => m.ParamDestination == ActivityParameterDestination.Activity)
			.ToImmutableArray();
		var parentContextOrIdParams = methodTarget
			.Parameters.Where(m =>
				m.ParamDestination == ActivityParameterDestination.ParentContextOrId
			)
			.ToImmutableArray();
		var tagsParams = methodTarget
			.Parameters.Where(m =>
				m.ParamDestination == ActivityParameterDestination.TagsEnumerable
			)
			.ToImmutableArray();
		var linksParams = methodTarget
			.Parameters.Where(m =>
				m.ParamDestination == ActivityParameterDestination.LinksEnumerable
			)
			.ToImmutableArray();
		var startTimeParams = methodTarget
			.Parameters.Where(m => m.ParamDestination == ActivityParameterDestination.StartTime)
			.ToImmutableArray();
		var timestampParams = methodTarget
			.Parameters.Where(m => m.ParamDestination == ActivityParameterDestination.Timestamp)
			.ToImmutableArray();
		var escapeParams = methodTarget
			.Parameters.Where(m => m.ParamDestination == ActivityParameterDestination.Escape)
			.ToImmutableArray();
		var statusDescriptionParams = methodTarget
			.Parameters.Where(m =>
				m.ParamDestination == ActivityParameterDestination.StatusDescription
			)
			.ToImmutableArray();

		if (activityParams.Length > 1)
		{
			logger?.Diagnostic("More than one activity parameter defined.");

			return false;
		}
		else
		{
			activityParam = activityParams.FirstOrDefault();
		}

		if (parentContextOrIdParams.Length > 1)
		{
			logger?.Diagnostic("More than one parent context/ id defined.");

			return false;
		}
		else
		{
			parentContextOrId = parentContextOrIdParams.FirstOrDefault();
		}

		if (tagsParams.Length > 1)
		{
			logger?.Diagnostic("More than one tag IEnumerable defined.");

			return false;
		}
		else
		{
			tagsParam = tagsParams.FirstOrDefault();
		}

		if (linksParams.Length > 1)
		{
			logger?.Diagnostic(
				"More than one ActivityLink/ IEnumerable of ActivityLink is defined."
			);

			return false;
		}
		else
		{
			linksParam = linksParams.FirstOrDefault();
		}

		if (escapeParams.Length > 1)
		{
			logger?.Diagnostic("More than one Escape parameter defined.");

			return false;
		}
		else
		{
			escapeParam = escapeParams.FirstOrDefault();
			if (escapeParam != null)
			{
				if (escapeParam.ParameterType.SpecialType != SpecialType.System_Boolean)
				{
					return false;
				}

				if (methodTarget.MethodType != ActivityMethodType.Event)
				{
					return false;
				}
			}
		}

		if (statusDescriptionParams.Length > 1)
		{
			logger?.Diagnostic("More than one StatusDescription parameter defined.");

			return false;
		}
		else
		{
			statusDescriptionParam = statusDescriptionParams.FirstOrDefault();
			if (statusDescriptionParam != null)
			{
				if (statusDescriptionParam.ParameterType.SpecialType != SpecialType.System_String)
				{
					return false;
				}

				if (methodTarget.MethodType != ActivityMethodType.Event)
				{
					return false;
				}
			}
		}

		// There can be only one as it's checked on the
		// combination of parameter name and type.
		startTimeParam = startTimeParams.FirstOrDefault();
		timestampParam = timestampParams.FirstOrDefault();

		return true;
	}
}
