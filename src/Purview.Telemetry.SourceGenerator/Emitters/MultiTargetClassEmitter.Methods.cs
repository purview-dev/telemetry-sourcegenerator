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

		// Determine the return type based on enabled telemetry types
		var returnType = MultiTargetGenerationTarget.DetermineReturnType(
			method.Configuration, 
			method.MethodSymbol.ReturnType
		);

		// Emit the public interface method
		EmitPublicInterfaceMethod(method, target, returnType, builder, indent, context, logger);

		// Emit private methods for each enabled telemetry type, reusing existing infrastructure
		if (method.Configuration.TargetTypes.HasFlag(GenerationType.Activities))
		{
			EmitActivityTargetMethodUsingExistingInfrastructure(method, target, builder, indent, context, logger);
		}

		if (method.Configuration.TargetTypes.HasFlag(GenerationType.Logging))
		{
			EmitLoggingTargetMethodUsingExistingInfrastructure(method, target, builder, indent, context, logger);
		}

		if (method.Configuration.TargetTypes.HasFlag(GenerationType.Metrics))
		{
			EmitMetricsTargetMethodUsingExistingInfrastructure(method, target, builder, indent, context, logger);
		}
	}

	static void EmitPublicInterfaceMethod(
		MultiTargetMethod method,
		MultiTargetGenerationTarget target,
		string returnType,
		StringBuilder builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
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

		// Handle different return type scenarios
		if (returnType == "MultiTargetTelemetryResult")
		{
			EmitCombinedReturnTypeLogic(method, builder, indent);
		}
		else if (returnType == "global::System.Diagnostics.Activity?")
		{
			EmitActivityOnlyReturnLogic(method, builder, indent);
		}
		else if (returnType == "global::System.IDisposable?")
		{
			EmitScopedLoggingOnlyReturnLogic(method, builder, indent);
		}
		else
		{
			EmitVoidReturnLogic(method, builder, indent);
		}

		indent--;
		builder.Append(indent, "}").AppendLine();
	}

	static void EmitCombinedReturnTypeLogic(
		MultiTargetMethod method,
		StringBuilder builder,
		int indent
	)
	{
		// For combined return types, we need to coordinate both Activity and Scoped Logging
		var activityParams = GetFilteredParametersForTarget(method, "Activity");
		var loggingParams = GetFilteredParametersForTarget(method, "Logging");

		var activityParamNames = string.Join(", ", activityParams.Select(p => p.Name));
		var loggingParamNames = string.Join(", ", loggingParams.Select(p => p.Name));

		builder
			.Append(indent, "var activity = ", withNewLine: false)
			.Append(method.MethodName)
			.Append("_Activity(")
			.Append(activityParamNames)
			.AppendLine(");")
			.Append(indent, "var scope = ", withNewLine: false)
			.Append(method.MethodName)
			.Append("_Logging(")
			.Append(loggingParamNames)
			.AppendLine(");")
			.Append(indent, "return new MultiTargetTelemetryResult(activity, scope);");
	}

	static void EmitActivityOnlyReturnLogic(
		MultiTargetMethod method,
		StringBuilder builder,
		int indent
	)
	{
		var activityParams = GetFilteredParametersForTarget(method, "Activity");
		var paramNames = string.Join(", ", activityParams.Select(p => p.Name));

		builder
			.Append(indent, "return ", withNewLine: false)
			.Append(method.MethodName)
			.Append("_Activity(")
			.Append(paramNames)
			.AppendLine(");");

		// Call other non-returning targets
		if (method.Configuration.TargetTypes.HasFlag(GenerationType.Logging))
		{
			var loggingParams = GetFilteredParametersForTarget(method, "Logging");
			var loggingParamNames = string.Join(", ", loggingParams.Select(p => p.Name));
			builder
				.Append(indent, method.MethodName, withNewLine: false)
				.Append("_Logging(")
				.Append(loggingParamNames)
				.AppendLine(");");
		}

		if (method.Configuration.TargetTypes.HasFlag(GenerationType.Metrics))
		{
			var metricsParams = GetFilteredParametersForTarget(method, "Metrics");
			var metricsParamNames = string.Join(", ", metricsParams.Select(p => p.Name));
			builder
				.Append(indent, method.MethodName, withNewLine: false)
				.Append("_Metrics(")
				.Append(metricsParamNames)
				.AppendLine(");");
		}
	}

	static void EmitScopedLoggingOnlyReturnLogic(
		MultiTargetMethod method,
		StringBuilder builder,
		int indent
	)
	{
		var loggingParams = GetFilteredParametersForTarget(method, "Logging");
		var paramNames = string.Join(", ", loggingParams.Select(p => p.Name));

		builder
			.Append(indent, "return ", withNewLine: false)
			.Append(method.MethodName)
			.Append("_Logging(")
			.Append(paramNames)
			.AppendLine(");");

		// Call other non-returning targets
		if (method.Configuration.TargetTypes.HasFlag(GenerationType.Activities))
		{
			var activityParams = GetFilteredParametersForTarget(method, "Activity");
			var activityParamNames = string.Join(", ", activityParams.Select(p => p.Name));
			builder
				.Append(indent, method.MethodName, withNewLine: false)
				.Append("_Activity(")
				.Append(activityParamNames)
				.AppendLine(");");
		}

		if (method.Configuration.TargetTypes.HasFlag(GenerationType.Metrics))
		{
			var metricsParams = GetFilteredParametersForTarget(method, "Metrics");
			var metricsParamNames = string.Join(", ", metricsParams.Select(p => p.Name));
			builder
				.Append(indent, method.MethodName, withNewLine: false)
				.Append("_Metrics(")
				.Append(metricsParamNames)
				.AppendLine(");");
		}
	}

	static void EmitVoidReturnLogic(
		MultiTargetMethod method,
		StringBuilder builder,
		int indent
	)
	{
		// For void returns, call all enabled targets
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
}

	static void EmitActivityTargetMethodUsingExistingInfrastructure(
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
		var returnType = method.Configuration.ActivityMethodType == ActivityMethodType.Activity 
			? "global::System.Diagnostics.Activity?" 
			: "void";

		EmitPrivateMethodSignature(methodName, filteredParams, returnType, builder, indent);
		indent++;

		// Convert multi-target configuration to ActivityBasedGenerationTarget format
		var activityTarget = CreateActivityTargetFromMultiTarget(method, filteredParams);
		
		// Use existing activity generation logic
		if (method.Configuration.ActivityMethodType == ActivityMethodType.Activity)
		{
			// Use existing ActivitySourceTargetClassEmitter.EmitActivityMethodBody logic
			EmitActivityMethodBodyFromExistingInfrastructure(builder, indent, activityTarget, context, logger);
		}
		else if (method.Configuration.ActivityMethodType == ActivityMethodType.Event)
		{
			// Use existing event generation logic
			EmitEventMethodBodyFromExistingInfrastructure(builder, indent, activityTarget, context, logger);
		}
		else if (method.Configuration.ActivityMethodType == ActivityMethodType.Context)
		{
			// Use existing context generation logic
			EmitContextMethodBodyFromExistingInfrastructure(builder, indent, activityTarget, context, logger);
		}

		indent--;
		builder.Append(indent, "}").AppendLine();
	}

	static void EmitLoggingTargetMethodUsingExistingInfrastructure(
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
		var returnType = method.Configuration.UsesScopedLogging 
			? "global::System.IDisposable?" 
			: "void";

		EmitPrivateMethodSignature(methodName, filteredParams, returnType, builder, indent);
		indent++;

		// Convert multi-target configuration to LogMethodTarget format
		var logTarget = CreateLogTargetFromMultiTarget(method, filteredParams);
		
		// Use existing LoggerTargetClassEmitter.EmitLogActionMethod logic
		EmitLogActionMethodFromExistingInfrastructure(builder, indent, logTarget, context, logger);

		indent--;
		builder.Append(indent, "}").AppendLine();
	}

	static void EmitMetricsTargetMethodUsingExistingInfrastructure(
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

		EmitPrivateMethodSignature(methodName, filteredParams, "void", builder, indent);
		indent++;

		// Convert multi-target configuration to metrics target format
		var metricsTarget = CreateMetricsTargetFromMultiTarget(method, filteredParams);
		
		// Use existing MeterTargetClassEmitter logic
		EmitMetricsMethodFromExistingInfrastructure(builder, indent, metricsTarget, context, logger);

		indent--;
		builder.Append(indent, "}").AppendLine();
	}

	static void EmitPrivateMethodSignature(
		string methodName,
		IEnumerable<MultiTargetParameter> parameters,
		string returnType,
		StringBuilder builder,
		int indent
	)
	{
		var paramList = string.Join(", ", parameters.Select(p => $"{p.TypeName} {p.Name}"));

		builder
			.AppendLine()
			.CodeGen(indent)
			.AggressiveInlining(indent)
			.Append(indent, "private ", withNewLine: false)
			.Append(returnType)
			.Append(' ')
			.Append(methodName)
			.Append('(')
			.Append(paramList)
			.AppendLine(')')
			.Append(indent, '{');
	}

	// Helper methods to create target records from multi-target configuration
	// These will convert the multi-target format to the existing single-target formats
	// so we can reuse the existing generation logic

	static ActivityBasedGenerationTarget CreateActivityTargetFromMultiTarget(
		MultiTargetMethod method,
		MultiTargetParameter[] filteredParams
	)
	{
		// Convert MultiTargetMethod + filtered parameters to ActivityBasedGenerationTarget
		// This allows us to reuse existing activity generation logic
		
		// For now, return a simplified version - this will be expanded to fully map all properties
		return new ActivityBasedGenerationTarget(
			MethodName: method.MethodName,
			ReturnType: method.Configuration.ActivityMethodType == ActivityMethodType.Activity 
				? Constants.Activities.SystemDiagnostics.Activity 
				: PurviewTypeFactory.Void,
			ActivityOrEventName: method.Configuration.ActivityName ?? method.MethodName,
			HasActivityParameter: filteredParams.Any(p => p.IsActivity),
			Locations: [method.Location],
			ActivityAttribute: CreateActivityAttributeRecord(method.Configuration),
			EventAttribute: method.Configuration.ActivityMethodType == ActivityMethodType.Event 
				? CreateEventAttributeRecord(method.Configuration) 
				: null,
			MethodType: method.Configuration.ActivityMethodType,
			Parameters: ConvertToActivityParameters(filteredParams),
			Baggage: filteredParams.Where(p => p.IsBaggage).Select(ConvertToActivityParameter).ToImmutableArray(),
			Tags: filteredParams.Where(p => p.IsTag).Select(ConvertToActivityParameter).ToImmutableArray(),
			TargetGenerationState: new TargetGeneration(IsValid: true, false, false)
		);
	}

	static LogMethodTarget CreateLogTargetFromMultiTarget(
		MultiTargetMethod method,
		MultiTargetParameter[] filteredParams
	)
	{
		// Convert MultiTargetMethod + filtered parameters to LogMethodTarget
		// This allows us to reuse existing logging generation logic
		
		return new LogMethodTarget(
			MethodName: method.MethodName,
			IsScoped: method.Configuration.UsesScopedLogging,
			LoggerActionFieldName: $"_log{method.MethodName}Action",
			UnknownReturnType: false,
			LogName: method.Configuration.LogName ?? method.MethodName,
			EventId: method.Configuration.LogEventId,
			MessageTemplate: method.Configuration.LogMessage ?? BuildDefaultLogMessage(method.MethodName, filteredParams),
			TemplateProperties: [], // Will be calculated from message template
			TemplateIsOrdinalBased: false,
			TemplateIsNamedBased: true,
			MSLevel: $"global::Microsoft.Extensions.Logging.LogLevel.{method.Configuration.LogLevel ?? "Information"}",
			Parameters: ConvertToLogParameters(filteredParams),
			ParametersSansException: ConvertToLogParameters(filteredParams.Where(p => !p.IsException).ToArray()),
			ExceptionParameter: filteredParams.Where(p => p.IsException).Select(ConvertToLogParameter).FirstOrDefault(),
			HasMultipleExceptions: filteredParams.Count(p => p.IsException) > 1,
			MethodLocation: method.Location,
			InferredErrorLevel: false,
			TargetGenerationState: new TargetGeneration(IsValid: true, false, false)
		);
	}

	// Placeholder methods for the existing infrastructure calls
	// These will be implemented to call the actual existing emitter methods

	static void EmitActivityMethodBodyFromExistingInfrastructure(
		StringBuilder builder,
		int indent,
		ActivityBasedGenerationTarget methodTarget,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		// TODO: Call existing ActivitySourceTargetClassEmitter.EmitActivityMethodBody
		// For now, emit a placeholder that will be replaced
		builder.Append(indent, "// TODO: Implement activity generation using existing infrastructure");
	}

	static void EmitEventMethodBodyFromExistingInfrastructure(
		StringBuilder builder,
		int indent,
		ActivityBasedGenerationTarget methodTarget,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		// TODO: Call existing ActivitySourceTargetClassEmitter.EmitEventMethodBody
		builder.Append(indent, "// TODO: Implement event generation using existing infrastructure");
	}

	static void EmitContextMethodBodyFromExistingInfrastructure(
		StringBuilder builder,
		int indent,
		ActivityBasedGenerationTarget methodTarget,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		// TODO: Call existing ActivitySourceTargetClassEmitter.EmitContextMethodBody
		builder.Append(indent, "// TODO: Implement context generation using existing infrastructure");
	}

	static void EmitLogActionMethodFromExistingInfrastructure(
		StringBuilder builder,
		int indent,
		LogMethodTarget logTarget,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		// TODO: Call existing LoggerTargetClassEmitter.EmitLogActionMethod
		builder.Append(indent, "// TODO: Implement logging generation using existing infrastructure");
	}

	static void EmitMetricsMethodFromExistingInfrastructure(
		StringBuilder builder,
		int indent,
		object metricsTarget,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		// TODO: Call existing MeterTargetClassEmitter logic
		builder.Append(indent, "// TODO: Implement metrics generation using existing infrastructure");
	}

	// Helper conversion methods
	static ActivityAttributeRecord? CreateActivityAttributeRecord(MultiTargetConfiguration config)
	{
		if (!config.TargetTypes.HasFlag(GenerationType.Activities))
			return null;
			
		// Convert multi-target config to ActivityAttributeRecord format
		// This is a simplified version - expand as needed
		return null; // Placeholder
	}

	static EventAttributeRecord? CreateEventAttributeRecord(MultiTargetConfiguration config)
	{
		if (config.ActivityMethodType != ActivityMethodType.Event)
			return null;
			
		// Convert multi-target config to EventAttributeRecord format  
		return null; // Placeholder
	}

	static ImmutableArray<ActivityBasedParameterTarget> ConvertToActivityParameters(
		MultiTargetParameter[] parameters
	)
	{
		return parameters.Select(ConvertToActivityParameter).ToImmutableArray();
	}

	static ActivityBasedParameterTarget ConvertToActivityParameter(MultiTargetParameter param)
	{
		// Convert MultiTargetParameter to ActivityBasedParameterTarget format
		return new ActivityBasedParameterTarget(
			ParameterName: param.Name,
			ParameterType: PurviewTypeFactory.Create(param.ParameterSymbol.Type),
			GeneratedName: param.TagName ?? param.BaggageName ?? param.Name,
			ParamDestination: param.IsTag ? ActivityParameterDestination.Tag : 
			                 param.IsBaggage ? ActivityParameterDestination.Baggage :
			                 param.IsActivity ? ActivityParameterDestination.Activity :
			                 ActivityParameterDestination.Tag, // Default
			SkipOnNullOrEmpty: false, // Could be enhanced to read from attributes
			IsException: param.IsException,
			Locations: [param.ParameterSymbol.Locations.FirstOrDefault() ?? Location.None]
		);
	}

	static ImmutableArray<LogParameterTarget> ConvertToLogParameters(
		MultiTargetParameter[] parameters
	)
	{
		return parameters.Select(ConvertToLogParameter).ToImmutableArray();
	}

	static LogParameterTarget ConvertToLogParameter(MultiTargetParameter param)
	{
		// Convert MultiTargetParameter to LogParameterTarget format
		return new LogParameterTarget(
			Name: param.Name,
			UpperCasedName: param.Name.ToUpperInvariant(),
			ParameterType: PurviewTypeFactory.Create(param.ParameterSymbol.Type),
			IsException: param.IsException,
			IsFirstException: param.IsException, // Simplified - could be enhanced
			IsIEnumerable: false, // Could be enhanced to detect enumerable types
			IsArray: false, // Could be enhanced to detect array types
			IsComplexType: false, // Could be enhanced to detect complex types
			Locations: [param.ParameterSymbol.Locations.FirstOrDefault() ?? Location.None],
			LogPropertiesAttribute: null, // Could be enhanced to read log properties attributes
			LogProperties: null, // Could be enhanced to read log properties
			ExpandEnumerableAttribute: null // Could be enhanced to read expand attributes
		);
	}

	static object CreateMetricsTargetFromMultiTarget(
		MultiTargetMethod method,
		MultiTargetParameter[] filteredParams
	)
	{
		// TODO: Create appropriate metrics target record
		// The exact type will depend on the metric type (Counter, Histogram, etc.)
		return new { }; // Placeholder
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
}

