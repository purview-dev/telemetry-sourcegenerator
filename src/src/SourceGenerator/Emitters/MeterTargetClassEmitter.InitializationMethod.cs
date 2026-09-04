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

		writer.NewLine();

		using (
			writer.MethodScope(
				new MethodDeclarationOptions(
					PropertyLibrary.Metrics.MeterInitializationMethod,
					PurviewTypeLibrary.System.Void.AsTypeReference()
				)
				{
					Parameters = supportsIMeterFactory
						?
						[
							new ParameterDeclarationOptions(
								PropertyLibrary.Metrics.MeterFactoryParameterName,
								TypeLibrary.Metrics.SystemDiagnostics.IMeterFactory.AsTypeReference()
							),
						]
						: [],
					IncludeGeneratedAttributes = false,
				}
			)
		)
		{
			// Double-init guard: prevents re-initialization when the method path is used
			// (occurs in Logging+Metrics multi-target where Logging owns the constructor).
			writer.IfBlock(
				MeterFieldName + " != null",
				body =>
					body.Throw(
						"new " + TypeLibrary.System.Exception + "(\"The meters have already been initialized.\")"
					)
			);

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

		writer.NewLine();

		using (
			writer.ConstructorScope(
				new ConstructorDeclarationOptions(target.ClassNameToGenerate, TypeDeclarationAccessibility.Public)
				{
					Parameters = supportsIMeterFactory
						?
						[
							new ParameterDeclarationOptions(
								PropertyLibrary.Metrics.MeterFactoryParameterName,
								TypeLibrary.Metrics.SystemDiagnostics.IMeterFactory.AsTypeReference()
							),
						]
						: [],
					IncludeGeneratedAttributes = false,
				}
			)
		)
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
		writer.Assignment((string)dictType, meterTagsVariableName, "new " + (string)dictType + "()").NewLine();

		writer.Write(PartialMeterTagsMethod).Write('(').Write(meterTagsVariableName).Line(");").NewLine();

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
			writer.Line("Version = null,");
			writer.Line($"Tags = {meterTagsVariableName}");
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
				.Line(");")
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
				.Assignment((string)dictType, tagVariableName, "new " + (string)dictType + "()")
				.NewLine()
				.Write(method.TagPopulateMethodName)
				.Write('(')
				.Write(tagVariableName)
				.Line(");")
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
				.Line(");");
		}
	}
}
