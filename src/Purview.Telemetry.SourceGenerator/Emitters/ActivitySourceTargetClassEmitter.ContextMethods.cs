using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class ActivitySourceTargetClassEmitter
{
	static void EmitContextMethodBody(
		StringBuilder builder,
		int indent,
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
			activityParam?.ParameterName
			?? (Constants.Activities.SystemDiagnostics.Activity + ".Current");

		if (tagsParam != null)
		{
			logger?.Diagnostic(
				"Tags parameter not allowed on context method, only activities or events."
			);

			return;
		}

		if (linksParam != null)
		{
			logger?.Diagnostic("Links parameter not allowed on context method, only activities.");

			return;
		}

		EmitHasListenersTest(builder, indent, methodTarget, emitNullable);

		builder
			.Append(indent, "if (", withNewLine: false)
			.Append(activityVariableName)
			.AppendLine(" != null)")
			.Append(indent, '{');

		indent++;

		EmitTagsOrBaggageParameters(
			builder,
			indent,
			activityVariableName,
			true,
			methodTarget,
			false,
			context,
			logger
		);
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
