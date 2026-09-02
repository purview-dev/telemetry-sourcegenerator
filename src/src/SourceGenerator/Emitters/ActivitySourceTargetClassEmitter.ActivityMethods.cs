using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class ActivitySourceTargetClassEmitter
{
	static void EmitActivityMethodBody(
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
				out var _,
				out var _
			)
		)
		{
			return;
		}

		if (activityParam != null)
		{
			output.Context.Diagnostic("Activity parameter not allowed on Activity start/ create method, only event.");
			return;
		}

		if (timestampParam != null)
		{
			output.Context.Diagnostic("Timestamp parameter not allowed on Activity start/ create method, only events.");
			return;
		}

		EmitHasListenersTest(writer, methodTarget);

		var activityVariableName = "activity" + methodTarget.MethodName;

		writer.WriteAssignment(
			TypeLibrary.Activities.SystemDiagnostics.Activity.MakeNullable(writer),
			activityVariableName,
			writeValue: assignmentWriter =>
			{
				assignmentWriter
					//.Write(" = ")
					.Write(PropertyLibrary.Activities.ActivitySourceFieldName)
					.Write('.');

				var createOnly = methodTarget.ActivityAttribute?.CreateOnly == true;
				var useParentContext =
					parentContextOrId != null
					&& parentContextOrId.ParameterType.Identity.Equals(
						TypeLibrary.Activities.SystemDiagnostics.ActivityContext
					);
				var parentContextParameterName = useParentContext ? "parentContext" : "parentId";

				if (createOnly && startTimeParam != null)
				{
					output.Context.Diagnostic("StartTime parameter not allowed on Activity create method.");

					return;
				}

				var kind = methodTarget.ActivityAttribute?.Kind ?? PropertyLibrary.Activities.DefaultActivityKind;

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

				EmitActivityCall(
					assignmentWriter,
					methodTarget,
					createOnly,
					useParentContext,
					parentContextParameterName,
					parentContextOrIdParameterValue,
					tagsParam,
					linksParam,
					startTimeParam,
					kind
				);
			}
		);

		context.CancellationToken.ThrowIfCancellationRequested();

		if (methodTarget.Tags.Count > 0 || methodTarget.Baggage.Count > 0)
		{
			writer.NewLine().Write("if (").Write(activityVariableName).WriteLine(" != null)");

			using (writer.OpenBlockScope())
			{
				EmitTagsOrBaggageParameters(writer, activityVariableName, true, methodTarget, false, output);
				EmitTagsOrBaggageParameters(writer, activityVariableName, false, methodTarget, false, output);
			}
		}

		context.CancellationToken.ThrowIfCancellationRequested();

		if (methodTarget.ReturnType.Similar(TypeLibrary.Activities.SystemDiagnostics.Activity))
		{
			writer.WriteReturn(returnWriter =>
				returnWriter.Write(activityVariableName).Write(methodTarget.ReturnType.IsNullable ? null : "!")
			);
		}
	}

	static void AddActivityNameParameter(CodeWriter writer, ActivityBasedGenerationTarget methodTarget, bool useName)
	{
		if (useName)
			writer.Write("name: ");

		writer.Write(methodTarget.ActivityOrEventName.Wrap());
	}

	static void EmitActivityCall(
		CodeWriter writer,
		ActivityBasedGenerationTarget methodTarget,
		bool createOnly,
		bool useParentContext,
		string parentContextParameterName,
		string parentContextOrIdParameterValue,
		ActivityBasedParameterTarget? tagsParam,
		ActivityBasedParameterTarget? linksParam,
		ActivityBasedParameterTarget? startTimeParam,
		int kind
	)
	{
		writer.Write(createOnly ? "CreateActivity(" : "StartActivity(");

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
			.Write(PropertyLibrary.Activities.ActivityKindTypeMap[kind])
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

		writer.WriteLine(")");
	}

	static void EmitHasListenersTest(CodeWriter writer, ActivityBasedGenerationTarget methodTarget)
	{
		var returnsVoid = methodTarget.ReturnType.Identity.SpecialType == SpecialType.System_Void;
		writer.Write("if (!").Write(PropertyLibrary.Activities.ActivitySourceFieldName).WriteLine(".HasListeners())");

		using (writer.OpenBlockScope())
		{
			writer.WriteLine(
				"return" + (returnsVoid ? null : " null" + (methodTarget.ReturnType.IsNullable ? null : "!")) + ";"
			);
		}

		writer.NewLine();
	}
}
