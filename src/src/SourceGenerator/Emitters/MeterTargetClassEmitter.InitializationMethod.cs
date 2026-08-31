using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class MeterTargetClassEmitter
{
	static void EmitInitializationMethod(
		MeterTarget target,
		CodeWriter writer,
		SourceProductionContext context,
		bool emitNullable,
		bool supportsIMeterFactory
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		writer
			.NewLine()
			.WriteLine(Constants.System.GeneratedCode.Value)
			.WriteLine(Constants.System.AggressiveInlining)
			.Write("void ")
			.Write(Constants.Metrics.MeterInitializationMethod)
			.Write('(');

		if (supportsIMeterFactory)
		{
			writer
				.Write(Constants.Metrics.SystemDiagnostics.IMeterFactory)
				.Write(' ')
				.Write(Constants.Metrics.MeterFactoryParameterName);
		}

		writer.Write(")");

		using (writer.OpenBlockScope())
		{
			// Double-init guard: prevents re-initialization when the method path is used
			// (occurs in Logging+Metrics multi-target where Logging owns the constructor).
			writer.Write("if (").Write(MeterFieldName).WriteLine(" != null)");

			using (writer.OpenBlockScope())
			{
				writer
					.Write("throw new ")
					.Write(Constants.System.Exception)
					.WriteLine("(\"The meters have already been initialized.\");");
			}

			writer.NewLine();

			EmitInitializationBodyContent(target, writer, emitNullable, supportsIMeterFactory);
		}
	}

	// Emits the inline constructor for the Metrics-only (and Activity+Metrics) case
	// where Metrics owns the constructor. Instrument fields are readonly, so we
	// inline the init body directly rather than delegating to InitializeMeters().
	static void EmitInlineConstructor(
		MeterTarget target,
		CodeWriter writer,
		SourceProductionContext context,
		bool emitNullable,
		bool supportsIMeterFactory
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		writer
			.NewLine()
			.WriteLine(Constants.System.GeneratedCode.Value)
			.Write("public ")
			.Write(target.ClassNameToGenerate)
			.Write('(');

		if (supportsIMeterFactory)
		{
			writer
				.Write(Constants.Metrics.SystemDiagnostics.IMeterFactory)
				.Write(' ')
				.Write(Constants.Metrics.MeterFactoryParameterName);
		}

		writer.Write(")");

		using (writer.OpenBlockScope())
		{
			EmitInitializationBodyContent(target, writer, emitNullable, supportsIMeterFactory);
		}
	}

	static void EmitInitializationBodyContent(
		MeterTarget target,
		CodeWriter writer,
		bool emitNullable,
		bool supportsIMeterFactory
	)
	{
		const string meterTagsVariableName = "meterTags";

		var dictType = GetDictionaryType(emitNullable);
		writer
			.Write((string)dictType)
			.Write(' ')
			.Write(meterTagsVariableName)
			.Write(" = new ")
			.Write((string)dictType)
			.WriteLine("();")
			.NewLine();

		writer.Write(PartialMeterTagsMethod).Write('(').Write(meterTagsVariableName).WriteLine(");").NewLine();

		if (supportsIMeterFactory)
		{
			writer
				.Write(MeterFieldName)
				.Write(" = ")
				.Write(Constants.Metrics.MeterFactoryParameterName)
				.Write(".Create(new ")
				.Write(Constants.Metrics.SystemDiagnostics.MeterOptions)
				.Write("(")
				.Write(target.MeterName!.Wrap())
				.Write(") {")
				.NewLine();
			writer.Indent();
			writer.WriteLine("Version = null,");
			writer.WriteLine($"Tags = {meterTagsVariableName}");
			writer.Unindent();
			writer.Write("});").NewLine();
		}
		else
		{
			writer
				.Write(MeterFieldName)
				.Write(" = new ")
				.Write(Constants.Metrics.SystemDiagnostics.Meter)
				.Write('(')
				.Write(target.MeterName!.Wrap())
				.WriteLine(");")
				.NewLine();
		}

		foreach (var method in target.InstrumentationMethods)
			EmitInitialiseInstrumentVariable(method, writer, emitNullable);
	}

	static void EmitInitialiseInstrumentVariable(InstrumentTarget method, CodeWriter writer, bool emitNullable)
	{
		if (!method.TargetGenerationState.IsValid)
			return;

		if (!method.IsObservable)
		{
			var unit = method.InstrumentAttribute?.Unit?.Value?.Wrap() ?? Constants.System.NullKeyword;
			var description = method.InstrumentAttribute?.Description?.Value?.Wrap() ?? Constants.System.NullKeyword;
			var tagVariableName = Utilities.LowercaseFirstChar(method.MethodName) + "Tags";

			var dictType = GetDictionaryType(emitNullable);
			writer
				.Write((string)dictType)
				.Write(' ')
				.Write(tagVariableName)
				.Write(" = new ")
				.Write((string)dictType)
				.WriteLine("();")
				.NewLine()
				.Write(method.TagPopulateMethodName)
				.Write('(')
				.Write(tagVariableName)
				.WriteLine(");")
				.NewLine();

			writer
				.Write(method.FieldName)
				.Write(" = ")
				.Write(MeterFieldName)
				.Write(".Create")
				.Write(method.InstrumentAttribute!.InstrumentType.ToString())
				.Write('<')
				.Write(method.InstrumentMeasurementType)
				.Write(">(name: ")
				.Write(method.MetricName.Wrap())
				.Write(", unit: ")
				.Write(unit)
				.Write(", description: ")
				.Write(description)
				.Write(", tags: ")
				.Write(tagVariableName)
				.WriteLine(");");
		}
	}
}
