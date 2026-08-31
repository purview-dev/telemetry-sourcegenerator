using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class MeterTargetClassEmitter
{
	static void EmitFields(
		MeterTarget target,
		CodeWriter writer,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool readonlyFields = false,
		bool emitNullable = true
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Emitting fields for {target.ClassNameToGenerate}");

		// When metrics owns the constructor, emit readonly fields so the JIT can treat
		// them as immutable after construction and eliminate null checks in hot paths.
		if (readonlyFields)
		{
			writer
				.Write("readonly ")
				.Write((string)Constants.Metrics.SystemDiagnostics.Meter)
				.Write(' ')
				.Write(MeterFieldName)
				.WriteLine(";")
				.NewLine();
		}
		else
		{
			writer
				.Write(Constants.Metrics.SystemDiagnostics.Meter)
				.Write(' ')
				.Write(MeterFieldName)
				.WriteLine(emitNullable ? " = default!;" : " = default;")
				.NewLine();
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
				writer.Write("readonly ").Write((string)type).Write(' ').Write(method.FieldName).WriteLine(";");
			}
			else
			{
				writer
					.Write(type)
					.Write(' ')
					.Write(method.FieldName)
					.WriteLine(emitNullable ? " = default!;" : " = default;");
			}
		}
	}
}
