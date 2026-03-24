using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class MeterTargetClassEmitter
{
	static int EmitFields(
		MeterTarget target,
		StringBuilder builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool readonlyFields = false
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Emitting fields for {target.ClassNameToGenerate}");

		indent++;

		// When metrics owns the constructor, emit readonly fields so the JIT can treat
		// them as immutable after construction and eliminate null checks in hot paths.
		if (readonlyFields)
		{
			builder
				.Append(indent, "readonly ", withNewLine: false)
				.Append((string)Constants.Metrics.SystemDiagnostics.Meter)
				.Append(' ')
				.Append(MeterFieldName)
				.AppendLine(";")
				.AppendLine();
		}
		else
		{
			builder
				.Append(indent, Constants.Metrics.SystemDiagnostics.Meter, withNewLine: false)
				.Append(' ')
				.Append(MeterFieldName)
				.AppendLine(" = default!;")
				.AppendLine();
		}

		foreach (var method in target.InstrumentationMethods)
		{
			if (!method.TargetGenerationState.IsValid)
			{
				// Skip invalid methods (e.g. post-pass duplicates); no field needed.
				continue;
			}

			if (method.InstrumentAttribute == null)
			{
				// We've already 'reported' this error, so we can skip it.
				continue;
			}

			var type = Constants
				.Metrics.InstrumentTypeMap[method.InstrumentAttribute.InstrumentType]
				.MakeGeneric(method.InstrumentMeasurementType);

			// Observable instruments are registered via callback, not assigned in the constructor,
			// so they cannot be readonly.
			var emitReadonly = readonlyFields && !method.IsObservable;

			if (emitReadonly)
			{
				builder
					.Append(indent, "readonly ", withNewLine: false)
					.Append((string)type)
					.Append(' ')
					.Append(method.FieldName)
					.AppendLine(";");
			}
			else
			{
				builder
					.Append(indent, type, withNewLine: false)
					.Append(' ')
					.Append(method.FieldName)
					.AppendLine(" = default!;");
			}
		}

		return --indent;
	}
}
