using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class MeterTargetClassEmitter
{
	static void EmitInitializationMethod(MeterOutputContext output, CodeWriter writer, SourceProductionContext context)
	{
		var supportsIMeterFactory = output.Context.Capabilities.SupportsIMeterFactory;

		context.CancellationToken.ThrowIfCancellationRequested();

		writer.NewLine().Write("void ").Write(PropertyLibrary.Metrics.MeterInitializationMethod).Write('(');

		if (supportsIMeterFactory)
		{
			writer
				.Write(TypeLibrary.Metrics.SystemDiagnostics.IMeterFactory)
				.Write(' ')
				.Write(PropertyLibrary.Metrics.MeterFactoryParameterName);
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
					.Write(TypeLibrary.System.Exception)
					.WriteLine("(\"The meters have already been initialized.\");");
			}

			writer.NewLine();

			EmitInitializationBodyContent(output, writer);
		}
	}

	// Emits the inline constructor for the Metrics-only (and Activity+Metrics) case
	// where Metrics owns the constructor. Instrument fields are readonly, so we
	// inline the init body directly rather than delegating to InitializeMeters().
	static void EmitInlineConstructor(MeterOutputContext output, CodeWriter writer, SourceProductionContext context)
	{
		var target = output.Target;
		var supportsIMeterFactory = output.Context.Capabilities.SupportsIMeterFactory;

		context.CancellationToken.ThrowIfCancellationRequested();

		writer.NewLine().Write("public ").Write(target.ClassNameToGenerate).Write('(');

		if (supportsIMeterFactory)
		{
			writer
				.Write(TypeLibrary.Metrics.SystemDiagnostics.IMeterFactory)
				.Write(' ')
				.Write(PropertyLibrary.Metrics.MeterFactoryParameterName);
		}

		writer.Write(")");

		using (writer.OpenBlockScope())
		{
			EmitInitializationBodyContent(output, writer);
		}
	}

	static void EmitInitializationBodyContent(MeterOutputContext output, CodeWriter writer)
	{
		var target = output.Target;
		var supportsIMeterFactory = output.Context.Capabilities.SupportsIMeterFactory;
		const string meterTagsVariableName = "meterTags";

		var dictType = GetDictionaryType(writer);
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
				.Write(PropertyLibrary.Metrics.MeterFactoryParameterName)
				.Write(".Create(new ")
				.Write(TypeLibrary.Metrics.SystemDiagnostics.MeterOptions)
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
				.Write(TypeLibrary.Metrics.SystemDiagnostics.Meter)
				.Write('(')
				.Write(target.MeterName!.Wrap())
				.WriteLine(");")
				.NewLine();
		}

		foreach (var method in target.InstrumentationMethods)
			EmitInitialiseInstrumentVariable(method, writer);
	}

	static void EmitInitialiseInstrumentVariable(InstrumentTarget method, CodeWriter writer)
	{
		if (!method.TargetGenerationState.IsValid)
			return;

		if (!method.IsObservable)
		{
			var unit = method.InstrumentAttribute?.Unit?.Wrap() ?? PropertyLibrary.System.NullKeyword;
			var description = method.InstrumentAttribute?.Description?.Wrap() ?? PropertyLibrary.System.NullKeyword;
			var tagVariableName = Utilities.LowercaseFirstChar(method.MethodName) + "Tags";

			var dictType = GetDictionaryType(writer);
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
