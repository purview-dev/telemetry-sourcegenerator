using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class LoggerGenTargetClassEmitter
{
	static int EmitMethods(
		LoggerTarget target,
		StringBuilder builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable
	)
	{
		indent++;

		foreach (var methodTarget in target.LogMethods)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			if (!methodTarget.TargetGenerationState.IsValid)
			{
				// HasLogPropertiesAndExpandEnumerable stubs have IsValid=false; report TSG2006 here.
				if (methodTarget.HasLogPropertiesAndExpandEnumerable)
				{
					TelemetryDiagnostics.Report(
						context.ReportDiagnostic,
						TelemetryDiagnostics.Logging.ExpandEnumerableAndLogPropertiesNotSupported
					);
					LoggerTargetClassEmitter.EmitThrowStub(builder, indent, methodTarget, emitNullable);
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
					LoggerTargetClassEmitter.EmitThrowStub(builder, indent, methodTarget, emitNullable);
				}
				continue;
			}

			if (methodTarget.UnknownReturnType)
			{
				LoggerTargetClassEmitter.EmitThrowStub(builder, indent, methodTarget, emitNullable);
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
				TelemetryDiagnostics.Report(
					context.ReportDiagnostic,
					TelemetryDiagnostics.Logging.ScopedMethodShouldNotHaveLevel
				);
			}

			EmitMethod(builder, indent, methodTarget, context, logger, emitNullable);
		}

		return --indent;
	}

	static void EmitMethod(
		StringBuilder builder,
		int indent,
		LogMethodTarget methodTarget,
		SourceProductionContext context,
		GenerationLogger? logger,
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
				&& methodTarget.ParameterCountSansException <= Constants.Logging.MaxNonExceptionParameters
			)
			{
				LoggerTargetClassEmitter.EmitLogActionMethod(
					builder,
					indent,
					methodTarget,
					context,
					logger,
					emitNullable
				);
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

		var accessModifier = generatePrivateLogging ? "private" : "public";
		var methodName = generatePrivateLogging ? methodTarget.MethodName + "_Logging" : methodTarget.MethodName;

		builder
			.AppendLine()
			.CodeGen(indent)
			.AggressiveInlining(indent)
			.Append(indent, accessModifier + " ", withNewLine: false);

		if (methodTarget.IsScoped)
		{
			builder.Append(Constants.System.IDisposable);
			if (emitNullable)
				builder.Append('?');
		}
		else
			builder.Append(Constants.System.VoidKeyword);

		builder.Append(' ').Append(methodName).Append('(');

		EmitParametersAsMethodArgumentList(methodTarget, builder, context);

		builder.Append(')').AppendLine().Append(indent, '{');

		indent++;

		// Output state here...then we can use it in
		// the scoped and none-scoped output.
		// If we have exceptions, output them to the state...
		// UNLESS it's NOT-scoped and then we take the FIRST
		// exception and output it as the exception parameter in
		// the Log method.

		List<string> existingParamNames = new(methodTarget.Parameters.Length);
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
			builder
				.Append(indent, "if (!", withNewLine: false)
				.Append(Constants.Logging.LoggerFieldName)
				.Append(".IsEnabled(")
				.Append(methodTarget.MSLevel)
				.AppendLine("))")
				.Append(indent, '{')
				.Append(indent + 1, "return;")
				.Append(indent, '}')
				.AppendLine();
		}

		// Output the state here...
		if (!useTypedState && !useScopedTypedState)
		{
			EmitStateContent(
				builder,
				indent,
				methodTarget,
				stateVarName,
				existingParamNames,
				context,
				logger,
				emitNullable
			);
		}

		if (methodTarget.IsScoped)
		{
			if (useScopedTypedState)
			{
				// Zero-allocation: typed _ScopeState struct — no ThreadLocalState, no eager string formatting.
				var scopeStructName = methodTarget.MethodName + "_ScopeState";
				var scopeNonExceptionParams = methodTarget.ParametersSansException;

				builder
					.Append(indent, "return ", withNewLine: false)
					.Append(Constants.Logging.LoggerFieldName)
					.Append(".BeginScope(new ")
					.Append(scopeStructName)
					.Append('(');

				for (var i = 0; i < scopeNonExceptionParams.Length; i++)
				{
					context.CancellationToken.ThrowIfCancellationRequested();

					builder.Append(scopeNonExceptionParams[i].Name);
					if (i < scopeNonExceptionParams.Length - 1)
						builder.Append(", ");
				}

				builder.AppendLine("));");
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
						builder.Append(indent, variableDefinition);

					builder.AppendLine();
				}

				var formattedMessageVarName = FindUniqueName("formattedMessage", existingParamNames);
				builder
					.Append(indent, "var ", withNewLine: false)
					.AppendLine("formattedMessage = ")
					.AppendLine("#if NET")
					.Append(
						indent + 1,
						"string.Create(global::System.Globalization.CultureInfo.InvariantCulture, $",
						withNewLine: false
					)
					.Append(interpolatedMessage.Wrap())
					.AppendLine(");")
					.AppendLine("#else")
					.Append(indent + 1, "global::System.FormattableString.Invariant($", withNewLine: false)
					.Append(interpolatedMessage.Wrap())
					.AppendLine(");")
					.AppendLine("#endif")
					.Append(indent, ';')
					.AppendLine();

				OutputState(
					builder.WithIndent(indent),
					stateVarName,
					Utilities.UppercaseFirstChar(formattedMessageVarName).Wrap(),
					formattedMessageVarName,
					index: null
				);

				builder
					.AppendLine()
					.Append(indent, "return ", withNewLine: false)
					.Append(Constants.Logging.LoggerFieldName)
					.Append(".BeginScope(")
					.Append(stateVarName)
					.AppendLine(");");
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

				var eventId = methodTarget.EventId ?? SharedHelpers.GetNonRandomizedHashCode(methodTarget.MethodName);

				var nonExceptionParams = methodTarget.ParametersSansException;

				builder
					.Append(indent, Constants.Logging.LoggerFieldName, withNewLine: false)
					.AppendLine(".Log(")
					.Append(indent + 1, methodTarget.MSLevel.WithComma(andSpace: false))
					.Append(
						indent + 1,
						emitNullable ? "new (" : "new " + Constants.Logging.MicrosoftExtensions.EventId + "(",
						withNewLine: false
					)
					.Append(eventId)
					.Append(", nameof(")
					.Append(methodTarget.LogName)
					.AppendLine(")),")
					.Append(indent + 1, "new ", withNewLine: false)
					.Append(structName)
					.Append('(');

				for (var i = 0; i < nonExceptionParams.Length; i++)
				{
					builder.Append(nonExceptionParams[i].Name);
					if (i < nonExceptionParams.Length - 1)
						builder.Append(", ");
				}

				builder.AppendLine("),");
				builder.Append(indent + 1, methodTarget.ExceptionParameter.OrNullKeyword().WithComma(andSpace: false));

				if (emitNullable)
					builder.CodeGen(indent + 1);
				builder
					.Append(indent + 1, emitNullable ? "static string (" : "(", withNewLine: false)
					.Append(expressionStateVarName)
					.Append(", ")
					.Append(expressionExceptionVarName ?? "_")
					.AppendLine(") =>")
					.Append(indent + 1, "{")
					.AppendLine("#if NET")
					.Append(
						indent + 2,
						"return string.Create(global::System.Globalization.CultureInfo.InvariantCulture, $",
						withNewLine: false
					)
					.Append(interpolatedMessage.Wrap())
					.AppendLine(");")
					.AppendLine("#else")
					.Append(indent + 2, "return global::System.FormattableString.Invariant($", withNewLine: false)
					.Append(interpolatedMessage.Wrap())
					.AppendLine(");")
					.AppendLine("#endif")
					.Append(indent + 1, '}')
					.Append(indent, ");");

				builder.AppendLine();
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
				var eventId = methodTarget.EventId ?? SharedHelpers.GetNonRandomizedHashCode(methodTarget.MethodName);
				builder
					.Append(indent, Constants.Logging.LoggerFieldName, withNewLine: false)
					.AppendLine(".Log(")
					// Log level
					.Append(indent + 1, methodTarget.MSLevel.WithComma(andSpace: false))
					// Event Id
					.Append(
						indent + 1,
						emitNullable ? "new (" : "new " + Constants.Logging.MicrosoftExtensions.EventId + "(",
						withNewLine: false
					)
					.Append(eventId)
					.Append(", nameof(")
					.Append(methodTarget.LogName)
					.AppendLine(")),")
					// State
					.Append(indent + 1, stateVarName.WithComma(andSpace: false))
					// Exception
					.Append(indent + 1, methodTarget.ExceptionParameter.OrNullKeyword().WithComma(andSpace: false));
				// Message Template
				if (emitNullable)
					builder.CodeGen(indent + 1);
				builder
					.Append(indent + 1, emitNullable ? "static string (" : "(", withNewLine: false)
					.Append(expressionStateVarName)
					.Append(", ")
					.Append(expressionExceptionVarName ?? "_")
					.AppendLine(") =>")
					.Append(indent + 1, "{");

				if (variables.Length > 0)
				{
					foreach (var variableDefinition in variables)
						builder.Append(indent + 2, variableDefinition);

					builder.AppendLine();
				}

				builder
					.AppendLine("#if NET")
					.Append(
						indent + 2,
						"return string.Create(global::System.Globalization.CultureInfo.InvariantCulture, $",
						withNewLine: false
					)
					.Append(interpolatedMessage.Wrap())
					.AppendLine(");")
					.AppendLine("#else")
					.Append(indent + 2, "return global::System.FormattableString.Invariant($", withNewLine: false)
					.Append(interpolatedMessage.Wrap())
					.AppendLine(");")
					.AppendLine("#endif")
					.Append(indent + 1, '}')
					.Append(indent, ");");

				builder.AppendLine().Append(indent, stateVarName, withNewLine: false).AppendLine(".Clear();");
			}
		}

		builder.Append(--indent, '}').AppendLine();

		// Generate public delegating method if Logging owns it
		if (generatePublicDelegator)
		{
			EmitPublicLoggingDelegatingMethod(builder, indent, methodTarget, context, logger, emitNullable);
		}
	}

	static void EmitStateContent(
		StringBuilder builder,
		int indent,
		LogMethodTarget methodTarget,
		string stateVarName,
		List<string> existingParamNames,
		SourceProductionContext context,
		GenerationLogger? logger,
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
				reservationCount += param.LogProperties.Value.Length;
		}

		// Create the state variable,
		// and reserve the required number of variables.
		builder
			.Append(indent, "var ", withNewLine: false)
			.Append(stateVarName)
			.Append(" = ")
			.Append(Constants.Logging.MicrosoftExtensions.LoggerMessageHelper)
			.Append('.')
			.AppendLine("ThreadLocalState;")
			.Append(indent, stateVarName, withNewLine: false)
			.Append(".ReserveTagSpace(")
			.Append(reservationCount)
			.AppendLine(");")
			.AppendLine();

		// Original format is always at 0.
		OutputState(
			builder.WithIndent(indent),
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
				builder.WithIndent(indent),
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
						OutputExpandedEnumerable(indent, stateVarName, parameter, context, existingParamNames, logger)
					);
				}
			}
			else if (parameter.LogProperties != null)
			{
				OutputLogPropertyDetails(
					indent,
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
			builder.AppendLine();

			foreach (var nullableLogProperty in postSetProperties)
			{
				context.CancellationToken.ThrowIfCancellationRequested();
				builder.Append(nullableLogProperty);
			}
		}

		builder.AppendLine();

		static void OutputLogPropertyDetails(
			int indent,
			string stateVarName,
			SourceProductionContext context,
			ref List<string>? postPropertyDefinitions,
			LogParameterTarget parameter,
			List<string> existingParamNames
		)
		{
			StringBuilder logPropertiesBuilder = new();
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
					logPropertiesBuilder
						.Append(indent, '{')
						.Append(indent + 1, "var ", withNewLine: false)
						.Append(tmpVarName)
						.Append(" = ")
						.Append(logPropertyValue)
						.AppendLine(";")
						.Append(indent + 1, "if (", withNewLine: false)
						.Append(tmpVarName)
						.AppendLine(" != null)")
						.Append(indent + 1, '{');

					logPropertyValue = tmpVarName;

					indent += 2;
				}

				OutputState(
					logPropertiesBuilder.WithIndent(indent),
					stateVarName,
					logPropertyName.Wrap(),
					logPropertyValue,
					null
				);

				if (shouldSkipNull)
				{
					indent -= 2;
					logPropertiesBuilder.Append(indent + 1, '}').Append(indent, '}');
				}

				postPropertyDefinitions ??= [];
				postPropertyDefinitions.Add(logPropertiesBuilder.ToString());

				logPropertiesBuilder.Clear();
			}
		}
	}

	static void OutputState(
		StringBuilder builder,
		string stateVarName,
		string propertyName,
		string value,
		int? index,
		bool isEnumerable = false,
		bool emitNullable = true
	)
	{
		builder.Append(stateVarName).Append('.');

		if (index.HasValue)
		{
			builder
				.Append("TagArray[")
				.Append(index.Value)
				.Append("] = ")
				.Append(emitNullable ? "new(" : "new global::System.Collections.Generic.KeyValuePair<string, object>(");
		}
		else
		{
			builder.Append("AddTag(");
		}

		builder.Append(propertyName.WithComma());

		if (isEnumerable)
		{
			builder
				.Append(value)
				.Append(" == null ? null : ")
				.Append(Constants.Logging.MicrosoftExtensions.LoggerMessageHelper)
				.Append(".Stringify(")
				.Append(value)
				.Append(')');
		}
		else
		{
			builder.Append(value);
		}

		builder.AppendLine(");");
	}

	static string OutputExpandedEnumerable(
		int indent,
		string stateVarName,
		LogParameterTarget parameter,
		SourceProductionContext context,
		List<string> existingParamNames,
		GenerationLogger? logger
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		StringBuilder builder = new();
		var iteratorVarName = FindUniqueName("tmp_i", existingParamNames);
		var iteratorItemVarName = FindUniqueName("item", existingParamNames);
		builder
			.Append(indent, "if (", withNewLine: false)
			.Append(parameter.Name)
			.AppendLine(" != null)")
			.Append(indent, '{')
			.Append(++indent, "var ", withNewLine: false)
			.Append(iteratorVarName)
			.AppendLine(" = 0;");

		var maxCount =
			parameter.ExpandEnumerableAttribute!.MaximumValueCount.Value
			?? Constants.Logging.UnboundedIEnumerableMaxCountBeforeDiagnostic;

		if (maxCount < 1)
			maxCount = 1;

		if (maxCount > Constants.Logging.UnboundedIEnumerableMaxCountBeforeDiagnostic)
		{
			logger?.Diagnostic($"Identified {parameter.Name} that has a large unbounded ienumerable max.");
			TelemetryDiagnostics.Report(
				context.ReportDiagnostic,
				TelemetryDiagnostics.Logging.UnboundedIEnumerableMaxCount,
				parameter.ExpandEnumerableAttribute!.ParamLocation
			);
		}

		builder
			.Append(indent, "foreach (var ", withNewLine: false)
			.Append(iteratorItemVarName)
			.Append(" in ")
			.Append(parameter.Name)
			.AppendLine(")")
			.Append(indent, '{')
			.Append(++indent, "if (", withNewLine: false)
			.Append(iteratorVarName)
			.Append(" == ")
			.Append(maxCount)
			.AppendLine(")")
			.Append(indent, '{')
			.Append(indent + 1, "break;")
			.Append(indent, "}")
			.AppendLine();

		OutputState(
			builder.WithIndent(indent),
			stateVarName,
			$"$\"{parameter.Name}[{{{iteratorVarName}}}]\"",
			iteratorItemVarName,
			null
		);

		builder
			.Append(indent, iteratorVarName, withNewLine: false)
			.AppendLine("++;")
			.Append(--indent, '}')
			.Append(--indent, '}');

		return builder.ToString();
	}

	static void EmitParametersAsMethodArgumentList(
		LogMethodTarget methodTarget,
		StringBuilder builder,
		SourceProductionContext context
	)
	{
		for (var i = 0; i < methodTarget.TotalParameterCount; i++)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			builder
				.Append(methodTarget.Parameters[i].ParameterType)
				.Append(' ')
				.Append(methodTarget.Parameters[i].Name);

			if (i < methodTarget.TotalParameterCount - 1)
				builder.Append(", ");
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
			string replacement =
				$"{{{varName}"
				+ $"{(hole.Alignment.HasValue ? $",{hole.Alignment}" : "")}"
				+ $"{(hole.Format != null ? $":{hole.Format}" : "")}}}";

			// Replace all occurrences of this hole’s placeholders
			string placeholder = hole.IsPositional ? $"{{{hole.Ordinal}}}" : $"{{{hole.Name}}}";
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
		StringBuilder builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? _,
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
				&& methodTarget.ParameterCountSansException <= Constants.Logging.MaxNonExceptionParameters
			)
				continue;

			if (!IsSimpleLogMethod(methodTarget))
				continue;

			if (methodTarget.IsScoped)
				EmitScopeStateStruct(builder, indent, methodTarget, context, emitNullable);
			else
				EmitLogStateStruct(builder, indent, methodTarget, context, emitNullable);
		}
	}

	static void EmitLogStateStruct(
		StringBuilder builder,
		int indent,
		LogMethodTarget methodTarget,
		SourceProductionContext context,
		bool emitNullable
	)
	{
		var nonExceptionParams = methodTarget.ParametersSansException;
		var structName = methodTarget.MethodName + "_LogState";
		var count = nonExceptionParams.Length + 1; // +1 for {OriginalFormat}

		var kvpType = emitNullable
			? "global::System.Collections.Generic.KeyValuePair<string, object?>"
			: "global::System.Collections.Generic.KeyValuePair<string, object>";
		var iReadOnlyListType = $"global::System.Collections.Generic.IReadOnlyList<{kvpType}>";
		var ienumeratorType = $"global::System.Collections.Generic.IEnumerator<{kvpType}>";
		var ienumerableKvpType = $"global::System.Collections.Generic.IEnumerable<{kvpType}>";
		const string ienumerableType = "global::System.Collections.IEnumerator";

		builder
			.AppendLine()
			.CodeGen(indent)
			.Append(indent, "private readonly struct ", withNewLine: false)
			.Append(structName)
			.AppendLine($" : {iReadOnlyListType}")
			.Append(indent, '{');

		indent++;

		builder
			.Append(indent, "static readonly string s_originalFormat = ", withNewLine: false)
			.Append(methodTarget.MessageTemplate.Wrap())
			.AppendLine(";");

		if (nonExceptionParams.Length > 0)
		{
			builder.AppendLine();

			foreach (var param in nonExceptionParams)
			{
				context.CancellationToken.ThrowIfCancellationRequested();

				builder
					.Append(indent, "public readonly ", withNewLine: false)
					.Append(param.ParameterType)
					.Append(" _")
					.Append(param.UpperCasedName)
					.AppendLine(";");
			}

			builder.AppendLine().Append(indent, "public ", withNewLine: false).Append(structName).Append('(');

			for (var i = 0; i < nonExceptionParams.Length; i++)
			{
				context.CancellationToken.ThrowIfCancellationRequested();

				builder.Append(nonExceptionParams[i].ParameterType).Append(' ').Append(nonExceptionParams[i].Name);

				if (i < nonExceptionParams.Length - 1)
					builder.Append(", ");
			}

			builder.AppendLine(")").Append(indent, '{');
			indent++;

			foreach (var param in nonExceptionParams)
			{
				context.CancellationToken.ThrowIfCancellationRequested();

				builder
					.Append(indent, "_", withNewLine: false)
					.Append(param.UpperCasedName)
					.Append(" = ")
					.Append(param.Name)
					.AppendLine(";");
			}

			indent--;
			builder.Append(indent, '}');
		}

		builder
			.AppendLine()
			.AppendLine()
			.Append(indent, "public int Count => ", withNewLine: false)
			.Append(count)
			.AppendLine(";")
			.AppendLine()
			.Append(indent, $"public {kvpType} this[int index]")
			.Append(indent, '{');

		indent++;

		builder.Append(indent, Constants.System.AggressiveInlining);

		if (emitNullable)
		{
			builder.Append(indent, "get => index switch", withNewLine: false).AppendLine().Append(indent, '{');

			indent++;

			builder.Append(indent, "0 => new(\"{OriginalFormat}\", s_originalFormat),");

			for (var i = 0; i < nonExceptionParams.Length; i++)
			{
				context.CancellationToken.ThrowIfCancellationRequested();

				builder
					.Append(indent, $"{i + 1} => new(", withNewLine: false)
					.Append(nonExceptionParams[i].Name.Wrap())
					.Append(", _")
					.Append(nonExceptionParams[i].UpperCasedName)
					.AppendLine("),");
			}

			builder.Append(indent, "_ => throw new global::System.IndexOutOfRangeException(nameof(index))");

			indent--;
			builder.Append(indent, "};");

			indent--;
			builder.Append(indent, '}').AppendLine();
		}
		else
		{
			builder.Append(indent, "get").Append(indent, '{');

			indent++;

			builder.Append(indent, "switch (index)").Append(indent, '{');

			indent++;

			builder.Append(indent, "case 0: return new " + kvpType + "(\"{OriginalFormat}\", s_originalFormat);");

			for (var i = 0; i < nonExceptionParams.Length; i++)
			{
				context.CancellationToken.ThrowIfCancellationRequested();

				builder
					.Append(indent, $"case {i + 1}: return new " + kvpType + "(", withNewLine: false)
					.Append(nonExceptionParams[i].Name.Wrap())
					.Append(", _")
					.Append(nonExceptionParams[i].UpperCasedName)
					.AppendLine(");");
			}

			builder.Append(indent, "default: throw new global::System.IndexOutOfRangeException(nameof(index));");

			indent--;
			builder.Append(indent, '}');

			indent--;
			builder.Append(indent, '}').AppendLine();

			indent--;
			builder.Append(indent, '}').AppendLine();
		}

		EmitStructEnumerator(
			builder,
			ref indent,
			structName,
			kvpType,
			ienumeratorType,
			ienumerableType,
			ienumerableKvpType,
			emitNullable
		);

		indent--;
		builder.Append(indent, '}').AppendLine();
	}

	static void EmitStructEnumerator(
		StringBuilder builder,
		ref int indent,
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
		builder
			.AppendLine()
			.Append(indent, "public struct Enumerator : ", withNewLine: false)
			.AppendLine(ienumeratorType)
			.Append(indent, '{');

		indent++;

		builder
			.Append(indent, "readonly ", withNewLine: false)
			.Append(structName)
			.AppendLine(" _state;")
			.Append(indent, "int _index;")
			.AppendLine()
			.Append(indent, "public Enumerator(", withNewLine: false)
			.Append(structName)
			.AppendLine(" state)")
			.Append(indent, '{')
			.Append(indent + 1, "_state = state;")
			.Append(indent + 1, "_index = -1;")
			.Append(indent, '}')
			.AppendLine()
			.Append(indent, $"public {kvpType} Current => _state[_index];")
			.AppendLine()
			.Append(indent, currentPropertyType)
			.AppendLine()
			.Append(indent, "public bool MoveNext() => ++_index < _state.Count;")
			.AppendLine()
			.Append(indent, "public void Reset() => _index = -1;")
			.AppendLine()
			.Append(indent, "public void Dispose() { }");

		indent--;

		builder
			.Append(indent, '}')
			.AppendLine()
			.AppendLine()
			.Append(indent, "public Enumerator GetEnumerator() => new Enumerator(this);")
			.AppendLine()
			.AppendLine()
			.Append(indent, $"{ienumeratorType} {ienumerableKvpType}.GetEnumerator() => GetEnumerator();")
			.AppendLine()
			.AppendLine()
			.Append(
				indent,
				$"{ienumerableType} global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();"
			);
	}

	static void EmitScopeStateStruct(
		StringBuilder builder,
		int indent,
		LogMethodTarget methodTarget,
		SourceProductionContext context,
		bool emitNullable
	)
	{
		var nonExceptionParams = methodTarget.ParametersSansException;
		var structName = methodTarget.MethodName + "_ScopeState";
		var count = nonExceptionParams.Length + 1; // +1 for {OriginalFormat}

		var kvpType = emitNullable
			? "global::System.Collections.Generic.KeyValuePair<string, object?>"
			: "global::System.Collections.Generic.KeyValuePair<string, object>";
		var iReadOnlyListType = $"global::System.Collections.Generic.IReadOnlyList<{kvpType}>";
		var ienumeratorType = $"global::System.Collections.Generic.IEnumerator<{kvpType}>";
		var ienumerableKvpType = $"global::System.Collections.Generic.IEnumerable<{kvpType}>";
		const string ienumerableType = "global::System.Collections.IEnumerator";

		builder
			.AppendLine()
			.CodeGen(indent)
			.Append(indent, "private readonly struct ", withNewLine: false)
			.Append(structName)
			.AppendLine($" : {iReadOnlyListType}")
			.Append(indent, '{');

		indent++;

		builder
			.Append(indent, "static readonly string s_originalFormat = ", withNewLine: false)
			.Append(methodTarget.MessageTemplate.Wrap())
			.AppendLine(";");

		if (nonExceptionParams.Length > 0)
		{
			builder.AppendLine();

			foreach (var param in nonExceptionParams)
			{
				context.CancellationToken.ThrowIfCancellationRequested();

				builder
					.Append(indent, "public readonly ", withNewLine: false)
					.Append(param.ParameterType)
					.Append(" _")
					.Append(param.UpperCasedName)
					.AppendLine(";");
			}

			builder.AppendLine().Append(indent, "public ", withNewLine: false).Append(structName).Append('(');

			for (var i = 0; i < nonExceptionParams.Length; i++)
			{
				context.CancellationToken.ThrowIfCancellationRequested();

				builder.Append(nonExceptionParams[i].ParameterType).Append(' ').Append(nonExceptionParams[i].Name);

				if (i < nonExceptionParams.Length - 1)
					builder.Append(", ");
			}

			builder.AppendLine(")").Append(indent, '{');
			indent++;

			foreach (var param in nonExceptionParams)
			{
				context.CancellationToken.ThrowIfCancellationRequested();

				builder
					.Append(indent, "_", withNewLine: false)
					.Append(param.UpperCasedName)
					.Append(" = ")
					.Append(param.Name)
					.AppendLine(";");
			}

			indent--;
			builder.Append(indent, '}');
		}

		// Lazy ToString() — format is deferred until a provider actually needs the string.
		var interpolatedMessage = GenerateTypedInterpolatedMessage(
			methodTarget.MessageTemplate,
			null, // null = direct field access (_FieldName instead of s._FieldName)
			null, // scoped methods have no exception parameter
			[.. methodTarget.Parameters]
		);

		builder
			.AppendLine()
			.AppendLine()
			.CodeGen(indent)
			.Append(indent, "public override string ToString()")
			.Append(indent, '{')
			.AppendLine("#if NET")
			.Append(
				indent + 1,
				"return string.Create(global::System.Globalization.CultureInfo.InvariantCulture, $",
				withNewLine: false
			)
			.Append(interpolatedMessage.Wrap())
			.AppendLine(");")
			.AppendLine("#else")
			.Append(indent + 1, "return global::System.FormattableString.Invariant($", withNewLine: false)
			.Append(interpolatedMessage.Wrap())
			.AppendLine(");")
			.AppendLine("#endif")
			.Append(indent, '}')
			.AppendLine()
			.AppendLine()
			.Append(indent, "public int Count => ", withNewLine: false)
			.Append(count)
			.AppendLine(";")
			.AppendLine()
			.Append(indent, $"public {kvpType} this[int index]")
			.Append(indent, '{');

		indent++;

		builder.Append(indent, Constants.System.AggressiveInlining);

		if (emitNullable)
		{
			builder.Append(indent, "get => index switch", withNewLine: false).AppendLine().Append(indent, '{');

			indent++;

			builder.Append(indent, "0 => new(\"{OriginalFormat}\", s_originalFormat),");

			for (var i = 0; i < nonExceptionParams.Length; i++)
			{
				context.CancellationToken.ThrowIfCancellationRequested();

				builder
					.Append(indent, $"{i + 1} => new(", withNewLine: false)
					.Append(nonExceptionParams[i].Name.Wrap())
					.Append(", _")
					.Append(nonExceptionParams[i].UpperCasedName)
					.AppendLine("),");
			}

			builder.Append(indent, "_ => throw new global::System.IndexOutOfRangeException(nameof(index))");

			indent--;
			builder.Append(indent, "};");

			indent--;
			builder.Append(indent, '}').AppendLine();
		}
		else
		{
			builder.Append(indent, "get").Append(indent, '{');

			indent++;

			builder.Append(indent, "switch (index)").Append(indent, '{');

			indent++;

			builder.Append(indent, "case 0: return new " + kvpType + "(\"{OriginalFormat}\", s_originalFormat);");

			for (var i = 0; i < nonExceptionParams.Length; i++)
			{
				context.CancellationToken.ThrowIfCancellationRequested();

				builder
					.Append(indent, $"case {i + 1}: return new " + kvpType + "(", withNewLine: false)
					.Append(nonExceptionParams[i].Name.Wrap())
					.Append(", _")
					.Append(nonExceptionParams[i].UpperCasedName)
					.AppendLine(");");
			}

			builder.Append(indent, "default: throw new global::System.IndexOutOfRangeException(nameof(index));");

			indent--;
			builder.Append(indent, '}');

			indent--;
			builder.Append(indent, '}').AppendLine();

			indent--;
			builder.Append(indent, '}').AppendLine();
		}

		EmitStructEnumerator(
			builder,
			ref indent,
			structName,
			kvpType,
			ienumeratorType,
			ienumerableType,
			ienumerableKvpType,
			emitNullable
		);

		indent--;
		builder.Append(indent, '}').AppendLine();
	}

	static void EmitPublicLoggingDelegatingMethod(
		StringBuilder builder,
		int indent,
		LogMethodTarget methodTarget,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable
	)
	{
		logger?.Debug($"Building public delegating logging method: {methodTarget.MethodName}");

		builder.AppendLine().CodeGen(indent).AggressiveInlining(indent).Append(indent, "public ", withNewLine: false);

		// When Logging owns the public method (with Metrics), return void
		// (Logging without Activity means the return type is void or IDisposable for scoped)
		if (methodTarget.IsScoped)
		{
			builder.Append(Constants.System.IDisposable);
			if (emitNullable)
				builder.Append('?');
		}
		else
		{
			builder.Append(Constants.System.VoidKeyword);
		}

		builder.Append(' ').Append(methodTarget.MethodName).Append('(');

		EmitParametersAsMethodArgumentList(methodTarget, builder, context);

		builder.Append(')').AppendLine().Append(indent, '{');

		// Call the private Logging method
		if (methodTarget.IsScoped)
		{
			builder.Append(indent + 1, "var loggingResult = ", withNewLine: false);
		}
		else
		{
			builder.Append(indent + 1, methodTarget.MethodName, withNewLine: false).Append("_Logging(");
		}

		if (!methodTarget.IsScoped)
		{
			// Emit parameters
			for (var i = 0; i < methodTarget.TotalParameterCount; i++)
			{
				context.CancellationToken.ThrowIfCancellationRequested();

				builder.Append(methodTarget.Parameters[i].Name);

				if (i < methodTarget.TotalParameterCount - 1)
					builder.Append(", ");
			}
			builder.AppendLine(");");
		}
		else
		{
			// For scoped, we need special handling
			builder.Append(methodTarget.MethodName).Append("_Logging(");
			for (var i = 0; i < methodTarget.TotalParameterCount; i++)
			{
				context.CancellationToken.ThrowIfCancellationRequested();

				builder.Append(methodTarget.Parameters[i].Name);

				if (i < methodTarget.TotalParameterCount - 1)
					builder.Append(", ");
			}
			builder.AppendLine(");");
		}

		// Call the private Metrics method
		builder.Append(indent + 1, methodTarget.MethodName, withNewLine: false).Append("_Metrics(");

		for (var i = 0; i < methodTarget.TotalParameterCount; i++)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			builder.Append(methodTarget.Parameters[i].Name);

			if (i < methodTarget.TotalParameterCount - 1)
				builder.Append(", ");
		}
		builder.AppendLine(");");

		// Return if scoped
		if (methodTarget.IsScoped)
		{
			builder.AppendLine().Append(indent + 1, "return loggingResult;");
		}

		builder.Append(indent, '}').AppendLine();
	}
}
