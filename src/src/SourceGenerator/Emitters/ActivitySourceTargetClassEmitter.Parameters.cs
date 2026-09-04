using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class ActivitySourceTargetClassEmitter
{
	static void EmitTagsOrBaggageParameters(
		CodeWriter writer,
		string activityVariableName,
		bool populateTags,
		ActivityBasedGenerationTarget method,
		bool checkForNullableActivity,
		ActivityOutputContext output
	)
	{
		var parameters = populateTags ? method.Tags : method.Baggage;
		if (parameters.Count == 0)
			return;

		var useRecordedExceptionRules =
			method.EventAttribute?.UseRecordExceptionRules ?? PropertyLibrary.Activities.UseRecordExceptionRulesDefault;

		void EmitParameter(ActivityBasedParameterTarget param)
		{
			writer
				.Write(activityVariableName)
				.Write('.')
				.Write(populateTags ? "SetTag" : "SetBaggage")
				.Write('(')
				.Write(param.GeneratedName.Wrap())
				.Write(", ")
				.Write(param.ParameterName);

			if (!populateTags && param.ParameterType.Identity.SpecialType != SpecialType.System_String)
			{
				output.Context.Diagnostic("Found a baggage parameter type that is not a string.");

				if (param.ParameterType.IsNullable)
					writer.Write('?');

				writer.Write(".ToString()");
			}

			writer.Line(");");
		}

		void EmitParameters()
		{
			foreach (var param in parameters)
			{
				if (populateTags && param.IsException && useRecordedExceptionRules)
					continue;

				if (param.SkipOnNullOrEmpty)
				{
					writer.IfBlock(param.ParameterName + " != default", _ => EmitParameter(param));
				}
				else
				{
					EmitParameter(param);
				}
			}
		}

		if (checkForNullableActivity)
		{
			writer.NewLine().IfBlock(activityVariableName + " != null", _ => EmitParameters());
		}
		else
		{
			EmitParameters();
		}
	}

	static bool GuardParameters(
		ActivityBasedGenerationTarget methodTarget,
		ActivityOutputContext output,
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
			.Parameters.Where(m => m.ParamDestination == ActivityParameterDestination.ParentContextOrId)
			.ToImmutableArray();
		var tagsParams = methodTarget
			.Parameters.Where(m => m.ParamDestination == ActivityParameterDestination.TagsEnumerable)
			.ToImmutableArray();
		var linksParams = methodTarget
			.Parameters.Where(m => m.ParamDestination == ActivityParameterDestination.LinksEnumerable)
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
			.Parameters.Where(m => m.ParamDestination == ActivityParameterDestination.StatusDescription)
			.ToImmutableArray();

		if (activityParams.Length > 1)
		{
			output.Context.Diagnostic("More than one activity parameter defined.");

			return false;
		}
		else
		{
			activityParam = activityParams.FirstOrDefault();
		}

		if (parentContextOrIdParams.Length > 1)
		{
			output.Context.Diagnostic("More than one parent context/ id defined.");

			return false;
		}
		else
		{
			parentContextOrId = parentContextOrIdParams.FirstOrDefault();
		}

		if (tagsParams.Length > 1)
		{
			output.Context.Diagnostic("More than one tag IEnumerable defined.");

			return false;
		}
		else
		{
			tagsParam = tagsParams.FirstOrDefault();
		}

		if (linksParams.Length > 1)
		{
			output.Context.Diagnostic("More than one ActivityLink/ IEnumerable of ActivityLink is defined.");

			return false;
		}
		else
		{
			linksParam = linksParams.FirstOrDefault();
		}

		if (escapeParams.Length > 1)
		{
			output.Context.Diagnostic("More than one Escape parameter defined.");

			return false;
		}
		else
		{
			escapeParam = escapeParams.FirstOrDefault();
			if (escapeParam != null)
			{
				if (escapeParam.ParameterType.Identity.SpecialType != SpecialType.System_Boolean)
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
			output.Context.Diagnostic("More than one StatusDescription parameter defined.");

			return false;
		}
		else
		{
			statusDescriptionParam = statusDescriptionParams.FirstOrDefault();
			if (statusDescriptionParam != null)
			{
				if (statusDescriptionParam.ParameterType.Identity.SpecialType != SpecialType.System_String)
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
