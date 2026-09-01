using System.Globalization;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class LoggerGenTargetClassEmitter
{
	static void EmitMethods(
		LoggerTarget target,
		CodeWriter writer,
		SourceProductionContext context,
		ISourceGenLogger? logger,
		bool emitNullable
	)
	{
		foreach (var methodTarget in target.LogMethods)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			if (!methodTarget.TargetGenerationState.IsValid)
			{
				// HasLogPropertiesAndExpandEnumerable stubs have IsValid=false; report TSG2006 here.
				if (methodTarget.HasLogPropertiesAndExpandEnumerable)
				{
					LoggerTargetClassEmitter.EmitThrowStub(writer, methodTarget, emitNullable);
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
					LoggerTargetClassEmitter.EmitThrowStub(writer, methodTarget, emitNullable);
				}
				continue;
			}

			if (methodTarget.UnknownReturnType)
			{
				LoggerTargetClassEmitter.EmitThrowStub(writer, methodTarget, emitNullable);
				continue; // Diagnostic already reported in EmitFields
			}

			// Report warning for Activity parameter without Activity target
			if (methodTarget.TargetGenerationState.ActivityParameterWithoutTarget != null)
			{
				logger?.Debug(
					$"Activity parameter '{methodTarget.TargetGenerationState.ActivityParameterWithoutTarget}' on {methodTarget.MethodName} has no Activity target."
				);
			}

			// Report TSG2007: scoped method must not have an explicit log level set.
			// Must be checked before V1/V2 dispatch since V1 returns early.
			if (methodTarget.IsScoped && methodTarget.HasExplicitLevel)
			{
				logger?.Diagnostic("Scoped method should not have an explicit log level.");
			}

			EmitMethod(writer, methodTarget, context, logger, emitNullable);
		}
	}

	static void EmitMethod(
		CodeWriter writer,
		LogMethodTarget methodTarget,
		SourceProductionContext context,
		ISourceGenLogger? logger,
		bool emitNullable
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Building logging method: {methodTarget.MethodName}");

		if (methodTarget.UseV1Generation)
		{
			// Only use v1 if within param limits; otherwise fall through to v2 (diagnostic already emitted in EmitFields)
			if (
				!methodTarget.HasMultipleExceptions
				&& methodTarget.ParameterCountSansException <= PropertyLibrary.Logging.MaxNonExceptionParameters
			)
			{
				LoggerTargetClassEmitter.EmitLogActionMethod(writer, methodTarget, context, logger, emitNullable);
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
			? emitNullable
				? TypeLibrary.System.IDisposable.AsTypeReference().Nullable()
				: TypeLibrary.System.IDisposable.AsTypeReference()
			: PurviewTypeLibrary.System.Void.AsTypeReference();

		writer.NewLine();

		using (
			writer.WriteMethodScope(
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

			List<string> existingParamNames = new(methodTarget.Parameters.Count);
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
				writer
					.Write("if (!")
					.Write(PropertyLibrary.Logging.LoggerFieldName)
					.Write(".IsEnabled(")
					.Write(methodTarget.MSLevel)
					.WriteLine("))");

				using (writer.OpenBlockScope())
					writer.WriteLine("return;");

				writer.NewLine();
			}

			// Output the state here...
			if (!useTypedState && !useScopedTypedState)
			{
				EmitStateContent(writer, methodTarget, stateVarName, existingParamNames, context, logger, emitNullable);
			}

			if (methodTarget.IsScoped)
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
				}
				else
				{
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
							writer.WriteLine(variableDefinition);

						writer.NewLine();
					}

					var formattedMessageVarName = FindUniqueName("formattedMessage", existingParamNames);
					writer
						.Write("var ")
						.WriteLine("formattedMessage = ")
						.WriteLine("#if NET")
						.Write("string.Create(global::System.Globalization.CultureInfo.InvariantCulture, $")
						.Write(interpolatedMessage.Wrap())
						.WriteLine(");")
						.WriteLine("#else")
						.Write("global::System.FormattableString.Invariant($")
						.Write(interpolatedMessage.Wrap())
						.WriteLine(");")
						.WriteLine("#endif")
						.Write(";")
						.NewLine();

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
						.WriteLine(");");
				}
			}
			else
			{
				var expressionStateVarName = FindUniqueName("s", existingParamNames);
				var expressionExceptionVarName =
					methodTarget.ExceptionParameter?.UsedInTemplate == true
						? FindUniqueName("e", existingParamNames)
						: null;

				if (useTypedState)
				{
					// Typed state struct approach: zero boxing, no ThreadLocalState.
					var structName = methodTarget.MethodName + "_LogState";
					var interpolatedMessage = GenerateTypedInterpolatedMessage(
						methodTarget.MessageTemplate,
						expressionStateVarName,
						expressionExceptionVarName,
						[.. methodTarget.Parameters]
					);

					var eventId =
						methodTarget.EventId ?? SharedHelpers.GetNonRandomizedHashCode(methodTarget.MethodName);

					var nonExceptionParams = methodTarget.ParametersSansException;

					writer
						.Write(PropertyLibrary.Logging.LoggerFieldName)
						.WriteLine(".Log(")
						.Write(methodTarget.MSLevel.WithComma(andSpace: false))
						.Write(emitNullable ? "new (" : "new " + TypeLibrary.Logging.MicrosoftExtensions.EventId + "(")
						.Write(eventId.ToString(CultureInfo.InvariantCulture))
						.Write(", nameof(")
						.Write(methodTarget.LogName)
						.WriteLine(")),")
						.Write("new ")
						.Write(structName)
						.Write('(');

					for (var i = 0; i < nonExceptionParams.Count; i++)
					{
						writer.Write(nonExceptionParams[i].Name);
						if (i < nonExceptionParams.Count - 1)
							writer.Write(", ");
					}

					writer.WriteLine("),");
					writer.Write(methodTarget.ExceptionParameter.OrNullKeyword().WithComma(andSpace: false));

					if (emitNullable)
						writer
							.Write(emitNullable ? "static string (" : "(")
							.Write(expressionStateVarName)
							.Write(", ")
							.Write(expressionExceptionVarName ?? "_")
							.WriteLine(") =>")
							.WriteLine("{")
							.WriteLine("#if NET")
							.Write("return string.Create(global::System.Globalization.CultureInfo.InvariantCulture, $")
							.Write(interpolatedMessage.Wrap())
							.WriteLine(");")
							.WriteLine("#else")
							.Write("return global::System.FormattableString.Invariant($")
							.Write(interpolatedMessage.Wrap())
							.WriteLine(");")
							.WriteLine("#endif")
							.Write("}")
							.Write(");");

					writer.NewLine();
				}
				else
				{
					var (interpolatedMessage, variables) = GenerateInterpolatedFunction(
						methodTarget.MessageTemplate,
						expressionStateVarName,
						expressionExceptionVarName,
						[.. methodTarget.Parameters],
						existingParamNames
					);

					// Call the .Log method.
					var eventId =
						methodTarget.EventId ?? SharedHelpers.GetNonRandomizedHashCode(methodTarget.MethodName);
					writer
						.Write(PropertyLibrary.Logging.LoggerFieldName)
						.WriteLine(".Log(")
						// Log level
						.Write(methodTarget.MSLevel.WithComma(andSpace: false))
						// Event Id
						.Write(emitNullable ? "new (" : "new " + TypeLibrary.Logging.MicrosoftExtensions.EventId + "(")
						.Write(eventId.ToString(CultureInfo.InvariantCulture))
						.Write(", nameof(")
						.Write(methodTarget.LogName)
						.WriteLine(")),")
						// State
						.Write(stateVarName.WithComma(andSpace: false))
						// Exception
						.Write(methodTarget.ExceptionParameter.OrNullKeyword().WithComma(andSpace: false));
					// Message Template
					if (emitNullable)
						writer
							.Write(emitNullable ? "static string (" : "(")
							.Write(expressionStateVarName)
							.Write(", ")
							.Write(expressionExceptionVarName ?? "_")
							.WriteLine(") =>")
							.WriteLine("{");

					if (variables.Length > 0)
					{
						foreach (var variableDefinition in variables)
							writer.WriteLine(variableDefinition);

						writer.NewLine();
					}

					writer
						.WriteLine("#if NET")
						.Write("return string.Create(global::System.Globalization.CultureInfo.InvariantCulture, $")
						.Write(interpolatedMessage.Wrap())
						.WriteLine(");")
						.WriteLine("#else")
						.Write("return global::System.FormattableString.Invariant($")
						.Write(interpolatedMessage.Wrap())
						.WriteLine(");")
						.WriteLine("#endif")
						.Write("}")
						.Write(");");

					writer.NewLine().Write(stateVarName).Write(".Clear();").NewLine();
				}
			}
		}

		// Generate public delegating method if Logging owns it
		if (generatePublicDelegator)
		{
			EmitPublicLoggingDelegatingMethod(writer, methodTarget, context, logger, emitNullable);
		}
	}

	static void EmitStateContent(
		CodeWriter writer,
		LogMethodTarget methodTarget,
		string stateVarName,
		List<string> existingParamNames,
		SourceProductionContext context,
		ISourceGenLogger? logger,
		bool emitNullable = true
	)
	{
		logger?.Debug("Emitting state content");

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
			.WriteLine("ThreadLocalState;")
			.Write(stateVarName)
			.Write(".ReserveTagSpace(")
			.Write(reservationCount.ToString(CultureInfo.InvariantCulture))
			.WriteLine(");")
			.NewLine();

		// Original format is always at 0.
		OutputState(
			writer,
			stateVarName,
			"{OriginalFormat}".Wrap(),
			methodTarget.MessageTemplate.Wrap(),
			0,
			emitNullable: emitNullable
		);

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
			OutputState(
				writer,
				stateVarName,
				parameter.Name.Wrap(),
				parameter.Name,
				++idx,
				isEnumerable: isEnumerable,
				emitNullable: emitNullable
			);

			if (isEnumerable)
			{
				if (parameter.ExpandEnumerableAttribute != null)
				{
					postSetProperties ??= [];
					postSetProperties.Add(
						OutputExpandedEnumerable(writer, stateVarName, parameter, context, existingParamNames, logger)
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
				writer.WriteLine(nullableLogProperty);
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
				if (!(parameter.LogPropertiesAttribute!.OmitReferenceName.Value ?? false))
				{
					logPropertyName = $"{parameter.Name}.{logPropertyName}";
				}

				var shouldSkipNull =
					(parameter.LogPropertiesAttribute.SkipNullProperties.Value ?? false) && logProperty.IsNullable;
				if (shouldSkipNull)
				{
					var tmpVarName = FindUniqueName("tmp", existingParamNames);
					logPropertiesWriter
						.Write("{")
						.Write("var ")
						.Write(tmpVarName)
						.Write(" = ")
						.Write(logPropertyValue)
						.WriteLine(";")
						.Write("if (")
						.Write(tmpVarName)
						.WriteLine(" != null)")
						.Write("{");
					logPropertiesWriter.Indent();

					logPropertyValue = tmpVarName;
				}

				OutputState(logPropertiesWriter, stateVarName, logPropertyName.Wrap(), logPropertyValue, null);

				if (shouldSkipNull)
				{
					logPropertiesWriter.Unindent();
					logPropertiesWriter.Write("}").Write("}");
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
		ISourceGenLogger? logger
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();
		_ = writer;

		CodeWriter snippet = new(GenerationSettings.Create<TelemetrySourceGenerator>(), throwOnUnclosedScopes: false);
		var iteratorVarName = FindUniqueName("tmp_i", existingParamNames);
		var iteratorItemVarName = FindUniqueName("item", existingParamNames);
		snippet.Write("if (").Write(parameter.Name).WriteLine(" != null)").Write("{");
		snippet.Indent();
		snippet.Write("var ").Write(iteratorVarName).WriteLine(" = 0;");

		var maxCount =
			parameter.ExpandEnumerableAttribute!.MaximumValueCount.Value
			?? PropertyLibrary.Logging.UnboundedIEnumerableMaxCountBeforeDiagnostic;

		if (maxCount < 1)
			maxCount = 1;

		if (maxCount > PropertyLibrary.Logging.UnboundedIEnumerableMaxCountBeforeDiagnostic)
		{
			logger?.Diagnostic($"Identified {parameter.Name} that has a large unbounded ienumerable max.");
		}

		snippet
			.Write("foreach (var ")
			.Write(iteratorItemVarName)
			.Write(" in ")
			.Write(parameter.Name)
			.WriteLine(")")
			.Write("{");
		snippet.Indent();
		snippet
			.Write("if (")
			.Write(iteratorVarName)
			.Write(" == ")
			.Write(maxCount.ToString(CultureInfo.InvariantCulture))
			.WriteLine(")")
			.Write("{");

		using (snippet.OpenBlockScope())
			snippet.WriteLine("break;");

		snippet.NewLine();

		OutputState(snippet, stateVarName, $"$\"{parameter.Name}[{{{iteratorVarName}}}]\"", iteratorItemVarName, null);

		snippet.Write(iteratorVarName).WriteLine("++;");

		snippet.Write("}");
		snippet.Unindent();
		snippet.Write("}");
		snippet.Unindent();
		snippet.Write("}");

		return snippet.ToString().TrimEnd('\n');
	}

	static void EmitParametersAsMethodArgumentList(
		LogMethodTarget methodTarget,
		CodeWriter writer,
		SourceProductionContext context
	)
	{
		for (var i = 0; i < methodTarget.TotalParameterCount; i++)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			writer.Write(methodTarget.Parameters[i].ParameterType).Write(' ').Write(methodTarget.Parameters[i].Name);

			if (i < methodTarget.TotalParameterCount - 1)
				writer.Write(", ");
		}
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

	static void EmitLogStateStructs(
		LoggerTarget target,
		CodeWriter writer,
		SourceProductionContext context,
		ISourceGenLogger? _,
		bool emitNullable
	)
	{
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
				EmitScopeStateStruct(writer, methodTarget, context, emitNullable);
			else
				EmitLogStateStruct(writer, methodTarget, context, emitNullable);
		}
	}

	static void EmitLogStateStruct(
		CodeWriter writer,
		LogMethodTarget methodTarget,
		SourceProductionContext context,
		bool emitNullable
	)
	{
		var nonExceptionParams = methodTarget.ParametersSansException;
		var structName = methodTarget.MethodName + "_LogState";
		var count = nonExceptionParams.Count + 1; // +1 for {OriginalFormat}

		var kvpType = emitNullable
			? "global::System.Collections.Generic.KeyValuePair<string, object?>"
			: "global::System.Collections.Generic.KeyValuePair<string, object>";
		var iReadOnlyListType = $"global::System.Collections.Generic.IReadOnlyList<{kvpType}>";
		var ienumeratorType = $"global::System.Collections.Generic.IEnumerator<{kvpType}>";
		var ienumerableKvpType = $"global::System.Collections.Generic.IEnumerable<{kvpType}>";
		const string ienumerableType = "global::System.Collections.IEnumerator";

		writer.NewLine();

		using (
			writer.WriteStructScope(
				new TypeDeclarationOptions(structName, TypeDeclarationAccessibility.Private)
				{
					IsReadOnly = true,
					Interfaces = [new TypeReference(new TypeIdentity(iReadOnlyListType, null))],
					IncludeGeneratedAttributes = false,
				}
			)
		)
		{
			writer
				.Write("static readonly string s_originalFormat = ")
				.Write(methodTarget.MessageTemplate.Wrap())
				.WriteLine(";");

			if (nonExceptionParams.Count > 0)
			{
				writer.NewLine();

				foreach (var param in nonExceptionParams)
				{
					context.CancellationToken.ThrowIfCancellationRequested();

					writer
						.Write("public readonly ")
						.Write(param.ParameterType)
						.Write(" _")
						.Write(param.UpperCasedName)
						.WriteLine(";");
				}

				writer.NewLine().Write("public ").Write(structName).Write('(');

				for (var i = 0; i < nonExceptionParams.Count; i++)
				{
					context.CancellationToken.ThrowIfCancellationRequested();

					writer.Write(nonExceptionParams[i].ParameterType).Write(' ').Write(nonExceptionParams[i].Name);

					if (i < nonExceptionParams.Count - 1)
						writer.Write(", ");
				}

				writer.Write(")");

				using (writer.OpenBlockScope())
				{
					foreach (var param in nonExceptionParams)
					{
						context.CancellationToken.ThrowIfCancellationRequested();

						writer.Write("_").Write(param.UpperCasedName).Write(" = ").Write(param.Name).WriteLine(";");
					}
				}
			}

			writer
				.NewLine()
				.NewLine()
				.Write("public int Count => ")
				.Write(count.ToString(CultureInfo.InvariantCulture))
				.WriteLine(";")
				.NewLine()
				.Write($"public {kvpType} this[int index]");

			using (writer.OpenBlockScope())
			{
				if (emitNullable)
				{
					writer.WriteLine("get => index switch {");
					writer.Indent();
					writer.WriteLine("0 => new(\"{OriginalFormat}\", s_originalFormat),");

					for (var i = 0; i < nonExceptionParams.Count; i++)
					{
						context.CancellationToken.ThrowIfCancellationRequested();

						writer
							.Write($"{i + 1} => new(")
							.Write(nonExceptionParams[i].Name.Wrap())
							.Write(", _")
							.Write(nonExceptionParams[i].UpperCasedName)
							.WriteLine("),");
					}

					writer.WriteLine("_ => throw new global::System.IndexOutOfRangeException(nameof(index))");
					writer.Unindent();
					writer.WriteLine("};");
				}
				else
				{
					writer.Write("get");
					using (writer.OpenBlockScope())
					{
						writer.Write("switch (index)");
						using (writer.OpenBlockScope())
						{
							writer.WriteLine(
								"case 0: return new " + kvpType + "(\"{OriginalFormat}\", s_originalFormat);"
							);

							for (var i = 0; i < nonExceptionParams.Count; i++)
							{
								context.CancellationToken.ThrowIfCancellationRequested();

								writer
									.Write($"case {i + 1}: return new " + kvpType + "(")
									.Write(nonExceptionParams[i].Name.Wrap())
									.Write(", _")
									.Write(nonExceptionParams[i].UpperCasedName)
									.WriteLine(");");
							}

							writer.WriteLine(
								"default: throw new global::System.IndexOutOfRangeException(nameof(index));"
							);
						}
					}
				}
			}

			EmitStructEnumerator(
				writer,
				structName,
				kvpType,
				ienumeratorType,
				ienumerableType,
				ienumerableKvpType,
				emitNullable
			);
		}

		writer.NewLine();
	}

	static void EmitStructEnumerator(
		CodeWriter writer,
		string structName,
		string kvpType,
		string ienumeratorType,
		string ienumerableType,
		string ienumerableKvpType,
		bool emitNullable
	)
	{
		var currentPropertyType = emitNullable
			? "object? global::System.Collections.IEnumerator.Current => Current;"
			: "object global::System.Collections.IEnumerator.Current => Current;";
		writer.NewLine();

		using (
			writer.WriteStructScope(
				new TypeDeclarationOptions("Enumerator", TypeDeclarationAccessibility.Public)
				{
					IncludeGeneratedAttributes = false,
					Interfaces = [new TypeReference(new TypeIdentity(ienumeratorType, null))],
				}
			)
		)
		{
			writer
				.Write("readonly ")
				.Write(structName)
				.WriteLine(" _state;")
				.Write("int _index;")
				.NewLine()
				.Write("public Enumerator(")
				.Write(structName)
				.WriteLine(" state)");

			using (writer.OpenBlockScope())
			{
				writer.Write("_state = state;").NewLine();
				writer.Write("_index = -1;").NewLine();
			}

			writer
				.NewLine()
				.Write($"public {kvpType} Current => _state[_index];")
				.NewLine()
				.NewLine()
				.Write(currentPropertyType)
				.NewLine()
				.NewLine()
				.Write("public bool MoveNext() => ++_index < _state.Count;")
				.NewLine()
				.NewLine()
				.Write("public void Reset() => _index = -1;")
				.NewLine()
				.NewLine()
				.Write("public void Dispose() { }");
		}

		writer
			.NewLine()
			.NewLine()
			.Write("public Enumerator GetEnumerator() => new Enumerator(this);")
			.NewLine()
			.NewLine()
			.Write($"{ienumeratorType} {ienumerableKvpType}.GetEnumerator() => GetEnumerator();")
			.NewLine()
			.NewLine()
			.Write($"{ienumerableType} global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();");
	}

	static void EmitScopeStateStruct(
		CodeWriter writer,
		LogMethodTarget methodTarget,
		SourceProductionContext context,
		bool emitNullable
	)
	{
		var nonExceptionParams = methodTarget.ParametersSansException;
		var structName = methodTarget.MethodName + "_ScopeState";
		var count = nonExceptionParams.Count + 1; // +1 for {OriginalFormat}

		var kvpType = emitNullable
			? "global::System.Collections.Generic.KeyValuePair<string, object?>"
			: "global::System.Collections.Generic.KeyValuePair<string, object>";
		var iReadOnlyListType = $"global::System.Collections.Generic.IReadOnlyList<{kvpType}>";
		var ienumeratorType = $"global::System.Collections.Generic.IEnumerator<{kvpType}>";
		var ienumerableKvpType = $"global::System.Collections.Generic.IEnumerable<{kvpType}>";
		const string ienumerableType = "global::System.Collections.IEnumerator";

		writer.NewLine();

		using (
			writer.WriteStructScope(
				new TypeDeclarationOptions(structName, TypeDeclarationAccessibility.Private)
				{
					IsReadOnly = true,
					Interfaces = [new TypeReference(new TypeIdentity(iReadOnlyListType, null))],
					IncludeGeneratedAttributes = false,
				}
			)
		)
		{
			writer
				.Write("static readonly string s_originalFormat = ")
				.Write(methodTarget.MessageTemplate.Wrap())
				.WriteLine(";");

			if (nonExceptionParams.Count > 0)
			{
				writer.NewLine();

				foreach (var param in nonExceptionParams)
				{
					context.CancellationToken.ThrowIfCancellationRequested();

					writer
						.Write("public readonly ")
						.Write(param.ParameterType)
						.Write(" _")
						.Write(param.UpperCasedName)
						.WriteLine(";");
				}

				writer.NewLine().Write("public ").Write(structName).Write('(');

				for (var i = 0; i < nonExceptionParams.Count; i++)
				{
					context.CancellationToken.ThrowIfCancellationRequested();

					writer.Write(nonExceptionParams[i].ParameterType).Write(' ').Write(nonExceptionParams[i].Name);

					if (i < nonExceptionParams.Count - 1)
						writer.Write(", ");
				}

				writer.Write(")");

				using (writer.OpenBlockScope())
				{
					foreach (var param in nonExceptionParams)
					{
						context.CancellationToken.ThrowIfCancellationRequested();

						writer.Write("_").Write(param.UpperCasedName).Write(" = ").Write(param.Name).WriteLine(";");
					}
				}
			}

			// Lazy ToString() — format is deferred until a provider actually needs the string.
			var interpolatedMessage = GenerateTypedInterpolatedMessage(
				methodTarget.MessageTemplate,
				null, // null = direct field access (_FieldName instead of s._FieldName)
				null, // scoped methods have no exception parameter
				[.. methodTarget.Parameters]
			);

			writer.NewLine().NewLine().Write("public override string ToString()");

			using (writer.OpenBlockScope())
			{
				writer
					.WriteLine("#if NET")
					.Write("return string.Create(global::System.Globalization.CultureInfo.InvariantCulture, $")
					.Write(interpolatedMessage.Wrap())
					.WriteLine(");")
					.WriteLine("#else")
					.Write("return global::System.FormattableString.Invariant($")
					.Write(interpolatedMessage.Wrap())
					.WriteLine(");")
					.WriteLine("#endif");
			}

			writer
				.NewLine()
				.NewLine()
				.Write("public int Count => ")
				.Write(count.ToString(CultureInfo.InvariantCulture))
				.WriteLine(";")
				.NewLine()
				.Write($"public {kvpType} this[int index]");

			using (writer.OpenBlockScope())
			{
				if (emitNullable)
				{
					writer.WriteLine("get => index switch {");
					writer.Indent();
					writer.WriteLine("0 => new(\"{OriginalFormat}\", s_originalFormat),");

					for (var i = 0; i < nonExceptionParams.Count; i++)
					{
						context.CancellationToken.ThrowIfCancellationRequested();

						writer
							.Write($"{i + 1} => new(")
							.Write(nonExceptionParams[i].Name.Wrap())
							.Write(", _")
							.Write(nonExceptionParams[i].UpperCasedName)
							.WriteLine("),");
					}

					writer.WriteLine("_ => throw new global::System.IndexOutOfRangeException(nameof(index))");
					writer.Unindent();
					writer.WriteLine("};");
				}
				else
				{
					writer.Write("get");
					using (writer.OpenBlockScope())
					{
						writer.Write("switch (index)");
						using (writer.OpenBlockScope())
						{
							writer.WriteLine(
								"case 0: return new " + kvpType + "(\"{OriginalFormat}\", s_originalFormat);"
							);

							for (var i = 0; i < nonExceptionParams.Count; i++)
							{
								context.CancellationToken.ThrowIfCancellationRequested();

								writer
									.Write($"case {i + 1}: return new " + kvpType + "(")
									.Write(nonExceptionParams[i].Name.Wrap())
									.Write(", _")
									.Write(nonExceptionParams[i].UpperCasedName)
									.WriteLine(");");
							}

							writer.WriteLine(
								"default: throw new global::System.IndexOutOfRangeException(nameof(index));"
							);
						}
					}
				}
			}

			EmitStructEnumerator(
				writer,
				structName,
				kvpType,
				ienumeratorType,
				ienumerableType,
				ienumerableKvpType,
				emitNullable
			);
		}

		writer.NewLine();
	}

	static void EmitPublicLoggingDelegatingMethod(
		CodeWriter writer,
		LogMethodTarget methodTarget,
		SourceProductionContext context,
		ISourceGenLogger? logger,
		bool emitNullable
	)
	{
		logger?.Debug($"Building public delegating logging method: {methodTarget.MethodName}");

		writer.NewLine().Write("public ");

		// When Logging owns the public method (with Metrics), return void
		// (Logging without Activity means the return type is void or IDisposable for scoped)
		if (methodTarget.IsScoped)
		{
			writer.Write(TypeLibrary.System.IDisposable);
			if (emitNullable)
				writer.Write('?');
		}
		else
		{
			writer.Write(PropertyLibrary.System.VoidKeyword);
		}

		writer.Write(' ').Write(methodTarget.MethodName).Write('(');

		EmitParametersAsMethodArgumentList(methodTarget, writer, context);

		writer.Write(")");

		using (writer.OpenBlockScope())
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
				writer.NewLine().Write("return loggingResult;");
			}
		}

		writer.NewLine();
	}
}
