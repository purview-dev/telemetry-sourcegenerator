using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class MultiTargetClassEmitter
{
	static int EmitMethods(
		MultiTargetGenerationTarget target,
		StringBuilder builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		indent++;

		foreach (var method in target.Methods)
		{
			context.CancellationToken.ThrowIfCancellationRequested();
			EmitMethod(method, target, builder, indent, context, logger);
		}

		return --indent;
	}

	static void EmitMethod(
		MultiTargetMethod method,
		MultiTargetGenerationTarget target,
		StringBuilder builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Emitting multi-target method: {method.MethodName}");

		// Emit the public interface method that calls private target methods
		EmitPublicInterfaceMethod(method, builder, indent, context, logger);

		// Emit private methods for each enabled telemetry type
		if (method.Configuration.TargetTypes.HasFlag(GenerationType.Activities))
		{
			EmitActivityTargetMethod(method, target, builder, indent, context, logger);
		}

		if (method.Configuration.TargetTypes.HasFlag(GenerationType.Logging))
		{
			EmitLoggingTargetMethod(method, target, builder, indent, context, logger);
		}

		if (method.Configuration.TargetTypes.HasFlag(GenerationType.Metrics))
		{
			EmitMetricsTargetMethod(method, target, builder, indent, context, logger);
		}
	}

	static void EmitPublicInterfaceMethod(
		MultiTargetMethod method,
		StringBuilder builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		var returnType = method.MethodSymbol.ReturnType.ToDisplayString();
		var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.TypeName} {p.Name}"));

		builder
			.AppendLine()
			.CodeGen(indent)
			.AggressiveInlining(indent)
			.Append(indent, "public ", withNewLine: false)
			.Append(returnType)
			.Append(' ')
			.Append(method.MethodName)
			.Append('(')
			.Append(parameters)
			.AppendLine(')')
			.Append(indent, '{');

		indent++;

		// Call the appropriate private target methods
		if (method.Configuration.TargetTypes.HasFlag(GenerationType.Activities))
		{
			EmitCallToTargetMethod(method, "Activity", builder, indent);
		}

		if (method.Configuration.TargetTypes.HasFlag(GenerationType.Logging))
		{
			EmitCallToTargetMethod(method, "Logging", builder, indent);
		}

		if (method.Configuration.TargetTypes.HasFlag(GenerationType.Metrics))
		{
			EmitCallToTargetMethod(method, "Metrics", builder, indent);
		}

		// Return default if needed
		if (returnType != "void")
		{
			builder.Append(indent, "return default;");
		}

		indent--;
		builder.Append(indent, "}").AppendLine();
	}

	static void EmitCallToTargetMethod(
		MultiTargetMethod method,
		string targetType,
		StringBuilder builder,
		int indent
	)
	{
		var filteredParams = GetFilteredParametersForTarget(method, targetType);
		var paramNames = string.Join(", ", filteredParams.Select(p => p.Name));

		builder
			.Append(indent, method.MethodName, withNewLine: false)
			.Append('_')
			.Append(targetType)
			.Append('(')
			.Append(paramNames)
			.AppendLine(");");
	}

	static void EmitActivityTargetMethod(
		MultiTargetMethod method,
		MultiTargetGenerationTarget target,
		StringBuilder builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		var filteredParams = GetFilteredParametersForTarget(method, "Activity");
		var methodName = $"{method.MethodName}_Activity";

		EmitPrivateMethodSignature(methodName, filteredParams, builder, indent);

		indent++;

		// Generate activity using similar logic to existing activity emitter
		var activityName = method.Configuration.ActivityName ?? method.MethodName;

		builder
			.Append(indent, "using var activity = ", withNewLine: false)
			.Append(Constants.VariableNames.ActivitySourceFieldName)
			.Append(".StartActivity(\"")
			.Append(activityName)
			.AppendLine("\");");

		// Add tags for parameters marked as tags
		foreach (var param in filteredParams.Where(p => p.IsTag))
		{
			var tagName = param.TagName ?? param.Name.ToLowerInvariant();
			builder
				.Append(indent, "activity?.SetTag(\"", withNewLine: false)
				.Append(tagName)
				.Append("\", ")
				.Append(param.Name)
				.AppendLine(");");
		}

		// Add baggage for parameters marked as baggage
		foreach (var param in filteredParams.Where(p => p.IsBaggage))
		{
			var baggageName = param.BaggageName ?? param.Name.ToLowerInvariant();
			builder
				.Append(indent, "activity?.SetBaggage(\"", withNewLine: false)
				.Append(baggageName)
				.Append("\", ")
				.Append(param.Name)
				.AppendLine("?.ToString());");
		}

		indent--;
		builder.Append(indent, "}").AppendLine();
	}

	static void EmitLoggingTargetMethod(
		MultiTargetMethod method,
		MultiTargetGenerationTarget target,
		StringBuilder builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		var filteredParams = GetFilteredParametersForTarget(method, "Logging");
		var methodName = $"{method.MethodName}_Logging";

		EmitPrivateMethodSignature(methodName, filteredParams, builder, indent);

		indent++;

		// Generate logging using similar logic to existing logging emitter
		var logLevel = method.Configuration.LogLevel ?? "Information";
		var logMessage =
			method.Configuration.LogMessage
			?? BuildDefaultLogMessage(method.MethodName, filteredParams);

		builder
			.Append(indent, "if (!", withNewLine: false)
			.Append(Constants.VariableNames.LoggerFieldName)
			.Append(".IsEnabled(")
			.Append("global::Microsoft.Extensions.Logging.LogLevel.")
			.Append(logLevel)
			.AppendLine("))")
			.Append(indent, '{')
			.Append(indent + 1, "return;")
			.Append(indent, '}')
			.AppendLine()
			.Append(indent, Constants.VariableNames.LoggerFieldName, withNewLine: false)
			.Append('.')
			.Append(GetLogMethodName(logLevel))
			.Append("(\"")
			.Append(logMessage)
			.Append('"');

		// Add parameters for template
		foreach (var param in filteredParams)
		{
			builder.Append(", ").Append(param.Name);
		}

		builder.AppendLine(");");

		indent--;
		builder.Append(indent, "}").AppendLine();
	}

	static void EmitMetricsTargetMethod(
		MultiTargetMethod method,
		MultiTargetGenerationTarget target,
		StringBuilder builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		var filteredParams = GetFilteredParametersForTarget(method, "Metrics");
		var methodName = $"{method.MethodName}_Metrics";

		EmitPrivateMethodSignature(methodName, filteredParams, builder, indent);

		indent++;

		// Generate metrics using similar logic to existing metrics emitter
		var tagParams = filteredParams.Where(p => p.IsTag).ToArray();

		if (tagParams.Length != 0)
		{
			EmitTagsCollection(tagParams, builder, indent);
		}
		else
		{
			builder.Append(
				indent,
				"var tags = global::System.Array.Empty<global::System.Collections.Generic.KeyValuePair<string, object?>>();"
			);
		}

		builder
			.Append(
				indent,
				"// TODO: Implement actual metrics instrumentation for ",
				withNewLine: false
			)
			.AppendLine(method.MethodName)
			.Append(indent, "// Example: _someCounter.Add(1, tags);");

		indent--;
		builder.Append(indent, "}").AppendLine();
	}

	static void EmitPrivateMethodSignature(
		string methodName,
		IEnumerable<MultiTargetParameter> parameters,
		StringBuilder builder,
		int indent
	)
	{
		var paramList = string.Join(", ", parameters.Select(p => $"{p.TypeName} {p.Name}"));

		builder
			.AppendLine()
			.CodeGen(indent)
			.AggressiveInlining(indent)
			.Append(indent, "private void ", withNewLine: false)
			.Append(methodName)
			.Append('(')
			.Append(paramList)
			.AppendLine(')')
			.Append(indent, '{');
	}

	static MultiTargetParameter[] GetFilteredParametersForTarget(
		MultiTargetMethod method,
		string targetType
	)
	{
		var exclusionFlag = targetType switch
		{
			"Activity" => ParameterExclusions.Activities,
			"Logging" => ParameterExclusions.Logging,
			"Metrics" => ParameterExclusions.Metrics,
			_ => ParameterExclusions.None,
		};

		return method
			.Parameters.Where(p =>
			{
				// Apply explicit exclusions
				if (p.Exclusions.HasFlag(exclusionFlag))
					return false;

				// Apply automatic exclusions based on parameter type
				return !ShouldAutoExcludeFromTarget(p.TypeName, targetType);
			})
			.ToArray();
	}

	static bool ShouldAutoExcludeFromTarget(string typeName, string targetType)
	{
		// Automatically exclude Activity parameters from Logging and Metrics
		if (
			typeName == "System.Diagnostics.Activity"
			|| typeName == "global::System.Diagnostics.Activity"
		)
		{
			return targetType is "Logging" or "Metrics";
		}

		// Automatically exclude CancellationToken from all targets
		if (
			typeName == "System.Threading.CancellationToken"
			|| typeName == "global::System.Threading.CancellationToken"
		)
		{
			return true;
		}

		return false;
	}

	static void EmitTagsCollection(
		MultiTargetParameter[] tagParams,
		StringBuilder builder,
		int indent
	)
	{
		builder.Append(
			indent,
			"var tags = new global::System.Collections.Generic.KeyValuePair<string, object?>[] {"
		);

		for (int i = 0; i < tagParams.Length; i++)
		{
			var param = tagParams[i];
			var tagName = param.TagName ?? param.Name.ToLowerInvariant();

			if (i > 0)
				builder.Append(", ");

			builder.Append($"new(\"{tagName}\", {param.Name})");
		}

		builder.AppendLine("};");
	}

	static string BuildDefaultLogMessage(
		string methodName,
		IEnumerable<MultiTargetParameter> parameters
	)
	{
		var paramList = parameters.ToArray();
		var message = $"{methodName} called";

		if (paramList.Length > 0)
		{
			message += " with";
			for (int i = 0; i < paramList.Length; i++)
			{
				if (i > 0)
					message += ",";
				message += $" {{{paramList[i].Name}}}";
			}
		}

		return message;
	}

	static string GetLogMethodName(string logLevel)
	{
		return logLevel switch
		{
			"Trace" => "LogTrace",
			"Debug" => "LogDebug",
			"Information" => "LogInformation",
			"Warning" => "LogWarning",
			"Error" => "LogError",
			"Critical" => "LogCritical",
			_ => "LogInformation",
		};
	}
}
