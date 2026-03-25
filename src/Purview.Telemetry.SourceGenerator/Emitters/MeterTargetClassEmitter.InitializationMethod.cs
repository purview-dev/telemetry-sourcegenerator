using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class MeterTargetClassEmitter
{
	static int EmitInitializationMethod(
	MeterTarget target,
	StringBuilder builder,
	int indent,
	SourceProductionContext context,
	bool emitNullable,
	bool supportsIMeterFactory
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		indent++;

		builder
		.AppendLine()
		.CodeGen(indent)
		.AggressiveInlining(indent)
		.Append(indent, "void ", withNewLine: false)
		.Append(Constants.Metrics.MeterInitializationMethod)
		.Append('(');

		if (supportsIMeterFactory)
		{
			builder
			.Append(Constants.Metrics.SystemDiagnostics.IMeterFactory)
			.Append(' ')
			.Append(Constants.Metrics.MeterFactoryParameterName);
		}

		builder
		.AppendLine(')')
		.Append(indent, '{');

		indent++;

		// Double-init guard: prevents re-initialization when the method path is used
		// (occurs in Logging+Metrics multi-target where Logging owns the constructor).
		builder
		.Append(indent, "if (", withNewLine: false)
		.Append(MeterFieldName)
		.AppendLine(" != null)")
		.Append(indent, '{')
		.Append(indent + 1, "throw new ", withNewLine: false)
		.Append(Constants.System.Exception)
		.AppendLine("(\"The meters have already been initialized.\");")
		.Append(indent, '}')
		.AppendLine();

		EmitInitializationBodyContent(target, builder, indent, emitNullable, supportsIMeterFactory);

		indent--;

		builder.Append(indent, '}');

		return --indent;
	}

	// Emits the inline constructor for the Metrics-only (and Activity+Metrics) case
	// where Metrics owns the constructor. Instrument fields are readonly, so we
	// inline the init body directly rather than delegating to InitializeMeters().
	static int EmitInlineConstructor(
	MeterTarget target,
	StringBuilder builder,
	int indent,
	SourceProductionContext context,
	bool emitNullable,
	bool supportsIMeterFactory
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		indent++;

		builder
		.AppendLine()
		.CodeGen(indent)
		.Append(indent, "public ", withNewLine: false)
		.Append(target.ClassNameToGenerate)
		.Append('(');

		if (supportsIMeterFactory)
		{
			builder
			.Append(Constants.Metrics.SystemDiagnostics.IMeterFactory)
			.Append(' ')
			.Append(Constants.Metrics.MeterFactoryParameterName);
		}

		builder
		.AppendLine(')')
		.Append(indent, '{');

		indent++;

		EmitInitializationBodyContent(target, builder, indent, emitNullable, supportsIMeterFactory);

		indent--;

		builder.Append(indent, '}');

		return --indent;
	}

	static void EmitInitializationBodyContent(
	MeterTarget target,
	StringBuilder builder,
	int indent,
	bool emitNullable,
	bool supportsIMeterFactory
	)
	{
		const string meterTagsVariableName = "meterTags";

		var dictType = GetDictionaryType(emitNullable);
		builder
		.Append(indent, (string)dictType, withNewLine: false)
		.Append(' ')
		.Append(meterTagsVariableName)
		.Append(" = new ")
		.Append((string)dictType)
		.AppendLine("();")
		.AppendLine();

		builder
		.Append(indent, PartialMeterTagsMethod, withNewLine: false)
		.Append('(')
		.Append(meterTagsVariableName)
		.AppendLine(");")
		.AppendLine();

		if (supportsIMeterFactory)
		{
			builder
			.Append(indent, MeterFieldName, withNewLine: false)
			.Append(" = ")
			.Append(Constants.Metrics.MeterFactoryParameterName)
			.Append(".Create(new ")
			.Append(Constants.Metrics.SystemDiagnostics.MeterOptions)
			.Append('(')
			.Append(target.MeterName!.Wrap())
			.AppendLine(')')
			.Append(indent, '{')
			.Append(indent + 1, "Version = ", withNewLine: false)
			.AppendLine("null,")
			.Append(indent + 1, "Tags = ", withNewLine: false)
			.AppendLine(meterTagsVariableName)
			.Append(indent, "});")
			.AppendLine();
		}
		else
		{
			builder
			.Append(indent, MeterFieldName, withNewLine: false)
			.Append(" = new ")
			.Append(Constants.Metrics.SystemDiagnostics.Meter)
			.Append('(')
			.Append(target.MeterName!.Wrap())
			.AppendLine(");")
			.AppendLine();
		}

		foreach (var method in target.InstrumentationMethods)
			EmitInitialiseInstrumentVariable(method, builder, indent, emitNullable);
	}

	static void EmitInitialiseInstrumentVariable(
	InstrumentTarget method,
	StringBuilder builder,
	int indent,
	bool emitNullable
	)
	{
		if (!method.TargetGenerationState.IsValid)
			return;

		if (!method.IsObservable)
		{
			var unit =
			method.InstrumentAttribute?.Unit?.Value?.Wrap() ?? Constants.System.NullKeyword;
			var description =
			method.InstrumentAttribute?.Description?.Value?.Wrap()
			?? Constants.System.NullKeyword;
			var tagVariableName = Utilities.LowercaseFirstChar(method.MethodName) + "Tags";

			var dictType = GetDictionaryType(emitNullable);
			builder
			.Append(indent, (string)dictType, withNewLine: false)
			.Append(' ')
			.Append(tagVariableName)
			.Append(" = new ")
			.Append((string)dictType)
			.AppendLine("();")
			.AppendLine()
			.Append(indent, method.TagPopulateMethodName, withNewLine: false)
			.Append('(')
			.Append(tagVariableName)
			.AppendLine(");")
			.AppendLine();

			builder
			.Append(indent, method.FieldName, withNewLine: false)
			.Append(" = ")
			.Append(MeterFieldName)
			.Append(".Create")
			.Append(method.InstrumentAttribute!.InstrumentType)
			.Append('<')
			.Append(method.InstrumentMeasurementType)
			.Append(">(name: ")
			.Append(method.MetricName.Wrap())
			.Append(", unit: ")
			.Append(unit)
			.Append(", description: ")
			.Append(description)
			.Append(", tags: ")
			.Append(tagVariableName)
			.AppendLine(");");
		}
	}
}
