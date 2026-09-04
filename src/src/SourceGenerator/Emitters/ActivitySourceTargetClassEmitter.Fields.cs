using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class ActivitySourceTargetClassEmitter
{
	static void EmitFields(ActivityOutputContext output, CodeWriter writer, SourceProductionContext context)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		var target = output.Target;
		var activitySourceName = target.ActivitySourceName;
		if (string.IsNullOrWhiteSpace(activitySourceName))
		{
			output.Context.Diagnostic("No activity source specified.");

			activitySourceName = PropertyLibrary.Activities.DefaultActivitySourceName;
		}

		writer
			.Field(
				new FieldDeclarationOptions(
					PropertyLibrary.Activities.ActivitySourceFieldName,
					TypeLibrary.Activities.SystemDiagnostics.ActivitySource.AsTypeReference()
				)
				{
					IsStatic = true,
					IsReadOnly = true,
					Initializer =
						$"new {(string)TypeLibrary.Activities.SystemDiagnostics.ActivitySource}({activitySourceName!.Wrap()})",
					IncludeGeneratedAttributes = false,
				}
			)
			.NewLine();
	}
}
