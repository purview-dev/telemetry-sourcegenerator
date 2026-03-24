using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class ActivitySourceTargetClassEmitter
{
	static int EmitFields(
		ActivitySourceTarget target,
		StringBuilder builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		indent++;

		var activitySourceName = target.ActivitySourceName;
		if (string.IsNullOrWhiteSpace(activitySourceName))
		{
			logger?.Diagnostic("No activity source specified.");
			TelemetryDiagnostics.Report(context.ReportDiagnostic, TelemetryDiagnostics.Activities.NoActivitySourceSpecified);

			activitySourceName = Constants.Activities.DefaultActivitySourceName;
		}

		builder
			.Append(indent, "readonly static ", withNewLine: false)
			.Append(Constants.Activities.SystemDiagnostics.ActivitySource)
			.Append(' ')
			.Append(Constants.Activities.ActivitySourceFieldName)
			.Append(" = new(")
			.Append(activitySourceName!.Wrap())
			.AppendLine(");")
			.AppendLine();

		return --indent;
	}
}
