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
				.Field(
					new FieldDeclarationOptions(
						MeterFieldName,
						TypeLibrary.Metrics.SystemDiagnostics.Meter.AsTypeReference()
					)
					{
						IsReadOnly = true,
						IncludeGeneratedAttributes = false,
					}
				)
				.NewLine();
		}
		else
		{
			writer
				.Field(
					new FieldDeclarationOptions(
						MeterFieldName,
						TypeLibrary.Metrics.SystemDiagnostics.Meter.AsTypeReference()
					)
					{
						Initializer = writer.IsNullableContextEnabled is null or true ? "default!" : "default",
						IncludeGeneratedAttributes = false,
					}
				)
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
				writer.Field(
					new FieldDeclarationOptions(method.FieldName, type)
					{
						IsReadOnly = true,
						IncludeGeneratedAttributes = false,
					}
				);
			}
			else
			{
				writer.Field(
					new FieldDeclarationOptions(method.FieldName, type)
					{
						Initializer = writer.IsNullableContextEnabled is null or true ? "default!" : "default",
						IncludeGeneratedAttributes = false,
					}
				);
			}
		}
	}
}
