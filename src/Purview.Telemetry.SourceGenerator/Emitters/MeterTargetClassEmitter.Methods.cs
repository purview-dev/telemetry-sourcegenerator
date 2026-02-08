using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class MeterTargetClassEmitter
{
	static int EmitMethods(
		MeterTarget target,
		StringBuilder builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		indent++;

		EmitPartialMethods(builder, indent, target, context, logger);

		foreach (var methodTarget in target.InstrumentationMethods)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			if (!methodTarget.TargetGenerationState.IsValid)
				continue;

			// Report warning for Activity parameter without Activity target
			if (methodTarget.TargetGenerationState.ActivityParameterWithoutTarget != null)
			{
				logger?.Debug(
					$"Activity parameter '{methodTarget.TargetGenerationState.ActivityParameterWithoutTarget}' on {methodTarget.MethodName} has no Activity target."
				);
				TelemetryDiagnostics.Report(
					context.ReportDiagnostic,
					TelemetryDiagnostics.General.ActivityParameterWithoutActivityTarget,
					methodTarget.Locations,
					methodTarget.TargetGenerationState.ActivityParameterWithoutTarget
				);
			}

			EmitMethod(builder, indent, methodTarget, context, logger);
		}

		return --indent;
	}

	static void EmitPartialMethods(
		StringBuilder builder,
		int indent,
		MeterTarget target,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Emitting partial method for populating tags: {PartialMeterTagsMethod}.");

		builder
			.AppendLine()
			.CodeGen(indent)
			.Append(indent, "partial void ", withNewLine: false)
			.Append(PartialMeterTagsMethod)
			.Append('(')
			.Append(DictionaryStringObjectType)
			.AppendLine(" meterTags);")
			.AppendLine();

		foreach (var instrument in target.InstrumentationMethods)
		{
			if (!instrument.TargetGenerationState.IsValid)
				continue;

			if (instrument.IsObservable)
				continue;

			builder
				.CodeGen(indent)
				.Append(indent, "partial void ", withNewLine: false)
				.Append(instrument.TagPopulateMethodName)
				.Append('(')
				.Append(DictionaryStringObjectType)
				.AppendLine(" instrumentTags);")
				.AppendLine();
		}
	}

	static void EmitMethod(
		StringBuilder builder,
		int indent,
		InstrumentTarget methodTarget,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		if (methodTarget.InstrumentAttribute == null)
			return;

		if (
			!methodTarget.InstrumentAttribute!.IsAutoIncrement
			&& methodTarget.MeasurementParameter == null
		)
		{
			return;
		}

		logger?.Debug($"Emitting instrument method: {methodTarget.MethodName}.");

		var isMultiTarget = methodTarget.TargetGenerationState.IsMultiTarget;
		var methodTargets = methodTarget.TargetGenerationState.MethodTargets;

		// Determine if Activity or Logging target owns the public method
		var activityOwnsPublicMethod = methodTargets.HasFlag(GenerationType.Activities);
		var loggingOwnsPublicMethod =
			!activityOwnsPublicMethod && methodTargets.HasFlag(GenerationType.Logging);
		var metricsOwnsPublicMethod = !activityOwnsPublicMethod && !loggingOwnsPublicMethod;

		// For multi-target where Activity or Logging owns public method, generate private method
		var accessModifier = isMultiTarget && !metricsOwnsPublicMethod ? "private" : "public";
		var methodName =
			isMultiTarget && !metricsOwnsPublicMethod
				? methodTarget.MethodName + "_Metrics"
				: methodTarget.MethodName;

		builder
			.CodeGen(indent)
			.AggressiveInlining(indent)
			.Append(indent, accessModifier + " ", withNewLine: false);

		// For multi-target private methods, always return void
		if (isMultiTarget && !metricsOwnsPublicMethod)
		{
			builder.Append(Constants.System.VoidKeyword);
		}
		else
		{
			builder.Append(methodTarget.ReturnType);
			if (methodTarget.IsNullableReturn)
				builder.Append('?');
		}

		builder.Append(' ').Append(methodName).Append('(');

		var index = 0;
		foreach (var parameter in methodTarget.Parameters)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			if (parameter.IsMeasurement)
			{
				var type = methodTarget.InstrumentMeasurementType;
				if (methodTarget.MeasurementParameter!.IsMeasurement)
					type = Constants.Metrics.SystemDiagnostics.Measurement.MakeGeneric(type);

				if (methodTarget.MeasurementParameter!.IsIEnumerable)
					type = Constants.System.GenericIEnumerable.MakeGeneric(type);

				type = Constants.System.Func.MakeGeneric(type);

				builder.Append(type);
			}
			else
			{
				builder.Append(parameter.ParameterType);
			}

			builder.Append(' ').Append(parameter.ParameterName);

			if (index < methodTarget.Parameters.Length - 1)
				builder.Append(", ");

			index++;
		}

		builder.AppendLine(')').Append(indent, '{');

		if (methodTarget.IsObservable)
			EmitObservableInstrumentBodyTest(builder, indent, methodTarget);
		else
			EmitInstrumentBodyTest(builder, indent, methodTarget);

		var tagVariableName = EmitTags(builder, indent, methodTarget);

		if (methodTarget.IsObservable)
			EmitObservableInstrumentBody(builder, indent, methodTarget, tagVariableName);
		else
			EmitInstrumentBody(builder, indent, methodTarget, tagVariableName);

		builder.Append(indent, '}');
	}

	static void EmitObservableInstrumentBodyTest(
		StringBuilder builder,
		int indent,
		InstrumentTarget method
	)
	{
		indent++;

		builder
			.Append(indent, "if (", withNewLine: false)
			.Append(method.FieldName)
			.AppendLine(" != null)")
			.Append(indent, '{');

		if (method.InstrumentAttribute?.ThrowOnAlreadyInitialized?.Value == true)
		{
			builder
				.Append(indent + 1, "throw new ", withNewLine: false)
				.Append(Constants.System.Exception)
				.Append("(\"")
				.Append(method.MetricName)
				.AppendLine(" has already been initialized.\");");
		}
		else
		{
			builder.Append(indent + 1, "return", withNewLine: false);

			if (method.ReturnsBool)
				builder.AppendLine(" false;");
			else
				builder.AppendLine(';');
		}

		builder.Append(indent, '}').AppendLine();
	}

	static void EmitInstrumentBodyTest(StringBuilder builder, int indent, InstrumentTarget method)
	{
		indent++;

		builder
			.Append(indent, "if (", withNewLine: false)
			.Append(method.FieldName)
			.AppendLine(" == null)")
			.Append(indent, '{');

		builder.Append(indent + 1, "return", withNewLine: false);

		if (method.ReturnsBool)
			builder.AppendLine(" false;");
		else
			builder.AppendLine(';');

		builder.Append(indent, '}').AppendLine();
	}

	static void EmitObservableInstrumentBody(
		StringBuilder builder,
		int indent,
		InstrumentTarget method,
		string? tagVariableName
	)
	{
		indent++;

		var unit = method.InstrumentAttribute!.Unit?.Value?.Wrap();
		var description = method.InstrumentAttribute!.Description?.Value?.Wrap();

		builder
			.Append(indent, method.FieldName, withNewLine: false)
			.Append(" = ")
			.Append(MeterFieldName)
			.Append(".Create")
			.Append(method.InstrumentAttribute!.InstrumentType)
			.Append('<')
			.Append(method.InstrumentMeasurementType)
			.Append(">(")
			.Append(method.MetricName.Wrap())
			.Append(", ")
			.Append(method.MeasurementParameter!.ParameterName)
			.Append(", unit: ")
			.Append(unit ?? Constants.System.NullKeyword)
			.Append(", description: ")
			.Append(description ?? Constants.System.NullKeyword);

		if (tagVariableName != null)
		{
			builder
				.AppendLine()
				.Append(indent + 1, ", tags: ", withNewLine: false)
				.AppendLine(tagVariableName)
				.WithIndent(indent);
		}

		builder.AppendLine(");");

		if (method.ReturnsBool)
		{
			builder.AppendLine().Append(indent, "return true;");
		}
	}

	static void EmitInstrumentBody(
		StringBuilder builder,
		int indent,
		InstrumentTarget methodTarget,
		string? tagVariableName
	)
	{
		indent++;

		var instrumentMeasureMethodName =
			methodTarget.InstrumentAttribute!.InstrumentType == InstrumentTypes.Histogram
				? "Record"
				: "Add";

		var tagCount = methodTarget.Tags.Length;
		var hasConditionalTags = methodTarget.Tags.Any(t => t.SkipOnNullOrEmpty);
		var useDirectTagParams = tagCount <= 3 && tagCount > 0 && !hasConditionalTags;

		builder
			.Append(indent, methodTarget.FieldName, withNewLine: false)
			.Append('.')
			.Append(instrumentMeasureMethodName)
			.Append('(')
			.Append(methodTarget.MeasurementParameter?.ParameterName ?? "1");

		if (useDirectTagParams)
		{
			// For 1-3 tags without conditionals, pass as direct KeyValuePair parameters
			foreach (var tag in methodTarget.Tags)
			{
				builder
					.Append(
						", new global::System.Collections.Generic.KeyValuePair<string, object?>("
					)
					.Append(tag.GeneratedName.Wrap())
					.Append(", ")
					.Append(tag.ParameterName)
					.Append(')');
			}

			builder.AppendLine(");");
		}
		else
		{
			// For 4+ tags or conditional tags, use TagList
			builder.Append(", tagList: ");

			if (tagVariableName == null)
				builder.Append("default");
			else
				builder.Append(tagVariableName);

			builder.AppendLine(");");
		}

		if (methodTarget.ReturnsBool)
		{
			builder.AppendLine().Append(indent, "return true;");
		}
	}

	static string? EmitTags(StringBuilder builder, int indent, InstrumentTarget methodTarget)
	{
		if (methodTarget.Tags.Length == 0)
			return null;

		var tagCount = methodTarget.Tags.Length;
		var hasConditionalTags = methodTarget.Tags.Any(t => t.SkipOnNullOrEmpty);

		// OpenTelemetry best practice:
		// - 0-3 tags without conditionals: pass directly as KeyValuePair parameters (no TagList needed)
		// - 4+ tags OR any conditional tags: use TagList to avoid heap allocation or handle conditionals
		// From: https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/metrics#instruments
		if (tagCount < Constants.Metrics.MinimumParamsForTagList && !hasConditionalTags)
		{
			// No TagList needed - tags will be passed directly as parameters
			return null;
		}

		indent++;

		var tagVariableName = Utilities.LowercaseFirstChar(methodTarget.MethodName + "TagList");
		builder
			.Append(indent, Constants.System.TagList, withNewLine: false)
			.Append(' ')
			.Append(tagVariableName)
			.Append(" = new")
			.AppendLine("();")
			.AppendLine();

		foreach (var param in methodTarget.Tags)
		{
			if (param.SkipOnNullOrEmpty)
			{
				builder
					.Append(indent, "if (", withNewLine: false)
					.Append(param.ParameterName)
					.AppendLine(" != default)")
					.Append(indent, "{");

				indent++;
			}

			builder
				.Append(indent, tagVariableName, withNewLine: false)
				.Append(".Add(")
				.Append(param.GeneratedName.Wrap())
				.Append(", ")
				.Append(param.ParameterName)
				.AppendLine(");");

			if (param.SkipOnNullOrEmpty)
			{
				indent--;

				builder.Append(indent, "}").AppendLine();
			}
		}

		builder.AppendLine();

		return tagVariableName;
	}
}
