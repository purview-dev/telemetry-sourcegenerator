using System.Collections.Immutable;
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
					TypeLibrary.System.Void.AsTypeReference()
				)
				{
					Parameters = supportsIMeterFactory
						?
						[
							new ParameterDeclarationOptions(
								PropertyLibrary.Metrics.MeterFactoryParameterName,
								TypeLibrary.System.Diagnostics.Metrics.IMeterFactory.AsTypeReference()
							),
						]
						: [],
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
		var needsActivitySourceFactory = ConstructorEmitter.NeedsActivitySourceFactory(
			target.GenerationType,
			output.Context
		);

		context.CancellationToken.ThrowIfCancellationRequested();

		writer.NewLine();

		var parameters = ImmutableArray.CreateBuilder<ParameterDeclarationOptions>();
		if (supportsIMeterFactory)
			parameters.Add(
				new ParameterDeclarationOptions(
					PropertyLibrary.Metrics.MeterFactoryParameterName,
					TypeLibrary.System.Diagnostics.Metrics.IMeterFactory.AsTypeReference()
				)
			);

		// Activities + Metrics: Metrics owns the constructor, so it also receives the .NET 11 ActivitySourceFactory.
		if (needsActivitySourceFactory)
			parameters.Add(ConstructorEmitter.ActivitySourceFactoryParameter());

		using (
			writer.ConstructorScope(
				new ConstructorDeclarationOptions(target.ClassNameToGenerate, TypeDeclarationAccessibility.Public)
				{
					Parameters = parameters.ToImmutable(),
				}
			)
		)
		{
			EmitInitializationBodyContent(output, writer);

			if (needsActivitySourceFactory)
				ConstructorEmitter.EmitActivitySourceInitializationCall(writer);
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
				.Write(TypeLibrary.System.Diagnostics.Metrics.MeterOptions)
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
				.Write(TypeLibrary.System.Diagnostics.Metrics.Meter)
				.Write('(')
				.Write(target.MeterName!.Wrap())
				.Line(");")
				.NewLine();
		}

		foreach (var method in target.InstrumentationMethods)
			EmitInitialiseInstrumentVariable(method, writer, output.Context);
	}

	static void EmitInitialiseInstrumentVariable(
		InstrumentTarget method,
		CodeWriter writer,
		GenerationContext<TelemetryCapabilities> context
	)
	{
		if (!method.TargetGenerationState.IsValid || !IsInstrumentAvailable(method, context))
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
				.Write("." + method.InstrumentAttribute!.CreationMethodName)
				.Write('<')
				.Write(method.InstrumentMeasurementType)
				.Write(">(name: ")
				.Write(method.MetricName.Wrap())
				.Write(", unit: ")
				.Write(unit)
				.Write(", description: ")
				.Write(description)
				.Write(", tags: ")
				.Write(tagVariableName);

			EmitInstrumentAdvice(method, writer, context);

			writer.Line(");");
		}
	}

	/// <summary>
	/// Appends <c>advice:</c> for a histogram whose <c>HistogramBucketBoundaries</c> parsed. Only emitted when the
	/// compilation resolves <c>InstrumentAdvice&lt;T&gt;</c>, so an older analyzer-only
	/// System.Diagnostics.DiagnosticSource still compiles (and <c>TSG4011</c> reports the omission).
	/// </summary>
	static void EmitInstrumentAdvice(
		InstrumentTarget method,
		CodeWriter writer,
		GenerationContext<TelemetryCapabilities> context
	)
	{
		if (!context.Capabilities.SupportsInstrumentAdvice)
			return;

		if (
			method.InstrumentAttribute
			is not HistogramInstrumentAttributeRecord { HistogramBucketBoundaries: { Count: > 0 } boundaries }
		)
			return;

		writer
			.NewLine()
			.Write(", advice: new ")
			.Write(
				TypeLibrary.System.Diagnostics.Metrics.InstrumentAdvice.MakeGeneric(method.InstrumentMeasurementType)
			)
			.Write(" { HistogramBucketBoundaries = new double[] { ");

		for (var i = 0; i < boundaries.Count; i++)
		{
			if (i > 0)
				writer.Write(", ");

			writer.Write(boundaries[i].ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "d");
		}

		writer.Write(" } }");
	}

	/// <summary>
	/// Whether the compilation can carry this instrument: a gauge needs <c>Gauge&lt;T&gt;</c>. A method whose
	/// instrument cannot be generated gets a throw stub instead, and <c>TSG4010</c> explains why.
	/// </summary>
	internal static bool IsInstrumentAvailable(
		InstrumentTarget method,
		GenerationContext<TelemetryCapabilities> context
	) => method.InstrumentAttribute is not GaugeInstrumentAttributeRecord || context.Capabilities.SupportsGauge;
}
