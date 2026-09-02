using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class ActivitySourceTargetClassEmitter
{
	static void EmitContextMethodBody(
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
				out var _,
				out var tagsParam,
				out var linksParam,
				out var _,
				out var _,
				out var _,
				out var _
			)
		)
		{
			return;
		}

		var activityVariableName =
			activityParam?.ParameterName ?? TypeLibrary.Activities.SystemDiagnostics.Activity.StaticMember("Current");

		if (tagsParam != null)
		{
			output.Context.Diagnostic("Tags parameter not allowed on context method, only activities or events.");

			return;
		}

		if (linksParam != null)
		{
			output.Context.Diagnostic("Links parameter not allowed on context method, only activities.");

			return;
		}

		EmitHasListenersTest(writer, methodTarget);

		writer.Write("if (").Write(activityVariableName).WriteLine(" != null)");

		using (writer.OpenBlockScope())
		{
			EmitTagsOrBaggageParameters(writer, activityVariableName, true, methodTarget, false, output);
			EmitTagsOrBaggageParameters(writer, activityVariableName, false, methodTarget, false, output);
		}

		context.CancellationToken.ThrowIfCancellationRequested();

		if (methodTarget.ReturnType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.Activity))
		{
			writer.NewLine().Write("return ").Write(activityVariableName).Write(";").NewLine();
		}
	}
}
