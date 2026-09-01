using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class ActivitySourceTargetClassEmitter
{
	static void EmitContextMethodBody(
		CodeWriter writer,
		ActivityBasedGenerationTarget methodTarget,
		SourceProductionContext context,
		ISourceGenLogger? logger,
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
			activityParam?.ParameterName ?? (TypeLibrary.Activities.SystemDiagnostics.Activity + ".Current");

		if (tagsParam != null)
		{
			logger?.Diagnostic("Tags parameter not allowed on context method, only activities or events.");

			return;
		}

		if (linksParam != null)
		{
			logger?.Diagnostic("Links parameter not allowed on context method, only activities.");

			return;
		}

		EmitHasListenersTest(writer, methodTarget, emitNullable);

		writer.Write("if (").Write(activityVariableName).WriteLine(" != null)");

		using (writer.OpenBlockScope())
		{
			EmitTagsOrBaggageParameters(writer, activityVariableName, true, methodTarget, false, logger);
			EmitTagsOrBaggageParameters(writer, activityVariableName, false, methodTarget, false, logger);
		}

		context.CancellationToken.ThrowIfCancellationRequested();

		if (methodTarget.ReturnType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.Activity))
		{
			writer.NewLine().Write("return ").Write(activityVariableName).Write(";").NewLine();
		}
	}
}
