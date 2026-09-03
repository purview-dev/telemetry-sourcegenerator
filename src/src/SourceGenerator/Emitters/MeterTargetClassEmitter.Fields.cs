using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class MeterTargetClassEmitter
{
	static void EmitFields(
		MeterOutputContext output,
		CodeWriter writer,
		SourceProductionContext context,
		bool readonlyFields = false
	)
	{
		var target = output.Target;
		context.CancellationToken.ThrowIfCancellationRequested();

		output.Context.Debug($"Emitting fields for {target.ClassNameToGenerate}");

		// When metrics owns the constructor, emit readonly fields so the JIT can treat
		// them as immutable after construction and eliminate null checks in hot paths.
		if (readonlyFields)
		{
			writer
				.Write("readonly ")
				.Write((string)TypeLibrary.Metrics.SystemDiagnostics.Meter)
				.Write(' ')
				.Write(MeterFieldName)
				.WriteLine(";")
				.NewLine();
		}
		else
		{
			writer
				.Write(TypeLibrary.Metrics.SystemDiagnostics.Meter)
				.Write(' ')
				.Write(MeterFieldName)
				.WriteLine(writer.IsNullableContextEnabled is null or true ? " = default!;" : " = default;")
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

			var type = TypeLibrary
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
					.WriteLine(writer.IsNullableContextEnabled is null or true ? " = default!;" : " = default;");
			}
		}
	}
}
