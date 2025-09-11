using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class MeterTargetClassEmitter
{
	static int EmitFields(
		MeterTarget target,
		CodeWriter builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Emitting fields for {target.ClassNameToGenerate}");

		indent++;

		builder
			.Append(indent, Constants.Metrics.SystemDiagnostics.Meter, withNewLine: false)
			.Append(' ')
			.Append(Constants.VariableNames.MeterFieldName)
			.AppendLine(" = default!;")
			.AppendLine();

		foreach (var method in target.InstrumentationMethods)
		{
			if (method.InstrumentAttribute == null)
			{
				// We've already 'reported' this error, so we can skip it.
				continue;
			}

			var type = Constants
				.Metrics.InstrumentTypeMap.Value[method.InstrumentAttribute.InstrumentType]
				.MakeGeneric(method.InstrumentMeasurementType);

			builder
				.Append(indent, type, withNewLine: false)
				.Append("? ")
				.Append(method.FieldName)
				.AppendLine(" = null;");
		}

		return --indent;
	}
}
