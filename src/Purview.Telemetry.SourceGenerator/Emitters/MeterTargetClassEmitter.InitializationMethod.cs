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
		SourceProductionContext context
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		indent++;

		const string meterTagsVariableName = "meterTags";

		builder
			.AppendLine()
			.CodeGen(indent)
			.AggressiveInlining(indent)
			.Append(indent, "void ", withNewLine: false)
			.Append(Constants.Metrics.MeterInitializationMethod)
			.Append('(')
			.Append(Constants.Metrics.SystemDiagnostics.IMeterFactory)
			.Append(' ')
			.Append(Constants.Metrics.MeterFactoryParameterName)
			.AppendLine(')')
			.Append(indent, '{');

		indent++;

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

		builder
			.Append(indent, DictionaryStringObjectType, withNewLine: false)
			.Append(' ')
			.Append(meterTagsVariableName)
			.AppendLine(" = new();")
			.AppendLine();

		builder
			.Append(indent, PartialMeterTagsMethod, withNewLine: false)
			.Append('(')
			.Append(meterTagsVariableName)
			.AppendLine(");")
			.AppendLine();

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
			.AppendLine("null,") // We'll support version later.
			.Append(indent + 1, "Tags = ", withNewLine: false)
			.AppendLine(meterTagsVariableName)
			.Append(indent, "});")
			.AppendLine();

		foreach (var method in target.InstrumentationMethods)
			EmitInitialiseInstrumentVariable(method, builder, indent);

		indent--;

		builder.Append(indent, '}');

		return --indent;
	}

	static void EmitInitialiseInstrumentVariable(
		InstrumentTarget method,
		StringBuilder builder,
		int indent
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
			var tagVariableName = Utilities.LowerCaseFirstChar(method.MethodName) + "Tags";

			builder
				.Append(indent, DictionaryStringObjectType, withNewLine: false)
				.Append(' ')
				.Append(tagVariableName)
				.Append(" = new")
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
