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
		ISourceGenLogger? logger
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		var activitySourceName = target.ActivitySourceName;
		if (string.IsNullOrWhiteSpace(activitySourceName))
		{
			logger?.Diagnostic("No activity source specified.");

			activitySourceName = PropertyLibrary.Activities.DefaultActivitySourceName;
		}

		writer
			.Write("readonly static ")
			.Write(TypeLibrary.Activities.SystemDiagnostics.ActivitySource)
			.Write(' ')
			.Write(PropertyLibrary.Activities.ActivitySourceFieldName)
			.Write(" = new ")
			.Write(TypeLibrary.Activities.SystemDiagnostics.ActivitySource)
			.Write('(')
			.Write(activitySourceName!.Wrap())
			.WriteLine(");")
			.NewLine();
	}
}
