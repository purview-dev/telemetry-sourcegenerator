using System.Globalization;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class LoggerGenTargetClassEmitter
{
	static void EmitMethods(LoggerOutputContext output, CodeWriter writer, SourceProductionContext context)
	{
		var target = output.Target;
		foreach (var methodTarget in target.LogMethods)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			if (!methodTarget.TargetGenerationState.IsValid)
			{
				// HasLogPropertiesAndExpandEnumerable stubs have IsValid=false; report TSG2006 here.
				if (methodTarget.HasLogPropertiesAndExpandEnumerable)
				{
					LoggerTargetClassEmitter.EmitThrowStub(writer, methodTarget);
					continue;
				}

				if (
					EmitterHelpers.ShouldEmitThrowStub(
						methodTarget.TargetGenerationState,
						GenerationType.Logging,
						target.GenerationType
					)
				)
				{
					LoggerTargetClassEmitter.EmitThrowStub(writer, methodTarget);
				}
				continue;
			}

			if (methodTarget.UnknownReturnType)
			{
				LoggerTargetClassEmitter.EmitThrowStub(writer, methodTarget);
				continue; // Diagnostic already reported in EmitFields
			}

			// Report warning for Activity parameter without Activity target
			if (methodTarget.TargetGenerationState.ActivityParameterWithoutTarget != null)
			{
				output.Context.Debug(
					$"Activity parameter '{methodTarget.TargetGenerationState.ActivityParameterWithoutTarget}' on {methodTarget.MethodName} has no Activity target."
				);
			}

			// Report TSG2007: scoped method must not have an explicit log level set.
			// Must be checked before V1/V2 dispatch since V1 returns early.
			if (methodTarget.IsScoped && methodTarget.HasExplicitLevel)
			{
				output.Context.Diagnostic("Scoped method should not have an explicit log level.");
			}

			EmitMethod(output, methodTarget, writer, context);
		}
	}

	static void EmitMethod(
		LoggerOutputContext output,
		LogMethodTarget methodTarget,
		CodeWriter writer,
		SourceProductionContext context
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		output.Context.Debug($"Building logging method: {methodTarget.MethodName}");

		if (methodTarget.UseV1Generation)
		{
			// Only use v1 if within param limits; otherwise fall through to v2 (diagnostic already emitted in EmitFields)
			if (
				!methodTarget.HasMultipleExceptions
				&& methodTarget.ParameterCountSansException <= PropertyLibrary.Logging.MaxNonExceptionParameters
			)
			{
				LoggerTargetClassEmitter.EmitLogActionMethod(output, methodTarget, writer, context);
				return;
			}

			// HasMultipleExceptions or too many params - fall through to v2 generation (diagnostic reported in EmitFields)
		}

		var isMultiTarget = methodTarget.TargetGenerationState.IsMultiTarget;
		var methodTargets = methodTarget.TargetGenerationState.MethodTargets;

		// Determine ownership hierarchy: Activity > Logging > Metrics
		var activityOwnsPublicMethod = methodTargets.HasFlag(GenerationType.Activities);
		var loggingOwnsPublicMethod = !activityOwnsPublicMethod && methodTargets.HasFlag(GenerationType.Logging);
		var hasMetricsTarget = methodTargets.HasFlag(GenerationType.Metrics);

		// For multi-target where Logging owns the public method, we need to:
		// 1. Generate a private _Logging method
		// 2. Generate a public delegating method
		var generatePrivateLogging =
			isMultiTarget && (activityOwnsPublicMethod || (loggingOwnsPublicMethod && hasMetricsTarget));
		var generatePublicDelegator = isMultiTarget && loggingOwnsPublicMethod && hasMetricsTarget;

		var methodName = generatePrivateLogging ? methodTarget.MethodName + "_Logging" : methodTarget.MethodName;

		var returnType = methodTarget.IsScoped
			? TypeLibrary.System.IDisposable.AsTypeReference().Nullable(writer)
			: PurviewTypeLibrary.System.Void.AsTypeReference();

		writer.NewLine();

		using (
			writer.MethodScope(
				new MethodDeclarationOptions(
					methodName,
					returnType,
					generatePrivateLogging ? TypeDeclarationAccessibility.Private : TypeDeclarationAccessibility.Public
				)
				{
					Parameters =
					[
						.. methodTarget.Parameters.Select(p => new ParameterDeclarationOptions(
							p.Name,
							p.ParameterType
						)),
					],
					IncludeGeneratedAttributes = false,
				}
			)
		)
		{
			// Output state here...then we can use it in
			// the scoped and none-scoped output.
			// If we have exceptions, output them to the state...
			// UNLESS it's NOT-scoped and then we take the FIRST
			// exception and output it as the exception parameter in
			// the Log method.

			List<string> existingParamNames = [with(methodTarget.Parameters.Count)];
			foreach (var param in methodTarget.Parameters)
			{
				existingParamNames.Add(param.Name);
			}
			var stateVarName = FindUniqueName("state", existingParamNames);

			// Should always be state, because we'll use the messageFormat. And we'll generate one if
			// one doesn't exist...
			var useTypedState = !methodTarget.IsScoped && IsSimpleLogMethod(methodTarget);
			var useScopedTypedState = methodTarget.IsScoped && IsSimpleLogMethod(methodTarget);
			if (!methodTarget.IsScoped)
			{
				// First check if the the Log Level is enabled.
				// if (!_logger.IsEnabled(LogLevel.Information)))
				// { return; };
				// ...but only if it's not been scoped.
				writer.IfBlock(
					"!" + PropertyLibrary.Logging.LoggerFieldName + ".IsEnabled(" + methodTarget.MSLevel + ")",
					static body => body.Return()
				);

				writer.NewLine();
			}

			// Output the state here...
			if (!useTypedState && !useScopedTypedState)
			{
				EmitStateContent(output, writer, methodTarget, stateVarName, existingParamNames, context);
			}

			if (methodTarget.IsScoped)
				EmitScopedBody(writer, methodTarget, stateVarName, existingParamNames, useScopedTypedState, context);
			else
				EmitNonScopedBody(writer, methodTarget, stateVarName, existingParamNames, useTypedState);
		}

		// Generate public delegating method if Logging owns it
		if (generatePublicDelegator)
		{
			EmitPublicLoggingDelegatingMethod(output, methodTarget, writer, context);
		}
	}

	static void EmitScopedBody(
		CodeWriter writer,
		LogMethodTarget methodTarget,
		string stateVarName,
		List<string> existingParamNames,
		bool useScopedTypedState,
		SourceProductionContext context
	)
	{
		if (useScopedTypedState)
		{
			// Zero-allocation: typed _ScopeState struct — no ThreadLocalState, no eager string formatting.
			var scopeStructName = methodTarget.MethodName + "_ScopeState";
			var scopeNonExceptionParams = methodTarget.ParametersSansException;

			writer
				.Write("return ")
				.Write(PropertyLibrary.Logging.LoggerFieldName)
				.Write(".BeginScope(new ")
				.Write(scopeStructName)
				.Write('(');

			for (var i = 0; i < scopeNonExceptionParams.Count; i++)
			{
				context.CancellationToken.ThrowIfCancellationRequested();

				writer.Write(scopeNonExceptionParams[i].Name);
				if (i < scopeNonExceptionParams.Count - 1)
					writer.Write(", ");
			}

			writer.Write("));").NewLine();
			return;
		}

		var (interpolatedMessage, variables) = GenerateInterpolatedFunction(
			methodTarget.MessageTemplate,
			stateVarName,
			methodTarget.ExceptionParameter?.Name,
			[.. methodTarget.Parameters],
			existingParamNames
		);

		if (variables.Length > 0)
		{
			foreach (var variableDefinition in variables)
				writer.Line(variableDefinition);

			writer.NewLine();
		}

		var formattedMessageVarName = FindUniqueName("formattedMessage", existingParamNames);
		writer.Write("var ").Line("formattedMessage = ");

		using (writer.HashDefinesScope("NET"))
		{
			writer
				.Write("string.Create(global::System.Globalization.CultureInfo.InvariantCulture, $")
				.Write(interpolatedMessage.Wrap())
				.Line(");");

			writer.HashElse();

			writer.Write("global::System.FormattableString.Invariant($").Write(interpolatedMessage.Wrap()).Line(");");
		}

		writer.Write(";").NewLine();

		OutputState(
			writer,
			stateVarName,
			Utilities.UppercaseFirstChar(formattedMessageVarName).Wrap(),
			formattedMessageVarName,
			index: null
		);

		writer
			.NewLine()
			.Write("return ")
			.Write(PropertyLibrary.Logging.LoggerFieldName)
			.Write(".BeginScope(")
			.Write(stateVarName)
			.Line(");");
	}

	static void EmitNonScopedBody(
		CodeWriter writer,
		LogMethodTarget methodTarget,
		string stateVarName,
		List<string> existingParamNames,
		bool useTypedState
	)
	{
		var expressionStateVarName = FindUniqueName("s", existingParamNames);
		var expressionExceptionVarName =
			methodTarget.ExceptionParameter?.UsedInTemplate == true ? FindUniqueName("e", existingParamNames) : null;

		if (useTypedState)
		{
			EmitTypedStateLogCall(writer, methodTarget, expressionStateVarName, expressionExceptionVarName);
			return;
		}

		var (interpolatedMessage, variables) = GenerateInterpolatedFunction(
			methodTarget.MessageTemplate,
			expressionStateVarName,
			expressionExceptionVarName,
			[.. methodTarget.Parameters],
			existingParamNames
		);

		// Call the .Log method.
		var eventId = methodTarget.EventId ?? SharedHelpers.GetNonRandomizedHashCode(methodTarget.MethodName);
		writer
			.Write(PropertyLibrary.Logging.LoggerFieldName)
			.Line(".Log(")
			// Log level
			.Write(methodTarget.MSLevel.WithComma(andSpace: false))
			// Event Id
			.Write(
				writer.IsNullableContextEnabled is null or true
					? "new ("
					: "new " + TypeLibrary.Logging.MicrosoftExtensions.EventId + "("
			)
			.Write(eventId.ToString(CultureInfo.InvariantCulture))
			.Write(", nameof(")
			.Write(methodTarget.LogName)
			.Line(")),")
			// State
			.Write(stateVarName.WithComma(andSpace: false))
			// Exception
			.Write(methodTarget.ExceptionParameter.OrNullKeyword().WithComma(andSpace: false));

		// Message Template
		writer
			.Write(writer.IsNullableContextEnabled is null or true ? "static string (" : "(")
			.Write(expressionStateVarName)
			.Write(", ")
			.Write(expressionExceptionVarName ?? "_")
			.Line(") =>")
			.Line("{");

		if (variables.Length > 0)
		{
			foreach (var variableDefinition in variables)
				writer.Line(variableDefinition);

			writer.NewLine();
		}

		using (writer.HashDefinesScope("NET"))
		{
			writer
				.Write("return string.Create(global::System.Globalization.CultureInfo.InvariantCulture, $")
				.Write(interpolatedMessage.Wrap())
				.Line(");");

			writer.HashElse();

			writer
				.Write("return global::System.FormattableString.Invariant($")
				.Write(interpolatedMessage.Wrap())
				.Line(");");
		}

		writer.Write("}").Write(");");

		writer.NewLine().MethodCallOn(stateVarName, "Clear").NewLine();
	}

	static void EmitTypedStateLogCall(
		CodeWriter writer,
		LogMethodTarget methodTarget,
		string expressionStateVarName,
		string? expressionExceptionVarName
	)
	{
		// Typed state struct approach: zero boxing, no ThreadLocalState.
		var structName = methodTarget.MethodName + "_LogState";
		var interpolatedMessage = GenerateTypedInterpolatedMessage(
			methodTarget.MessageTemplate,
			expressionStateVarName,
			expressionExceptionVarName,
			[.. methodTarget.Parameters]
		);

		var eventId = methodTarget.EventId ?? SharedHelpers.GetNonRandomizedHashCode(methodTarget.MethodName);

		var nonExceptionParams = methodTarget.ParametersSansException;

		writer
			.Write(PropertyLibrary.Logging.LoggerFieldName)
			.Line(".Log(")
			.Write(methodTarget.MSLevel.WithComma(andSpace: false))
			.Write(
				writer.IsNullableContextEnabled is null or true
					? "new ("
					: "new " + TypeLibrary.Logging.MicrosoftExtensions.EventId + "("
			)
			.Write(eventId.ToString(CultureInfo.InvariantCulture))
			.Write(", nameof(")
			.Write(methodTarget.LogName)
			.Line(")),")
			.Write("new ")
			.Write(structName)
			.Write('(');

		for (var i = 0; i < nonExceptionParams.Count; i++)
		{
			writer.Write(nonExceptionParams[i].Name);
			if (i < nonExceptionParams.Count - 1)
				writer.Write(", ");
		}

		writer.Line("),");
		writer.Write(methodTarget.ExceptionParameter.OrNullKeyword().WithComma(andSpace: false));

		writer
			.Write(writer.IsNullableContextEnabled is null or true ? "static string (" : "(")
			.Write(expressionStateVarName)
			.Write(", ")
			.Write(expressionExceptionVarName ?? "_")
			.Line(") =>")
			.Line("{");

		using (writer.HashDefinesScope("NET"))
		{
			writer
				.Write("return string.Create(global::System.Globalization.CultureInfo.InvariantCulture, $")
				.Write(interpolatedMessage.Wrap())
				.Line(");");
			writer.HashElse();

			writer
				.Write("return global::System.FormattableString.Invariant($")
				.Write(interpolatedMessage.Wrap())
				.Line(");");
		}
		writer.Write("}").Write(");");

		writer.NewLine();
	}

	static void EmitStateContent(
		LoggerOutputContext output,
		CodeWriter writer,
		LogMethodTarget methodTarget,
		string stateVarName,
		List<string> existingParamNames,
		SourceProductionContext context
	)
	{
		output.Context.Debug("Emitting state content");

		// +1 for the OriginalFormat entry.
		var reservationCount = methodTarget.TotalParameterCount + 1;
		if (methodTarget.ExceptionParameter != null)
			reservationCount--;

		// Add compile-time-known LogProperties expansion counts to avoid dynamic array resizing.
		foreach (var param in methodTarget.Parameters)
		{
			if (param.LogProperties.HasValue)
				reservationCount += param.LogProperties.Value.Count;
		}

		// Create the state variable,
		// and reserve the required number of variables.
		writer
			.Write("var ")
			.Write(stateVarName)
			.Write(" = ")
			.Write(TypeLibrary.Logging.MicrosoftExtensions.LoggerMessageHelper)
			.Write('.')
			.Line("ThreadLocalState;")
			.Write(stateVarName)
			.Write(".ReserveTagSpace(")
			.Write(reservationCount.ToString(CultureInfo.InvariantCulture))
			.Line(");")
			.NewLine();

		// Original format is always at 0.
		OutputState(writer, stateVarName, "{OriginalFormat}".Wrap(), methodTarget.MessageTemplate.Wrap(), 0);

		var idx = 0;
		List<string>? postSetProperties = null;
		foreach (var parameter in methodTarget.Parameters)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			if (parameter.Name == methodTarget.ExceptionParameter?.Name)
			{
				// We need to skip over the exception parameter as
				// its passed directly to the .Log method.
				continue;
			}

			var isEnumerable = parameter.IsArray || parameter.IsIEnumerable;
			// Need to match the name against the value.
			OutputState(writer, stateVarName, parameter.Name.Wrap(), parameter.Name, ++idx, isEnumerable: isEnumerable);

			if (isEnumerable)
			{
				if (parameter.ExpandEnumerableAttribute != null)
				{
					postSetProperties ??= [];
					postSetProperties.Add(
						OutputExpandedEnumerable(writer, stateVarName, parameter, context, existingParamNames, output)
					);
				}
			}
			else if (parameter.LogProperties != null)
			{
				OutputLogPropertyDetails(
					writer,
					stateVarName,
					context,
					ref postSetProperties,
					parameter,
					existingParamNames
				);
			}
		}

		if (postSetProperties != null)
		{
			writer.NewLine();

			foreach (var nullableLogProperty in postSetProperties)
			{
				context.CancellationToken.ThrowIfCancellationRequested();
				writer.Line(nullableLogProperty);
			}
		}

		writer.NewLine();

		static void OutputLogPropertyDetails(
			CodeWriter writer,
			string stateVarName,
			SourceProductionContext context,
			ref List<string>? postPropertyDefinitions,
			LogParameterTarget parameter,
			List<string> existingParamNames
		)
		{
			_ = writer;
			CodeWriter logPropertiesWriter = new(
				GenerationSettings.Create<TelemetrySourceGenerator>(),
				throwOnUnclosedScopes: false
			);
			foreach (var logProperty in parameter.LogProperties!.Value)
			{
				context.CancellationToken.ThrowIfCancellationRequested();

				var logPropertyValue = $"{parameter.Name}?.{logProperty.PropertyName}";
				var logPropertyName = logProperty.PropertyName;
				if (!parameter.LogPropertiesAttribute!.Value.OmitReferenceName)
				{
					logPropertyName = $"{parameter.Name}.{logPropertyName}";
				}

				var shouldSkipNull =
					parameter.LogPropertiesAttribute!.Value.SkipNullProperties && logProperty.IsNullable;
				if (shouldSkipNull)
				{
					var tmpVarName = FindUniqueName("tmp", existingParamNames);
					logPropertiesWriter.Write("{");
					logPropertiesWriter.Write("var ").Write(tmpVarName).Write(" = ").Write(logPropertyValue).Line(";");
					logPropertiesWriter.IfBlock(
						tmpVarName + " != null",
						body => OutputState(body, stateVarName, logPropertyName.Wrap(), tmpVarName, null)
					);
					logPropertiesWriter.Write("}");
				}
				else
				{
					OutputState(logPropertiesWriter, stateVarName, logPropertyName.Wrap(), logPropertyValue, null);
				}

				postPropertyDefinitions ??= [];
				postPropertyDefinitions.Add(logPropertiesWriter.ToString().TrimEnd('\n'));

				logPropertiesWriter = new(
					GenerationSettings.Create<TelemetrySourceGenerator>(),
					throwOnUnclosedScopes: false
				);
			}
		}
	}

	static void OutputState(
		CodeWriter writer,
		string stateVarName,
		string propertyName,
		string value,
		int? index,
		bool isEnumerable = false,
		bool emitNullable = true
	)
	{
		writer.Write(stateVarName).Write('.');

		if (index.HasValue)
		{
			writer
				.Write("TagArray[")
				.Write(index.Value.ToString(CultureInfo.InvariantCulture))
				.Write("] = ")
				.Write(emitNullable ? "new(" : "new global::System.Collections.Generic.KeyValuePair<string, object>(");
		}
		else
		{
			writer.Write("AddTag(");
		}

		writer.Write(propertyName.WithComma());

		if (isEnumerable)
		{
			writer
				.Write(value)
				.Write(" == null ? null : ")
				.Write(TypeLibrary.Logging.MicrosoftExtensions.LoggerMessageHelper)
				.Write(".Stringify(")
				.Write(value)
				.Write(')');
		}
		else
		{
			writer.Write(value);
		}

		writer.Write(");").NewLine();
	}

	static string OutputExpandedEnumerable(
		CodeWriter writer,
		string stateVarName,
		LogParameterTarget parameter,
		SourceProductionContext context,
		List<string> existingParamNames,
		LoggerOutputContext output
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();
		_ = writer;

		CodeWriter snippet = new(GenerationSettings.Create<TelemetrySourceGenerator>(), throwOnUnclosedScopes: false);
		var iteratorVarName = FindUniqueName("tmp_i", existingParamNames);
		var iteratorItemVarName = FindUniqueName("item", existingParamNames);
		snippet.IfBlock(
			parameter.Name + " != null",
			body =>
			{
				body.Write("var ").Write(iteratorVarName).Line(" = 0;");

				var maxCount = parameter.ExpandEnumerableAttribute!.Value.MaximumValueCount;

				if (maxCount < 1)
					maxCount = 1;

				if (maxCount > PropertyLibrary.Logging.UnboundedIEnumerableMaxCountBeforeDiagnostic)
				{
					output.Context.Diagnostic(
						$"Identified {parameter.Name} that has a large unbounded ienumerable max."
					);
				}

				body.Foreach(
					"var " + iteratorItemVarName + " in " + parameter.Name,
					loopBody =>
					{
						loopBody.IfBlock(
							iteratorVarName + " == " + maxCount.ToString(CultureInfo.InvariantCulture),
							static breakBody => breakBody.Line("break;")
						);

						loopBody.NewLine();

						OutputState(
							loopBody,
							stateVarName,
							$"$\"{parameter.Name}[{{{iteratorVarName}}}]\"",
							iteratorItemVarName,
							null
						);

						loopBody.Write(iteratorVarName).Line("++;");
					}
				);
			}
		);

		return snippet.ToString().TrimEnd('\n');
	}

	static string FindUniqueName(string name, List<string> existingValues)
	{
		var i = 0;
		var originalName = name;
		while (existingValues.Contains(name))
		{
			name = $"{originalName}_{i}";
			i++;
		}

		existingValues.Add(name);

		return name;
	}

	static (string InteropolatedMessage, string[] Variables) GenerateInterpolatedFunction(
		string messageTemplate,
		string expressionStateVarName,
		string? expressionExceptionVarName,
		LogParameterTarget[] parameters,
		List<string> existingParamNames
	)
	{
		if (parameters.Length == 0)
			return (messageTemplate, Array.Empty<string>());

		List<string> variableDefinitions = [];
		Dictionary<string, string> replacements = [];
		Dictionary<MessageTemplateHole, int> holeIndexMap = [];
		var currentIndex = 0;

		foreach (var param in parameters)
		{
			foreach (var hole in param.ReferencedHoles)
				holeIndexMap[hole] = currentIndex++;
		}

		var escapedTemplate = messageTemplate.Replace("{{", "\u0001").Replace("}}", "\u0002");

		var exceptionParameter = parameters?.FirstOrDefault(p => p.IsFirstException);
		var exceptionUsedInTemplate = exceptionParameter?.UsedInTemplate == true;
		foreach (var hole in holeIndexMap.Keys)
		{
			var index = hole.IsPositional ? hole.Ordinal!.Value : holeIndexMap[hole];
			string varName;

			var isUsingExpressionException = exceptionParameter?.ReferencedHoles.Contains(hole) == true;
			if (isUsingExpressionException)
			{
				varName = expressionExceptionVarName!;
			}
			else
			{
				varName = FindUniqueName($"v{index}", existingParamNames);
				existingParamNames.Add(varName);

				// Define variable for every placeholder and ensure null safety
				var varAssignment =
					$"var {varName} = {expressionStateVarName}.TagArray[{index + 1}].Value ?? \"(null)\";";
				variableDefinitions.Add(varAssignment);
			}

			// If this hole belongs to the Exception parameter, use it directly
			var replacement =
				$"{{{varName}"
				+ $"{(hole.Alignment.HasValue ? $",{hole.Alignment}" : "")}"
				+ $"{(hole.Format != null ? $":{hole.Format}" : "")}}}";

			// Replace all occurrences of this hole’s placeholders
			var placeholder = hole.IsPositional ? $"{{{hole.Ordinal}}}" : $"{{{hole.Name}}}";
			escapedTemplate = escapedTemplate.Replace(placeholder, replacement);
		}

		escapedTemplate = escapedTemplate.Replace("\u0001", "{{").Replace("\u0002", "}}");

		return (escapedTemplate, [.. variableDefinitions]);
	}

	static bool IsSimpleLogMethod(LogMethodTarget methodTarget)
	{
		foreach (var param in methodTarget.Parameters)
		{
			if (param.LogPropertiesAttribute != null)
				return false;
			if (param.ExpandEnumerableAttribute != null)
				return false;
		}

		return true;
	}

	static string GenerateTypedInterpolatedMessage(
		string messageTemplate,
		string? expressionStateVarName,
		string? expressionExceptionVarName,
		LogParameterTarget[] parameters
	)
	{
		if (parameters.Length == 0)
			return messageTemplate;

		Dictionary<MessageTemplateHole, string> holeToAccess = [];

		foreach (var param in parameters)
		{
			foreach (var hole in param.ReferencedHoles)
			{
				if (param.IsException && expressionExceptionVarName != null)
					holeToAccess[hole] = expressionExceptionVarName;
				else if (!param.IsException)
					holeToAccess[hole] =
						expressionStateVarName != null
							? $"{expressionStateVarName}._{param.UpperCasedName}"
							: $"_{param.UpperCasedName}";
			}
		}

		if (holeToAccess.Count == 0)
			return messageTemplate;

		var escapedTemplate = messageTemplate.Replace("{{", "\u0001").Replace("}}", "\u0002");

		foreach (var kvp in holeToAccess)
		{
			var hole = kvp.Key;
			var fieldAccess = kvp.Value;
			var replacement =
				$"{{{fieldAccess}"
				+ (hole.Alignment.HasValue ? $",{hole.Alignment}" : "")
				+ (hole.Format != null ? $":{hole.Format}" : "")
				+ "}";

			var placeholder = hole.IsPositional ? $"{{{hole.Ordinal}}}" : $"{{{hole.Name}}}";
			escapedTemplate = escapedTemplate.Replace(placeholder, replacement);
		}

		escapedTemplate = escapedTemplate.Replace("\u0001", "{{").Replace("\u0002", "}}");

		return escapedTemplate;
	}

	static void EmitLogStateStructs(LoggerOutputContext output, CodeWriter writer, SourceProductionContext context)
	{
		var target = output.Target;
		foreach (var methodTarget in target.LogMethods)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			if (!methodTarget.TargetGenerationState.IsValid)
				continue;

			if (
				methodTarget.UseV1Generation
				&& !methodTarget.HasMultipleExceptions
				&& methodTarget.ParameterCountSansException <= PropertyLibrary.Logging.MaxNonExceptionParameters
			)
				continue;

			if (!IsSimpleLogMethod(methodTarget))
				continue;

			if (methodTarget.IsScoped)
				EmitScopeStateStruct(writer, methodTarget, context);
			else
				EmitLogStateStruct(writer, methodTarget, context);
		}
	}

	static void EmitLogStateStruct(CodeWriter writer, LogMethodTarget methodTarget, SourceProductionContext context)
	{
		var nonExceptionParams = methodTarget.ParametersSansException;
		var structName = methodTarget.MethodName + "_LogState";
		var count = nonExceptionParams.Count + 1; // +1 for {OriginalFormat}

		var kvpType =
			$"global::System.Collections.Generic.KeyValuePair<string, {PurviewTypeLibrary.System.Object.MakeNullable(writer)}>";
		var iReadOnlyListType = $"global::System.Collections.Generic.IReadOnlyList<{kvpType}>";
		var ienumeratorType = $"global::System.Collections.Generic.IEnumerator<{kvpType}>";
		var ienumerableKvpType = $"global::System.Collections.Generic.IEnumerable<{kvpType}>";
		const string ienumerableType = "global::System.Collections.IEnumerator";

		writer.NewLine();

		using (
			writer.StructScope(
				new TypeDeclarationOptions(structName, TypeDeclarationAccessibility.Private)
				{
					IsReadOnly = true,
					Interfaces = [new TypeReference(new TypeIdentity(iReadOnlyListType, null))],
					IncludeGeneratedAttributes = false,
				}
			)
		)
		{
			writer.Field(
				new FieldDeclarationOptions("s_originalFormat", PurviewTypeLibrary.System.String.AsTypeReference())
				{
					IsStatic = true,
					IsReadOnly = true,
					Initializer = methodTarget.MessageTemplate.Wrap(),
					IncludeGeneratedAttributes = false,
				}
			);

			if (nonExceptionParams.Count > 0)
			{
				writer.NewLine();

				foreach (var param in nonExceptionParams)
				{
					context.CancellationToken.ThrowIfCancellationRequested();

					writer.Field(
						new FieldDeclarationOptions($"_{param.UpperCasedName}", param.ParameterType)
						{
							Accessibility = TypeDeclarationAccessibility.Public,
							IsReadOnly = true,
							IncludeGeneratedAttributes = false,
						}
					);
				}

				writer.NewLine();

				writer.Constructor(
					new ConstructorDeclarationOptions(structName, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							.. nonExceptionParams.Select(p => new ParameterDeclarationOptions(p.Name, p.ParameterType)),
						],
						IncludeGeneratedAttributes = false,
					},
					ctor =>
					{
						foreach (var param in nonExceptionParams)
						{
							context.CancellationToken.ThrowIfCancellationRequested();

							ctor.Write("_").Write(param.UpperCasedName).Write(" = ").Write(param.Name).Line(";");
						}
					}
				);
			}

			writer
				.NewLine()
				.NewLine()
				.Property(
					new PropertyDeclarationOptions(
						"Count",
						PurviewTypeLibrary.System.Int32.AsTypeReference(),
						TypeDeclarationAccessibility.Public
					)
					{
						ExpressionBody = count.ToString(CultureInfo.InvariantCulture),
						IncludeGeneratedAttributes = false,
					}
				)
				.NewLine();

			writer.Indexer(
				new IndexerDeclarationOptions(
					new TypeReference(new TypeIdentity(kvpType, null)),
					new ParameterDeclarationOptions("index", PurviewTypeLibrary.System.Int32.AsTypeReference())
				)
				{
					Accessibility = TypeDeclarationAccessibility.Public,
					IncludeGeneratedAttributes = false,
				},
				getter =>
				{
					if (writer.IsNullableContextEnabled is null or true)
					{
						getter.Line("return index switch {");
						getter.Indent();
						getter.Line("0 => new(\"{OriginalFormat}\", s_originalFormat),");

						for (var i = 0; i < nonExceptionParams.Count; i++)
						{
							context.CancellationToken.ThrowIfCancellationRequested();

							getter
								.Write($"{i + 1} => new(")
								.Write(nonExceptionParams[i].Name.Wrap())
								.Write(", _")
								.Write(nonExceptionParams[i].UpperCasedName)
								.Line("),");
						}

						getter.Line("_ => throw new global::System.IndexOutOfRangeException(nameof(index))");
						getter.Unindent();
						getter.Line("};");
					}
					else
					{
						getter.Write("switch (index)");
						using (getter.OpenBlockScope())
						{
							getter.Line("case 0: return new " + kvpType + "(\"{OriginalFormat}\", s_originalFormat);");

							for (var i = 0; i < nonExceptionParams.Count; i++)
							{
								context.CancellationToken.ThrowIfCancellationRequested();

								getter
									.Write($"case {i + 1}: return new " + kvpType + "(")
									.Write(nonExceptionParams[i].Name.Wrap())
									.Write(", _")
									.Write(nonExceptionParams[i].UpperCasedName)
									.Line(");");
							}

							getter
								.Write("default: throw new global::System.IndexOutOfRangeException(nameof(index))")
								.Line(";");
						}
					}
				},
				null
			);

			EmitStructEnumerator(writer, structName, kvpType, ienumeratorType, ienumerableType, ienumerableKvpType);
		}

		writer.NewLine();
	}

	static void EmitStructEnumerator(
		CodeWriter writer,
		string structName,
		string kvpType,
		string ienumeratorType,
		string ienumerableType,
		string ienumerableKvpType
	)
	{
		writer.NewLine();

		using (
			writer.StructScope(
				new TypeDeclarationOptions("Enumerator", TypeDeclarationAccessibility.Public)
				{
					IncludeGeneratedAttributes = false,
					Interfaces = [new TypeReference(new TypeIdentity(ienumeratorType, null))],
				}
			)
		)
		{
			writer.Field(
				new FieldDeclarationOptions("_state", new TypeReference(new TypeIdentity(structName, null)))
				{
					IsReadOnly = true,
					IncludeGeneratedAttributes = false,
				}
			);

			writer.Field(
				new FieldDeclarationOptions("_index", PurviewTypeLibrary.System.Int32.AsTypeReference())
				{
					IncludeGeneratedAttributes = false,
				}
			);

			writer.NewLine();

			writer.Constructor(
				new ConstructorDeclarationOptions("Enumerator", TypeDeclarationAccessibility.Public)
				{
					Parameters =
					[
						new ParameterDeclarationOptions("state", new TypeReference(new TypeIdentity(structName, null))),
					],
					IncludeGeneratedAttributes = false,
				},
				ctor =>
				{
					ctor.Assignment("_state", "state").NewLine();
					ctor.Assignment("_index", "-1").NewLine();
				}
			);

			writer
				.NewLine()
				.Property(
					new PropertyDeclarationOptions(
						"Current",
						new TypeReference(new TypeIdentity(kvpType, null)),
						TypeDeclarationAccessibility.Public
					)
					{
						ExpressionBody = "_state[_index]",
						IncludeGeneratedAttributes = false,
					}
				)
				.NewLine()
				.NewLine()
				.Write(PurviewTypeLibrary.System.Object.MakeNullable(writer))
				.Write(" global::System.Collections.IEnumerator.Current")
				.Line(" => Current;")
				.NewLine()
				.NewLine()
				.MethodExpression(
					new MethodDeclarationOptions(
						"MoveNext",
						PurviewTypeLibrary.System.Boolean.AsTypeReference(),
						TypeDeclarationAccessibility.Public
					)
					{
						ExpressionBody = "++_index < _state.Count",
						IncludeGeneratedAttributes = false,
					}
				)
				.NewLine()
				.NewLine()
				.MethodExpression(
					new MethodDeclarationOptions(
						"Reset",
						PurviewTypeLibrary.System.Void.AsTypeReference(),
						TypeDeclarationAccessibility.Public
					)
					{
						ExpressionBody = "_index = -1",
						IncludeGeneratedAttributes = false,
					}
				)
				.NewLine()
				.NewLine()
				.Method(
					new MethodDeclarationOptions(
						"Dispose",
						PurviewTypeLibrary.System.Void.AsTypeReference(),
						TypeDeclarationAccessibility.Public
					)
					{
						IncludeGeneratedAttributes = false,
					},
					_ => { }
				);
		}

		writer
			.NewLine()
			.NewLine()
			.MethodExpression(
				new MethodDeclarationOptions(
					"GetEnumerator",
					new TypeReference(new TypeIdentity("Enumerator", null)),
					TypeDeclarationAccessibility.Public
				)
				{
					ExpressionBody = "new Enumerator(this)",
					IncludeGeneratedAttributes = false,
				}
			)
			.NewLine()
			.NewLine()
			.Write(ienumeratorType + " " + ienumerableKvpType + ".GetEnumerator() => GetEnumerator()")
			.Line(";")
			.NewLine()
			.NewLine()
			.Write(ienumerableType + " global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator()")
			.Line(";");
	}

	static void EmitScopeStateStruct(CodeWriter writer, LogMethodTarget methodTarget, SourceProductionContext context)
	{
		var nonExceptionParams = methodTarget.ParametersSansException;
		var structName = methodTarget.MethodName + "_ScopeState";
		var count = nonExceptionParams.Count + 1; // +1 for {OriginalFormat}

		var kvpType =
			$"global::System.Collections.Generic.KeyValuePair<string, {PurviewTypeLibrary.System.Object.MakeNullable(writer)}>";
		var iReadOnlyListType = $"global::System.Collections.Generic.IReadOnlyList<{kvpType}>";
		var ienumeratorType = $"global::System.Collections.Generic.IEnumerator<{kvpType}>";
		var ienumerableKvpType = $"global::System.Collections.Generic.IEnumerable<{kvpType}>";
		const string ienumerableType = "global::System.Collections.IEnumerator";

		writer.NewLine();

		using (
			writer.StructScope(
				new TypeDeclarationOptions(structName, TypeDeclarationAccessibility.Private)
				{
					IsReadOnly = true,
					Interfaces = [new TypeReference(new(iReadOnlyListType, null))],
					IncludeGeneratedAttributes = false,
				}
			)
		)
		{
			writer.Field(
				new FieldDeclarationOptions("s_originalFormat", PurviewTypeLibrary.System.String.AsTypeReference())
				{
					IsStatic = true,
					IsReadOnly = true,
					Initializer = methodTarget.MessageTemplate.Wrap(),
					IncludeGeneratedAttributes = false,
				}
			);

			if (nonExceptionParams.Count > 0)
			{
				writer.NewLine();

				foreach (var param in nonExceptionParams)
				{
					context.CancellationToken.ThrowIfCancellationRequested();

					writer.Field(
						new FieldDeclarationOptions($"_{param.UpperCasedName}", param.ParameterType)
						{
							Accessibility = TypeDeclarationAccessibility.Public,
							IsReadOnly = true,
							IncludeGeneratedAttributes = false,
						}
					);
				}

				writer.NewLine();

				writer.Constructor(
					new ConstructorDeclarationOptions(structName, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							.. nonExceptionParams.Select(p => new ParameterDeclarationOptions(p.Name, p.ParameterType)),
						],
						IncludeGeneratedAttributes = false,
					},
					ctor =>
					{
						foreach (var param in nonExceptionParams)
						{
							context.CancellationToken.ThrowIfCancellationRequested();

							ctor.Write("_").Write(param.UpperCasedName).Write(" = ").Write(param.Name).Line(";");
						}
					}
				);
			}

			// Lazy ToString() — format is deferred until a provider actually needs the string.
			var interpolatedMessage = GenerateTypedInterpolatedMessage(
				methodTarget.MessageTemplate,
				null, // null = direct field access (_FieldName instead of s._FieldName)
				null, // scoped methods have no exception parameter
				[.. methodTarget.Parameters]
			);

			writer
				.NewLine()
				.NewLine()
				.Method(
					new MethodDeclarationOptions(
						"ToString",
						PurviewTypeLibrary.System.String.AsTypeReference(),
						TypeDeclarationAccessibility.Public
					)
					{
						IsOverride = true,
						IncludeGeneratedAttributes = false,
					},
					body =>
						body.HashDefines(
							"NET",
							hashWriter =>
								hashWriter
									.Write(
										"return string.Create(global::System.Globalization.CultureInfo.InvariantCulture, $"
									)
									.Write(interpolatedMessage.Wrap())
									.Line(");")
									.HashElse()
									.Write("return global::System.FormattableString.Invariant($")
									.Write(interpolatedMessage.Wrap())
									.Line(");")
						)
				);

			writer
				.NewLine()
				.NewLine()
				.Property(
					new PropertyDeclarationOptions(
						"Count",
						PurviewTypeLibrary.System.Int32.AsTypeReference(),
						TypeDeclarationAccessibility.Public
					)
					{
						ExpressionBody = count.ToString(CultureInfo.InvariantCulture),
						IncludeGeneratedAttributes = false,
					}
				)
				.NewLine();

			writer.Indexer(
				new IndexerDeclarationOptions(
					new TypeReference(new TypeIdentity(kvpType, null)),
					new ParameterDeclarationOptions("index", PurviewTypeLibrary.System.Int32.AsTypeReference())
				)
				{
					Accessibility = TypeDeclarationAccessibility.Public,
					IncludeGeneratedAttributes = false,
				},
				getter =>
				{
					if (writer.IsNullableContextEnabled is null or true)
					{
						getter.Line("return index switch {");
						getter.Indent();
						getter.Line("0 => new(\"{OriginalFormat}\", s_originalFormat),");

						for (var i = 0; i < nonExceptionParams.Count; i++)
						{
							context.CancellationToken.ThrowIfCancellationRequested();

							getter
								.Write($"{i + 1} => new(")
								.Write(nonExceptionParams[i].Name.Wrap())
								.Write(", _")
								.Write(nonExceptionParams[i].UpperCasedName)
								.Line("),");
						}

						getter.Line("_ => throw new global::System.IndexOutOfRangeException(nameof(index))");
						getter.Unindent();
						getter.Line("};");
					}
					else
					{
						getter.Write("switch (index)");
						using (getter.OpenBlockScope())
						{
							getter.Line("case 0: return new " + kvpType + "(\"{OriginalFormat}\", s_originalFormat);");

							for (var i = 0; i < nonExceptionParams.Count; i++)
							{
								context.CancellationToken.ThrowIfCancellationRequested();

								getter
									.Write($"case {i + 1}: return new " + kvpType + "(")
									.Write(nonExceptionParams[i].Name.Wrap())
									.Write(", _")
									.Write(nonExceptionParams[i].UpperCasedName)
									.Line(");");
							}

							getter
								.Write("default: throw new global::System.IndexOutOfRangeException(nameof(index))")
								.Line(";");
						}
					}
				},
				null
			);

			EmitStructEnumerator(writer, structName, kvpType, ienumeratorType, ienumerableType, ienumerableKvpType);
		}

		writer.NewLine();
	}

	static void EmitPublicLoggingDelegatingMethod(
		LoggerOutputContext output,
		LogMethodTarget methodTarget,
		CodeWriter writer,
		SourceProductionContext context
	)
	{
		output.Context.Debug($"Building public delegating logging method: {methodTarget.MethodName}");

		var returnType = methodTarget.IsScoped
			? TypeLibrary.System.IDisposable.MakeNullable(writer)
			: PurviewTypeLibrary.System.Void.AsTypeReference();

		writer.NewLine();

		using (
			writer.MethodScope(
				new MethodDeclarationOptions(methodTarget.MethodName, returnType, TypeDeclarationAccessibility.Public)
				{
					Parameters =
					[
						.. methodTarget.Parameters.Select(p => new ParameterDeclarationOptions(
							p.Name,
							p.ParameterType
						)),
					],
					IncludeGeneratedAttributes = false,
				}
			)
		)
		{
			// Call the private Logging method
			if (methodTarget.IsScoped)
			{
				writer.Write("var loggingResult = ");
			}
			else
			{
				writer.Write(methodTarget.MethodName).Write("_Logging(");
			}

			if (!methodTarget.IsScoped)
			{
				// Emit parameters
				for (var i = 0; i < methodTarget.TotalParameterCount; i++)
				{
					context.CancellationToken.ThrowIfCancellationRequested();

					writer.Write(methodTarget.Parameters[i].Name);

					if (i < methodTarget.TotalParameterCount - 1)
						writer.Write(", ");
				}
				writer.Write(");").NewLine();
			}
			else
			{
				// For scoped, we need special handling
				writer.Write(methodTarget.MethodName).Write("_Logging(");
				for (var i = 0; i < methodTarget.TotalParameterCount; i++)
				{
					context.CancellationToken.ThrowIfCancellationRequested();

					writer.Write(methodTarget.Parameters[i].Name);

					if (i < methodTarget.TotalParameterCount - 1)
						writer.Write(", ");
				}
				writer.Write(");").NewLine();
			}

			// Call the private Metrics method
			writer.Write(methodTarget.MethodName).Write("_Metrics(");

			for (var i = 0; i < methodTarget.TotalParameterCount; i++)
			{
				context.CancellationToken.ThrowIfCancellationRequested();

				writer.Write(methodTarget.Parameters[i].Name);

				if (i < methodTarget.TotalParameterCount - 1)
					writer.Write(", ");
			}
			writer.Write(");").NewLine();

			// Return if scoped
			if (methodTarget.IsScoped)
			{
				writer.NewLine().Return("loggingResult");
			}
		}

		writer.NewLine();
	}
}
