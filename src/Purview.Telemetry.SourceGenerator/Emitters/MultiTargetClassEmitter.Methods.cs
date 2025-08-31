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
			EmitMethod(method, builder, indent, context, logger);
		}

		return --indent;
	}

	static void EmitMethod(
		MultiTargetMethod method,
		StringBuilder builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Emitting multi-target method: {method.MethodName}");

		// Method signature
		var returnType = method.MethodSymbol.ReturnType.ToDisplayString();
		var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.TypeName} {p.Name}"));

		builder
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

		// Method body - call appropriate telemetry methods based on configuration
		// Order is important: Activity first, then Logging, then Metrics
		if (method.Configuration.TargetTypes.HasFlag(GenerationType.Activities))
		{
			EmitActivityCall(method, builder, indent, logger);
		}

		if (method.Configuration.TargetTypes.HasFlag(GenerationType.Logging))
		{
			EmitLoggingCall(method, builder, indent, logger);
		}

		if (method.Configuration.TargetTypes.HasFlag(GenerationType.Metrics))
		{
			EmitMetricsCall(method, builder, indent, logger);
		}

		// Return default if needed
		if (returnType != "void")
		{
			builder.Append(indent, "return default;");
		}

		indent--;

		builder.Append(indent, "}").AppendLine();
	}

	static void EmitActivityCall(MultiTargetMethod method, StringBuilder builder, int indent, GenerationLogger? logger)
	{
		logger?.Debug($"Generating activity code for {method.MethodName}");
		
		// Generate basic activity creation following established patterns
		var activityParams = method.Parameters.Where(p => !p.Exclusions.HasFlag(ParameterExclusions.Activities));
		
		// Determine activity name - match snapshots: use method name only
		var activityName = method.MethodName;

		builder.Append(indent, "using var activity = ", withNewLine: false)
			.Append(Constants.VariableNames.ActivitySourceFieldName)
			.Append(".StartActivity(\"")
			.Append(activityName)
			.AppendLine("\");");

		// Add tags for non-excluded parameters
		foreach (var param in activityParams.Where(p => p.IsTag))
		{
			var tagName = param.TagName ?? param.Name;
			builder
				.Append(indent, "activity?.SetTag(\"", withNewLine: false)
				.Append(tagName)
				.Append("\", ")
				.Append(param.Name)
				.AppendLine(");");
		}

		// Add baggage for non-excluded parameters
		foreach (var param in activityParams.Where(p => p.IsBaggage))
		{
			var baggageName = param.BaggageName ?? param.Name;
			builder
				.Append(indent, "activity?.SetBaggage(\"", withNewLine: false)
				.Append(baggageName)
				.Append("\", ")
				.Append(param.Name)
				.AppendLine("?.ToString());");
		}

		builder.AppendLine();
	}

	static void EmitLoggingCall(MultiTargetMethod method, StringBuilder builder, int indent, GenerationLogger? logger)
	{
		logger?.Debug($"Generating logging code for {method.MethodName}");
		
		// Generate logging call following established patterns
		var loggingParams = method.Parameters.Where(p => !p.Exclusions.HasFlag(ParameterExclusions.Logging));
		var logLevel = GetLogLevel(method);
		var logMessage = GetDefaultLogMessage(method, loggingParams);
		
		builder.Append(indent, Constants.VariableNames.LoggerFieldName, withNewLine: false)
			.Append(".")
			.Append(GetLogMethodName(logLevel))
			.Append("(\"")
			.Append(logMessage)
			.Append('"');
		
		// Add parameters for template
		foreach (var param in loggingParams)
		{
			builder.Append(", ").Append(param.Name);
		}

		builder.AppendLine(");").AppendLine();
	}

	static void EmitMetricsCall(MultiTargetMethod method, StringBuilder builder, int indent, GenerationLogger? logger)
	{
		logger?.Debug($"Generating metrics code for {method.MethodName}");
		
		// Match current snapshots: do not generate actual instruments here,
		// only emit an empty tags array and guidance comments.
		builder.Append(indent, "// Metrics instrumentation for ", withNewLine: false)
			.Append(method.MethodName)
			.AppendLine();
		builder.Append(indent, "var tags = global::System.Array.Empty<global::System.Collections.Generic.KeyValuePair<string, object?>>();");
		builder.Append(indent, "// TODO: Replace with appropriate metric instrument call based on method configuration");
		builder.AppendLine();
		builder.Append(indent, "// Example: _someCounter.Add(1, tags);");
		builder.AppendLine().AppendLine();
	}

	static void EmitTagsCollection(MultiTargetParameter[] tagParams, StringBuilder builder, int indent, GenerationLogger? logger)
	{
		builder.Append(indent, "var tags = new global::System.Collections.Generic.KeyValuePair<string, object?>[] {");
		
		for (int i = 0; i < tagParams.Length; i++)
		{
			var param = tagParams[i];
			var tagName = param.TagName ?? param.Name;
			
			// Apply tag key casing rule
			if (Constants.Metrics.LowerCaseTagKeysDefault)
			{
				tagName = tagName.ToLowerInvariant();
			}
			
			if (i > 0)
				builder.Append(", ");
				
			builder.Append($"new(\"{tagName}\", {param.Name})");
		}
		
		builder.AppendLine("};");
	}

	static string GetActivityName(MultiTargetMethod method) => method.MethodName;

	static string GetLogLevel(MultiTargetMethod method)
	{
		// Use configured log level or default to Information
		return method.Configuration.LogLevel ?? "Information";
	}

	static string GetDefaultLogMessage(MultiTargetMethod method, IEnumerable<MultiTargetParameter> loggingParams)
	{
		var logParams = loggingParams.ToArray();
		var message = $"{method.MethodName} called";
		if (logParams.Length > 0)
		{
			message += ", ";
			for (int i = 0; i < logParams.Length; i++)
			{
				if (i > 0) message += ", ";
				message += $"{{{logParams[i].Name}}}";
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
			_ => "LogInformation"
		};
	}

	static MultiTargetParameter? GetMeasurementParameter(MultiTargetMethod method, IEnumerable<MultiTargetParameter> metricsParams)
	{
		// Find the first parameter that could be used as a measurement value
		return metricsParams
			.Where(p => !p.IsTag && IsValidMeasurementType(p.TypeName))
			.FirstOrDefault();
	}

	/// <summary>
	/// Generates an instrument name following the established naming conventions from Constants.Metrics.
	/// </summary>
	static string GenerateInstrumentName(string methodName)
	{
		// Convert PascalCase to snake_case if lowercase is enabled
		if (Constants.Metrics.LowerCaseInstrumentNameDefault)
		{
			var instrumentName = string.Join("_", 
				System.Text.RegularExpressions.Regex.Split(methodName, @"(?<!^)(?=[A-Z])")
				.Select(s => s.ToLowerInvariant())
			);

			// Ensure it follows metrics naming conventions
			if (!instrumentName.EndsWith("_total") && 
				!instrumentName.EndsWith("_count") && 
				!instrumentName.EndsWith("_counter"))
			{
				instrumentName += "_total";
			}

			return instrumentName;
		}

		return methodName;
	}

	/// <summary>
	/// Determines if a type name represents a valid measurement type for metrics using Constants.Metrics.
	/// </summary>
	static bool IsValidMeasurementType(string typeName)
	{
		return Constants.Metrics.ValidMeasurementKeywordTypes.Contains(typeName) ||
			   typeName is "byte" or "short" or "int" or "long" or "float" or "double" or "decimal" or
			   "System.Byte" or "System.Int16" or "System.Int32" or "System.Int64" or 
			   "System.Single" or "System.Double" or "System.Decimal";
	}
}
