using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class ActivitySourceTargetClassEmitter
{
	static void EmitActivityMethodBody(
		CodeWriter writer,
		ActivityBasedGenerationTarget methodTarget,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable
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
			logger?.Diagnostic("Activity parameter not allowed on Activity start/ create method, only event.");

			return;
		}

		if (timestampParam != null)
		{
			logger?.Diagnostic("Timestamp parameter not allowed on Activity start/ create method, only events.");

			return;
		}

		EmitHasListenersTest(writer, methodTarget, emitNullable);

		var activityVariableName = "activity" + methodTarget.MethodName;

		writer
			.Write(Constants.Activities.SystemDiagnostics.Activity)
			.Write(emitNullable ? "? " : " ")
			.Write(activityVariableName)
			.Write(" = ")
			.Write(Constants.Activities.ActivitySourceFieldName)
			.Write('.');

		var createOnly = methodTarget.ActivityAttribute?.CreateOnly.Value == true;
		var createActivityMethod = createOnly ? "Create" : "Start";
		var useParentContext = Constants.Activities.SystemDiagnostics.ActivityContext.Equals(
			parentContextOrId?.ParameterType
		);
		var parentContextParameterName = useParentContext ? "parentContext" : "parentId";

		if (createOnly && startTimeParam != null)
		{
			logger?.Diagnostic("StartTime parameter not allowed on Activity create method.");

			return;
		}

		var kind =
			methodTarget.ActivityAttribute?.Kind.IsSet == true
				? methodTarget.ActivityAttribute.Value.Kind.Value.GetValueOrDefault()
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

		writer.Write(createActivityMethod).Write("Activity(");

		if (createOnly || !useParentContext)
		{
			// Only create the name always comes first.
			// If it's start, and we're using an ActivityContext then the
			// name comes last.
			AddActivityNameParameter(writer, methodTarget, false);
			writer.Write(", ");
		}

		writer
			// kind: (un-named)
			.Write(Constants.Activities.ActivityKindTypeMap[kind])
			// parentContext/ parentId:
			.Write(", ")
			.Write(parentContextParameterName)
			.Write(": ")
			.Write(parentContextOrIdParameterValue)
			// tags:
			.Write(", tags: ")
			.Write(tagsParam?.ParameterName ?? "default")
			// links:
			.Write(", links: ")
			.Write(linksParam?.ParameterName ?? "default");

		if (!createOnly)
		{
			writer
				// startTime:
				.Write(", startTime: ")
				.Write(startTimeParam?.ParameterName ?? "default");

			if (useParentContext)
			{
				// If it's a Start and we're using an ActivityContext,
				// the name comes last.
				writer.Write(", ");
				AddActivityNameParameter(writer, methodTarget, true);
			}
		}

		writer.WriteLine(");");

		context.CancellationToken.ThrowIfCancellationRequested();

		if (methodTarget.Tags.Count > 0 || methodTarget.Baggage.Count > 0)
		{
			writer.NewLine().Write("if (").Write(activityVariableName).WriteLine(" != null)");

			using (writer.OpenBlockScope())
			{
				EmitTagsOrBaggageParameters(writer, activityVariableName, true, methodTarget, false, context, logger);
				EmitTagsOrBaggageParameters(writer, activityVariableName, false, methodTarget, false, context, logger);
			}
		}

		context.CancellationToken.ThrowIfCancellationRequested();

		if (Constants.Activities.SystemDiagnostics.Activity.Equals(methodTarget.ReturnType))
		{
			writer
				.NewLine()
				.Write("return ")
				.Write(activityVariableName)
				.Write(!emitNullable || methodTarget.ReturnType.IsNullable ? null : "!")
				.Write(";")
				.NewLine();
		}

		static void AddActivityNameParameter(
			CodeWriter writer,
			ActivityBasedGenerationTarget methodTarget,
			bool useName
		)
		{
			if (useName)
				writer.Write("name: ");

			writer.Write(methodTarget.ActivityOrEventName.Wrap());
		}
	}

	static void EmitHasListenersTest(CodeWriter writer, ActivityBasedGenerationTarget methodTarget, bool emitNullable)
	{
		var returnsVoid = methodTarget.ReturnType.SpecialType == SpecialType.System_Void;
		writer.Write("if (!").Write(Constants.Activities.ActivitySourceFieldName).WriteLine(".HasListeners())");

		using (writer.OpenBlockScope())
		{
			writer.WriteLine(
				"return"
					+ (
						returnsVoid
							? null
							: " null" + (!emitNullable || methodTarget.ReturnType.IsNullable ? null : "!")
					)
					+ ";"
			);
		}

		writer.NewLine();
	}
}
