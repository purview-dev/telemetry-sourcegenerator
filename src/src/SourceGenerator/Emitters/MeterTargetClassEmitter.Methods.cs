using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class MeterTargetClassEmitter
{
	static void EmitThrowStub(CodeWriter writer, InstrumentTarget methodTarget)
	{
		writer.NewLine();

		using (
			writer.MethodScope(
				new MethodDeclarationOptions(
					methodTarget.MethodName,
					methodTarget.ReturnType,
					TypeDeclarationAccessibility.Public
				)
				{
					Parameters =
					[
						.. methodTarget.Parameters.Select(p => new ParameterDeclarationOptions(
							p.ParameterName,
							p.ParameterType
						)),
					],
					ExpressionBody = "throw new global::System.NotSupportedException()",
					IncludeGeneratedAttributes = false,
				}
			)
		)
		{
			//
		}

		writer.NewLine();
	}

	static void EmitMethods(MeterOutputContext output, CodeWriter writer, SourceProductionContext context)
	{
		var target = output.Target;

		EmitPartialMethods(output, writer, context);

		foreach (var methodTarget in target.InstrumentationMethods)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			if (!methodTarget.TargetGenerationState.IsValid)
			{
				if (
					EmitterHelpers.ShouldEmitThrowStub(
						methodTarget.TargetGenerationState,
						GenerationType.Metrics,
						target.GenerationType
					)
				)
				{
					EmitThrowStub(writer, methodTarget);
				}
				continue;
			}

			// Report warning for Activity parameter without Activity target
			if (methodTarget.TargetGenerationState.ActivityParameterWithoutTarget != null)
			{
				output.Context.Debug(
					$"Activity parameter '{methodTarget.TargetGenerationState.ActivityParameterWithoutTarget}' on {methodTarget.MethodName} has no Activity target."
				);
			}

			EmitMethod(output, methodTarget, writer, context);
		}
	}

	static void EmitPartialMethods(MeterOutputContext output, CodeWriter writer, SourceProductionContext context)
	{
		var target = output.Target;
		context.CancellationToken.ThrowIfCancellationRequested();

		output.Context.Debug($"Emitting partial method for populating tags: {PartialMeterTagsMethod}.");

		var dictType = GetDictionaryType(writer);
		writer
			.NewLine()
			.Write("partial")
			.Write(" void ")
			.Write(PartialMeterTagsMethod)
			.Write('(')
			.Write(dictType)
			.Line(" meterTags);")
			.NewLine();

		foreach (var instrument in target.InstrumentationMethods)
		{
			if (!instrument.TargetGenerationState.IsValid)
				continue;

			if (instrument.IsObservable)
				continue;

			writer
				.Write("partial")
				.Write(" void ")
				.Write(instrument.TagPopulateMethodName)
				.Write('(')
				.Write(dictType)
				.Line(" instrumentTags);")
				.NewLine();
		}
	}

	static void EmitMethod(
		MeterOutputContext output,
		InstrumentTarget methodTarget,
		CodeWriter writer,
		SourceProductionContext context
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		if (methodTarget.InstrumentAttribute == null || ShouldSkipEmit(methodTarget))
			return;

		var isMultiTarget = methodTarget.TargetGenerationState.IsMultiTarget;
		var methodTargets = methodTarget.TargetGenerationState.MethodTargets;
		var activityOwnsPublicMethod = methodTargets.HasFlag(GenerationType.Activities);
		var loggingOwnsPublicMethod = !activityOwnsPublicMethod && methodTargets.HasFlag(GenerationType.Logging);
		var metricsOwnsPublicMethod = !activityOwnsPublicMethod && !loggingOwnsPublicMethod;

		output.Context.Debug($"Emitting instrument method: {methodTarget.MethodName}.");

		// For multi-target where Activity or Logging owns public method, generate private method
		var methodName =
			isMultiTarget && !metricsOwnsPublicMethod ? methodTarget.MethodName + "_Metrics" : methodTarget.MethodName;

		var returnType =
			isMultiTarget && !metricsOwnsPublicMethod
				? PurviewTypeLibrary.System.Void.AsTypeReference()
				: methodTarget.ReturnType;

		var parameters = BuildParameters(methodTarget);

		writer.NewLine();

		using (
			writer.MethodScope(
				new MethodDeclarationOptions(
					methodName,
					returnType,
					isMultiTarget && !metricsOwnsPublicMethod
						? TypeDeclarationAccessibility.Private
						: TypeDeclarationAccessibility.Public
				)
				{
					Parameters = parameters,
					IncludeGeneratedAttributes = false,
				}
			)
		)
		{
			if (methodTarget.IsObservable)
				EmitObservableInstrumentBodyTest(writer, methodTarget);

			var tagVariableName = EmitTags(writer, methodTarget);

			if (methodTarget.IsObservable)
				EmitObservableInstrumentBody(writer, methodTarget, tagVariableName);
			else
				EmitInstrumentBody(writer, methodTarget, tagVariableName);
		}
	}

	static bool ShouldSkipEmit(InstrumentTarget methodTarget)
	{
		var methodTargets = methodTarget.TargetGenerationState.MethodTargets;
		var activityOwnsPublicMethod = methodTargets.HasFlag(GenerationType.Activities);
		var loggingOwnsPublicMethod = !activityOwnsPublicMethod && methodTargets.HasFlag(GenerationType.Logging);
		var metricsOwnsPublicMethod = !activityOwnsPublicMethod && !loggingOwnsPublicMethod;

		// Metrics-owned public methods must return void or bool.
		var isVoidReturn = methodTarget.ReturnType.Identity.SpecialType == SpecialType.System_Void;
		if (metricsOwnsPublicMethod && !isVoidReturn && !methodTarget.ReturnsBool)
			return true;

		// Observable instruments cannot return bool.
		if (methodTarget.IsObservable && methodTarget.ReturnsBool)
			return true;

		// Auto-counter instruments must return void (not bool).
		if (methodTarget.InstrumentAttribute!.IsAutoIncrement && methodTarget.ReturnsBool)
			return true;

		// Auto-counter cannot also have a measurement parameter.
		if (methodTarget.InstrumentAttribute.IsAutoIncrement && methodTarget.MeasurementParameter != null)
			return true;

		// Non-auto-counter instruments require a measurement parameter.
		return !methodTarget.InstrumentAttribute.IsAutoIncrement && methodTarget.MeasurementParameter == null;
	}

	static ImmutableArray<ParameterDeclarationOptions> BuildParameters(InstrumentTarget methodTarget)
	{
		return
		[
			.. methodTarget.Parameters.Select(p =>
			{
				if (!p.IsMeasurement)
					return new ParameterDeclarationOptions(p.ParameterName, p.ParameterType);

				var type = methodTarget.InstrumentMeasurementType;
				if (methodTarget.MeasurementParameter!.IsMeasurement)
					type = TypeLibrary.Metrics.SystemDiagnostics.Measurement.MakeGeneric(type);

				if (methodTarget.MeasurementParameter!.IsIEnumerable)
					type = TypeLibrary.System.GenericIEnumerable.MakeGeneric(type);

				type = PurviewTypeLibrary.System.Func.MakeGeneric(type);

				return new ParameterDeclarationOptions(p.ParameterName, new TypeReference(type));
			}),
		];
	}

	static void EmitObservableInstrumentBodyTest(CodeWriter writer, InstrumentTarget method)
	{
		writer.IfBlock(
			method.FieldName + " != null",
			body =>
			{
				if (method.InstrumentAttribute?.ThrowOnAlreadyInitialized == true)
				{
					writer
						.Write("throw new ")
						.Write(TypeLibrary.System.Exception)
						.Write("(\"")
						.Write(method.MetricName)
						.Line(" has already been initialized.\");");
				}
				else
				{
					writer.Write("return");

					if (method.ReturnsBool)
						writer.Line(" false;");
					else
						writer.Write(";").NewLine();
				}
			}
		);

		writer.NewLine();
	}

	static void EmitObservableInstrumentBody(CodeWriter writer, InstrumentTarget method, string? tagVariableName)
	{
		var unit = method.InstrumentAttribute!.Unit?.Wrap();
		var description = method.InstrumentAttribute!.Description?.Wrap();

		writer
			.Write(method.FieldName)
			.Write(" = ")
			.Write(MeterFieldName)
			.Write(".Create")
			.Write(method.InstrumentAttribute!.InstrumentType.ToString())
			.Write('<')
			.Write(method.InstrumentMeasurementType)
			.Write(">(")
			.Write(method.MetricName.Wrap())
			.Write(", ")
			.Write(method.MeasurementParameter!.ParameterName)
			.Write(", unit: ")
			.Write(unit ?? PropertyLibrary.System.NullKeyword)
			.Write(", description: ")
			.Write(description ?? PropertyLibrary.System.NullKeyword);

		if (tagVariableName != null)
		{
			writer.NewLine().Write(", tags: ").Line(tagVariableName);
		}

		writer.Line(");");

		if (method.ReturnsBool)
		{
			writer.NewLine().Return("true");
		}
	}

	static void EmitInstrumentBody(
		CodeWriter writer,
		InstrumentTarget methodTarget,
		string? tagVariableName,
		bool emitNullable = true
	)
	{
		var instrumentMeasureMethodName =
			methodTarget.InstrumentAttribute!.InstrumentType == InstrumentTypes.Histogram ? "Record" : "Add";

		var tagCount = methodTarget.Tags.Count;
		var hasConditionalTags = methodTarget.Tags.Any(t => t.SkipOnNullOrEmpty);
		var useDirectTagParams = tagCount <= 3 && tagCount > 0 && !hasConditionalTags;

		writer
			.Write(methodTarget.FieldName)
			.Write('.')
			.Write(instrumentMeasureMethodName)
			.Write('(')
			.Write(methodTarget.MeasurementParameter?.ParameterName ?? "1");

		if (tagCount == 0)
		{
			// No tags — use the simple no-tag overload so the JIT can inline the hot path fully.
			// Passing tagList: default would route through the TagList overload unnecessarily.
			writer.Write(");").NewLine();
		}
		else if (useDirectTagParams)
		{
			// For 1-3 tags without conditionals, pass as direct KeyValuePair parameters
			var kvpType = emitNullable
				? "global::System.Collections.Generic.KeyValuePair<string, object?>("
				: "global::System.Collections.Generic.KeyValuePair<string, object>(";
			foreach (var tag in methodTarget.Tags)
			{
				writer
					.Write(", new ")
					.Write(kvpType)
					.Write(tag.GeneratedName.Wrap())
					.Write(", ")
					.Write(tag.ParameterName)
					.Write(')');
			}

			writer.Write(");").NewLine();
		}
		else
		{
			if (tagVariableName != null)
			{
				// 4+ tags or conditional tags: use the TagList variable
				writer.Write(", tagList: ").Write(tagVariableName);
			}

			// 0 tags: close the call with no tag argument, using the simple overload
			// (e.g. Add(1) or Record(value) rather than Add(1, tagList: default))
			writer.Write(");").NewLine();
		}

		if (methodTarget.ReturnsBool)
		{
			writer.NewLine().Return("true");
		}
	}

	static string? EmitTags(CodeWriter writer, InstrumentTarget methodTarget)
	{
		if (methodTarget.Tags.Count == 0)
			return null;

		var tagCount = methodTarget.Tags.Count;
		var hasConditionalTags = methodTarget.Tags.Any(t => t.SkipOnNullOrEmpty);

		// OpenTelemetry best practice:
		// - 0-3 tags without conditionals: pass directly as KeyValuePair parameters (no TagList needed)
		// - 4+ tags OR any conditional tags: use TagList to avoid heap allocation or handle conditionals
		// From: https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/metrics#instruments
		if (tagCount < PropertyLibrary.Metrics.MinimumParamsForTagList && !hasConditionalTags)
		{
			// No TagList needed - tags will be passed directly as parameters
			return null;
		}

		var tagVariableName = Utilities.LowercaseFirstChar(methodTarget.MethodName + "TagList");
		writer.Assignment(TypeLibrary.System.TagList, tagVariableName, "new()").NewLine();

		foreach (var param in methodTarget.Tags)
		{
			if (param.SkipOnNullOrEmpty)
			{
				writer.IfBlock(
					param.ParameterName + " != default",
					body => body.MethodCallOn(tagVariableName, "Add", param.GeneratedName.Wrap(), param.ParameterName)
				);
			}
			else
			{
				writer.MethodCallOn(tagVariableName, "Add", param.GeneratedName.Wrap(), param.ParameterName);
			}
		}

		writer.NewLine();

		return tagVariableName;
	}
}
