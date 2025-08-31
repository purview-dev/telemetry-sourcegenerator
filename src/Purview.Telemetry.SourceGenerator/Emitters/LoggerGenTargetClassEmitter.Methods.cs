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
		GenerationLogger? logger
	)
	{
		indent++;

		foreach (var methodTarget in target.LogMethods)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			if (!methodTarget.TargetGenerationState.IsValid)
				continue;

			EmitMethod(builder, indent, methodTarget, context, logger);
		}

		return --indent;
	}

	static void EmitMethod(
		StringBuilder builder,
		int indent,
		LogMethodTarget methodTarget,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Building logging method: {methodTarget.MethodName}");

		builder
			.AppendLine()
			.CodeGen(indent)
			.AggressiveInlining(indent)
			.Append(indent, "public ", withNewLine: false);

		if (methodTarget.IsScoped)
			builder.Append(Constants.System.IDisposable.WithNullable());
		else
			builder.Append(Constants.System.VoidKeyword);

		builder.Append(' ').Append(methodTarget.MethodName).Append('(');

		EmitParametersAsMethodArgumentList(methodTarget, builder, context);

		builder.Append(')').AppendLine().Append(indent, '{');

		indent++;

		// Output state here...then we can use it in
		// the scoped and none-scoped output.
		// If we have exceptions, output them to the state...
		// UNLESS it's NOT-scoped and then we take the FIRST
		// exception and output it as the exception parameter in
		// the Log method.

		List<string> existingParamNames = [.. methodTarget.Parameters.Select(m => m.Name)];
		var stateVarName = FindUniqueName("state", existingParamNames);

		// Should always be state, because we'll use the messageFormat. And we'll generate one if
		// one doesn't exist...
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
		EmitStateContent(
			builder,
			indent,
			methodTarget,
			stateVarName,
			existingParamNames,
			context,
			logger
		);

		if (methodTarget.IsScoped)
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
				.Append(
					indent + 1,
					"global::System.FormattableString.Invariant($",
					withNewLine: false
				)
				.Append(interpolatedMessage.Wrap())
				.AppendLine(");")
				.AppendLine("#endif")
				.Append(indent, ';')
				.AppendLine();

			OutputState(
				builder.WithIndent(indent),
				stateVarName,
				Utilities.UpperCaseFirstChar(formattedMessageVarName).Wrap(),
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
		else
		{
			var expressionStateVarName = FindUniqueName("s", existingParamNames);
			var expressionExceptionVarName =
				methodTarget.ExceptionParameter?.UsedInTemplate == true
					? FindUniqueName("e", existingParamNames)
					: null;
			var (interpolatedMessage, variables) = GenerateInterpolatedFunction(
				methodTarget.MessageTemplate,
				expressionStateVarName,
				expressionExceptionVarName,
				[.. methodTarget.Parameters],
				existingParamNames
			);

			// Call the .Log method.
			var eventId =
				methodTarget.EventId
				?? SharedHelpers.GetNonRandomizedHashCode(methodTarget.MethodName);
			builder
				.Append(indent, Constants.Logging.LoggerFieldName, withNewLine: false)
				.AppendLine(".Log(")
				// Log level
				.Append(indent + 1, methodTarget.MSLevel.WithComma(andSpace: false))
				// Event Id
				.Append(indent + 1, "new (", withNewLine: false)
				.Append(eventId)
				.Append(", nameof(")
				.Append(methodTarget.LogName)
				.AppendLine(")),")
				// State
				.Append(indent + 1, stateVarName.WithComma(andSpace: false))
				// Exception
				.Append(
					indent + 1,
					methodTarget.ExceptionParameter.OrNullKeyword().WithComma(andSpace: false)
				)
				// Message Template
				.CodeGen(indent + 1)
				.Append(indent + 1, "static string (", withNewLine: false)
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
				.Append(
					indent + 2,
					"return global::System.FormattableString.Invariant($",
					withNewLine: false
				)
				.Append(interpolatedMessage.Wrap())
				.AppendLine(");")
				.AppendLine("#endif")
				.Append(indent + 1, '}')
				.Append(indent, ");");

			builder
				.AppendLine()
				.Append(indent, stateVarName, withNewLine: false)
				.AppendLine(".Clear();");
		}

		builder.Append(--indent, '}').AppendLine();
	}

	static void EmitStateContent(
		StringBuilder builder,
		int indent,
		LogMethodTarget methodTarget,
		string stateVarName,
		List<string> existingParamNames,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		logger?.Debug("Emitting state content");

		// +1 for the OriginalFormat entry.
		var reservationCount = methodTarget.TotalParameterCount + 1;
		if (methodTarget.ExceptionParameter != null)
			reservationCount--;

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
			0
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
				isEnumerable: isEnumerable
			);

			if (isEnumerable)
			{
				if (parameter.ExpandEnumerableAttribute != null)
				{
					postSetProperties ??= [];
					postSetProperties.Add(
						OutputExpandedEnumerable(
							indent,
							stateVarName,
							parameter,
							context,
							existingParamNames,
							logger
						)
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
					(parameter.LogPropertiesAttribute.SkipNullProperties.Value ?? false)
					&& logProperty.IsNullable;
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
		bool isEnumerable = false
	)
	{
		builder.Append(stateVarName).Append('.');

		if (index.HasValue)
		{
			builder.Append("TagArray[").Append(index.Value).Append("] = new(");
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
			logger?.Diagnostic(
				$"Identified {parameter.Name} that has a large unbounded ienumerable max."
			);
			TelemetryDiagnostics.Report(
				context.ReportDiagnostic,
				TelemetryDiagnostics.Logging.UnboundedIEnumerableMaxCount,
				parameter.Locations
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

			var isUsingExpressionException =
				exceptionParameter?.ReferencedHoles.Contains(hole) == true;
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
}
