using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class ActivitySourceTargetClassEmitter
{
	static void EmitActivityMethodBody(
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
				out var _,
				out var _
			)
		)
		{
			return;
		}

		if (activityParam != null)
		{
			logger?.Diagnostic(
				"Activity parameter not allowed on Activity start/ create method, only event."
			);

			TelemetryDiagnostics.Report(
				context.ReportDiagnostic,
				TelemetryDiagnostics.Activities.ActivityParameterNotAllowed,
				activityParam.Locations,
				activityParam.ParameterName
			);

			return;
		}

		if (timestampParam != null)
		{
			logger?.Diagnostic(
				"Timestamp parameter not allowed on Activity start/ create method, only events."
			);

			TelemetryDiagnostics.Report(
				context.ReportDiagnostic,
				TelemetryDiagnostics.Activities.TimeStampParameterNotAllowed,
				timestampParam.Locations,
				timestampParam.ParameterName
			);

			return;
		}

		EmitHasListenersTest(builder, indent, methodTarget);

		var activityVariableName = "activity" + methodTarget.MethodName;

		builder
			.Append(indent, Constants.Activities.SystemDiagnostics.Activity, withNewLine: false)
			.Append("? ")
			.Append(activityVariableName)
			.Append(" = ")
			.Append(Constants.Activities.ActivitySourceFieldName)
			.Append('.');

		var createOnly = methodTarget.ActivityAttribute?.CreateOnly.Value == true;
		var createActivityMethod = createOnly ? "Create" : "Start";
		var useParentContext = Constants.Activities.SystemDiagnostics.ActivityContext.Equals(
			parentContextOrId?.ParameterType
		);
		var parentContextParameterName = useParentContext ? "parentContext" : "parentId";

		if (createOnly && startTimeParam != null)
		{
			logger?.Diagnostic("StartTime parameter not allowed on Activity create method.");

			TelemetryDiagnostics.Report(
				context.ReportDiagnostic,
				TelemetryDiagnostics.Activities.StartTimeParameterNotAllowed,
				startTimeParam.Locations,
				startTimeParam.ParameterName
			);

			return;
		}

		var kind =
			methodTarget.ActivityAttribute?.Kind.IsSet == true
				? methodTarget.ActivityAttribute.Value.Kind.Value!.Value
				: Constants.Activities.DefaultActivityKind;

		var parentContextOrIdParameterValue = parentContextOrId?.ParameterName ?? "default";
		if (useParentContext && parentContextOrId!.ParameterType.IsNullable)
		{
			// parentContextOrId is not going to be null at this point as
			// we already checked the type.
			// If it's nullable we need to use the null-coalescing operator...
			// and we need to ensure its explicit or the call is ambiguous
			// between ActivityContext and ParentId.
			parentContextOrIdParameterValue += " ?? default";
		}

		builder.Append(createActivityMethod).Append("Activity(");

		if (createOnly || !useParentContext)
		{
			// Only create the name always comes first.
			// If it's start, and we're using an ActivityContext then the
			// name comes last.
			AddActivityNameParameter(builder, methodTarget, false);
			builder.Append(", ");
		}
		;

		builder
			// kind: (un-named)
			.Append(Constants.Activities.ActivityKindTypeMap[kind])
			// parentContext/ parentId:
			.Append(", ")
			.Append(parentContextParameterName)
			.Append(": ")
			.Append(parentContextOrIdParameterValue)
			// tags:
			.Append(", tags: ")
			.Append(tagsParam?.ParameterName ?? "default")
			// links:
			.Append(", links: ")
			.Append(linksParam?.ParameterName ?? "default");

		if (!createOnly)
		{
			builder
				// startTime:
				.Append(", startTime: ")
				.Append(startTimeParam?.ParameterName ?? "default");

			if (useParentContext)
			{
				// If it's a Start and we're using an ActivityContext,
				// the name comes last.
				builder.Append(", ");
				AddActivityNameParameter(builder, methodTarget, true);
			}
		}

		builder.AppendLine(");");

		context.CancellationToken.ThrowIfCancellationRequested();

		EmitTagsOrBaggageParameters(
			builder,
			indent,
			activityVariableName,
			true,
			methodTarget,
			true,
			context,
			logger
		);
		EmitTagsOrBaggageParameters(
			builder,
			indent,
			activityVariableName,
			false,
			methodTarget,
			true,
			context,
			logger
		);

		context.CancellationToken.ThrowIfCancellationRequested();

		if (Constants.Activities.SystemDiagnostics.Activity.Equals(methodTarget.ReturnType))
		{
			builder
				.AppendLine()
				.Append(indent, "return ", withNewLine: false)
				.Append(activityVariableName)
				.AppendLine(';');
		}

		static void AddActivityNameParameter(
			StringBuilder builder,
			ActivityBasedGenerationTarget methodTarget,
			bool useName
		)
		{
			if (useName)
				builder.Append("name: ");

			builder.Append(methodTarget.ActivityOrEventName.Wrap());
		}
	}

	static void EmitHasListenersTest(
		StringBuilder builder,
		int indent,
		ActivityBasedGenerationTarget methodTarget
	)
	{
		var returnsVoid = methodTarget.ReturnType.SpecialType == SpecialType.System_Void;
		builder
			.Append(indent, "if (!", withNewLine: false)
			.Append(Constants.Activities.ActivitySourceFieldName)
			.Append(".HasListeners())")
			.AppendLine()
			.Append(indent, '{')
			.Append(
				indent + 1,
				"return"
					+ (
						returnsVoid
							? null
							: " null" + (methodTarget.ReturnType.IsNullable ? null : "!")
					)
					+ ";"
			)
			.Append(indent, '}')
			.AppendLine();
	}
}
