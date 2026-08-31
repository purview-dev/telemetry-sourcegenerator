using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class ActivitySourceTargetClassEmitter
{
	static void EmitFields(
		ActivitySourceTarget target,
		CodeWriter writer,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		var activitySourceName = target.ActivitySourceName;
		if (string.IsNullOrWhiteSpace(activitySourceName))
		{
			logger?.Diagnostic("No activity source specified.");
			TelemetryDiagnostics.Report(
				context.ReportDiagnostic,
				TelemetryDiagnostics.Activities.NoActivitySourceSpecified
			);

			activitySourceName = Constants.Activities.DefaultActivitySourceName;
		}

		writer
			.Write("readonly static ")
			.Write(Constants.Activities.SystemDiagnostics.ActivitySource)
			.Write(' ')
			.Write(Constants.Activities.ActivitySourceFieldName)
			.Write(" = new ")
			.Write(Constants.Activities.SystemDiagnostics.ActivitySource)
			.Write('(')
			.Write(activitySourceName!.Wrap())
			.WriteLine(");")
			.NewLine();
	}
}
