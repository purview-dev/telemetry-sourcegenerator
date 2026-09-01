using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

/// <summary>
/// Builds the <see cref="LogMethodTarget"/> models for a logger interface.
/// </summary>
static class LogMethodModelBuilder
{
	static readonly string[] SuffixesToRemove = ["Logs", "Logger", "Telemetry"];

	public static ImmutableArray<LogMethodTarget> BuildLogMethods(
		GenerationType generationType,
		string className,
		int defaultLogLevel,
		int defaultPrefixType,
		LoggerAttributeData loggerTarget,
		Compilation compilation,
		INamedTypeSymbol interfaceSymbol,
		ISourceGenLogger? logger,
		int interfaceGenerationMode,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		List<LogMethodTarget> methodTargets = [];
		foreach (var method in PipelineHelpers.GetAllInterfaceMethods(interfaceSymbol, token))
		{
			if (Utilities.ContainsAttribute(method, TemplateLibrary.Shared.ExcludeAttribute, token))
			{
				logger?.Debug($"Skipping {interfaceSymbol.Name}.{method.Name}, explicitly excluded.");
				continue;
			}

			// For multi-target interfaces (generationType != GenerationType.Logging means interface has multiple targets):
			// - Include method ONLY if it has an explicit Logging attribute
			// - Methods with only Activity/Metrics attributes should be skipped
			if (generationType != GenerationType.Logging)
			{
				// Check if this method has an explicit logging attribute
				var hasLoggingAttribute = SharedHelpers.GetLogAttribute(method, token) != null;

				if (!hasLoggingAttribute)
				{
					logger?.Debug(
						$"Skipping {interfaceSymbol.Name}.{method.Name} from logging - no explicit Logging attribute on multi-target interface."
					);
					continue;
				}
			}

			if (method.Arity > 0)
			{
				continue;
			}

			logger?.Debug($"Found method {interfaceSymbol.Name}.{method.Name}.");

			// Validate return type - don't skip; let through with UnknownReturnType flag so the emitter can report the diagnostic
			var invalidReturnType = TelemetryRules.IsInvalidLogReturnType(method, token);

			var isScoped = TypeLibrary.System.IDisposable.Equals(method.ReturnType);
			var methodParameters = GetLogMethodParameters(
				method,
				compilation,
				logger,
				token,
				out var hasParameterError
			);
			if (hasParameterError)
			{
				// LogProperties + ExpandEnumerable conflict: add invalid stub so emitter can report TSG2006
				methodTargets.Add(BuildErrorStubTarget(method, isScoped, invalidReturnType, defaultLogLevel));
				continue;
			}

			var logAttribute = SharedHelpers.GetLogAttribute(method, token);
			var hasExplicitLevel = logAttribute?.LevelOrNull != null;

			var isKnownReturnType = !invalidReturnType;
			var loggerActionFieldName = $"_{Utilities.LowercaseFirstChar(method.Name)}Action";

			var (logName, messageTemplate, hasMultipleExceptions, exceptionParam, inferredErrorLevel, level) =
				ResolveLogMethodDetails(
					interfaceSymbol.Name,
					className,
					loggerTarget,
					logAttribute,
					method.Name,
					defaultPrefixType,
					defaultLogLevel,
					isScoped,
					methodParameters
				);

			var messageTemplateMatches = MessageTemplateHole.FromMatches(
				PropertyLibrary.MessageTemplateMatcher.Matches(messageTemplate)
			);

			var (
				templateValid,
				templateIsOrdinalBased,
				templateIsNamedBased,
				templateParameters,
				templateExceptionParam
			) = ProcessMessageTemplate(messageTemplateMatches, methodParameters, exceptionParam);
			if (!templateValid)
				continue;

			methodParameters = templateParameters;
			exceptionParam = templateExceptionParam;

			var targetGenerationState = Utilities.IsValidGenerationTarget(
				method,
				generationType,
				GenerationType.Logging
			);
			if (targetGenerationState.RaiseMissingInterfaceSource)
			{
				logger?.Debug(
					$"Identified {interfaceSymbol.Name}.{method.Name} as problematic as the interface is missing source attribute(s) for the method's target(s)."
				);
			}

			// Resolve per-method generation mode.
			// Priority: method GenerationMode > interface/assembly GenerationMode > Auto (per-method param analysis).
			var useV1Generation = ResolveUseV1Generation(
				logAttribute?.GenerationMode ?? 0, // Auto at method level - inherit from interface
				interfaceGenerationMode,
				hasMultipleExceptions,
				methodParameters
			);

			methodTargets.Add(
				new(
					MethodName: method.Name,
					IsScoped: isScoped,
					LoggerActionFieldName: loggerActionFieldName,
					UnknownReturnType: !isKnownReturnType,
					LogName: logName, // This includes any prefix information
					EventId: logAttribute?.EventIdOrNull,
					MessageTemplate: messageTemplate,
					TemplateProperties: messageTemplateMatches,
					TemplateIsOrdinalBased: templateIsOrdinalBased,
					TemplateIsNamedBased: templateIsNamedBased,
					MSLevel: PropertyLibrary.Logging.LogLevelTypeMap[level],
					Parameters: methodParameters,
					ParametersSansException: isScoped
						? methodParameters
						: [.. methodParameters.Where(m => !m.IsException)],
					ExceptionParameter: exceptionParam,
					HasMultipleExceptions: hasMultipleExceptions,
					InferredErrorLevel: inferredErrorLevel,
					TargetGenerationState: targetGenerationState,
					UseV1Generation: useV1Generation,
					HasExplicitLevel: hasExplicitLevel
				)
			);
		}

		// Post-pass: mark duplicate method names as invalid (emitter generates throw stubs; TSG1003 raised by analyzer)
		var seenNames = new HashSet<string>(StringComparer.Ordinal);
		for (var i = 0; i < methodTargets.Count; i++)
		{
			var t = methodTargets[i];
			if (!seenNames.Add(t.MethodName))
				methodTargets[i] = t with { TargetGenerationState = t.TargetGenerationState with { IsValid = false } };
		}

		return [.. methodTargets];
	}

	static (
		string LogName,
		string MessageTemplate,
		bool HasMultipleExceptions,
		LogParameterTarget? ExceptionParam,
		bool InferredErrorLevel,
		int Level
	) ResolveLogMethodDetails(
		string interfaceName,
		string className,
		LoggerAttributeData loggerTarget,
		LogAttributeData? logAttribute,
		string methodName,
		int defaultPrefixType,
		int defaultLogLevel,
		bool isScoped,
		ImmutableArray<LogParameterTarget> methodParameters
	)
	{
		var logName = GetLogName(interfaceName, className, loggerTarget, logAttribute, methodName, defaultPrefixType);
		var messageTemplate =
			logAttribute?.MessageTemplate ?? GenerateTemplateMessage(logName, isScoped, methodParameters);
		var hasMultipleExceptions = !isScoped && methodParameters.Count(m => m.IsException) > 1;
		var exceptionParam =
			hasMultipleExceptions ? null
			: isScoped ? null
			: methodParameters.FirstOrDefault(m => m.IsException);

		var inferredErrorLevel = exceptionParam != null && logAttribute?.LevelOrNull == null;
		var level = logAttribute?.LevelOrNull ?? (exceptionParam == null ? defaultLogLevel : 4); // Error

		return (logName, messageTemplate, hasMultipleExceptions, exceptionParam, inferredErrorLevel, level);
	}

	static LogMethodTarget BuildErrorStubTarget(
		IMethodSymbol method,
		bool isScoped,
		bool invalidReturnType,
		int defaultLogLevel
	)
	{
		var stubParams = ImmutableArray.CreateRange(
			method.Parameters,
			p => new LogParameterTarget(
				Name: p.Name,
				UpperCasedName: Utilities.UppercaseFirstChar(p.Name),
				ParameterType: TypeReference.Create(p.Type),
				IsException: false,
				IsFirstException: false,
				IsIEnumerable: false,
				IsArray: false,
				IsComplexType: false,
				LogPropertiesAttribute: null,
				LogProperties: null,
				ExpandEnumerableAttribute: null,
				ExcludedTargets: GenerationType.None
			)
		);

		return new(
			MethodName: method.Name,
			IsScoped: isScoped,
			LoggerActionFieldName: $"_{Utilities.LowercaseFirstChar(method.Name)}Action",
			UnknownReturnType: invalidReturnType,
			LogName: method.Name,
			EventId: null,
			MessageTemplate: string.Empty,
			TemplateProperties: ImmutableArray<MessageTemplateHole>.Empty,
			TemplateIsOrdinalBased: false,
			TemplateIsNamedBased: false,
			MSLevel: PropertyLibrary.Logging.LogLevelTypeMap[defaultLogLevel],
			Parameters: stubParams,
			ParametersSansException: stubParams,
			ExceptionParameter: null,
			HasMultipleExceptions: false,
			InferredErrorLevel: false,
			TargetGenerationState: new TargetGeneration(
				IsValid: false,
				RaiseInferenceNotSupportedWithMultiTargeting: false,
				RaiseMultiGenerationTargetsNotSupported: false
			),
			UseV1Generation: false,
			HasLogPropertiesAndExpandEnumerable: true
		);
	}

	static (
		bool IsValid,
		bool TemplateIsOrdinalBased,
		bool TemplateIsNamedBased,
		ImmutableArray<LogParameterTarget> Parameters,
		LogParameterTarget? ExceptionParam
	) ProcessMessageTemplate(
		EquatableArray<MessageTemplateHole> matches,
		ImmutableArray<LogParameterTarget> methodParameters,
		LogParameterTarget? exceptionParam
	)
	{
		if (matches.IsEmpty)
			return (true, false, false, methodParameters, exceptionParam);

		// Validate ordinal positions (not greater than number of params)
		// and that named and ordinal placeholders are not mixed.
		var templateIsOrdinalBased = matches.Any(m => m.Ordinal.HasValue);
		var templateIsNamedBased = matches.Any(m => m.Name != null);
		if (templateIsOrdinalBased && templateIsNamedBased)
			return (false, templateIsOrdinalBased, templateIsNamedBased, methodParameters, exceptionParam);

		var maxOrdinalValue = matches.Any(m => m.IsPositional)
			? matches.Where(m => m.IsPositional).Max(m => m.Ordinal!.Value)
			: 0;
		if (maxOrdinalValue > methodParameters.Length)
			return (false, templateIsOrdinalBased, templateIsNamedBased, methodParameters, exceptionParam);

		// Tag each parameter with the template holes it is referenced by (ordinal or name).
		var paramsBuilder = ImmutableArray.CreateBuilder<LogParameterTarget>(methodParameters.Length);
		for (var i = 0; i < methodParameters.Length; i++)
		{
			var param = methodParameters[i];
			var holes = matches
				.Where(m =>
					(m.IsPositional && m.Ordinal == i)
					|| (m.Name?.Equals(param.Name, StringComparison.OrdinalIgnoreCase) == true)
				)
				.ToImmutableArray();

			paramsBuilder.Add(holes.Length > 0 ? param with { ReferencedHoles = holes } : param);
		}

		var rebuilt = paramsBuilder.MoveToImmutable();

		// Re-acquire exceptionParam from rebuilt parameters so ReferencedHoles are up-to-date.
		var rebuiltExceptionParam = exceptionParam == null ? null : rebuilt.FirstOrDefault(m => m.IsException);

		return (true, templateIsOrdinalBased, templateIsNamedBased, rebuilt, rebuiltExceptionParam);
	}

	static bool ResolveUseV1Generation(
		int methodGenMode,
		int interfaceGenerationMode,
		bool hasMultipleExceptions,
		ImmutableArray<LogParameterTarget> methodParameters
	)
	{
		if (methodGenMode == 1) // V1 forced at method level
			return true;
		if (methodGenMode == 2) // V2 forced at method level
			return false;
		if (interfaceGenerationMode == 1) // V1 forced at interface/assembly level
			return true;
		if (interfaceGenerationMode == 2) // V2 forced at interface/assembly level
			return false;

		// Auto: choose v1 when the method's parameters allow it (best performance).
		// v1 requires: ≤6 non-exception parameters, single exception, no [ExpandEnumerable], no [LogProperties].
		return !hasMultipleExceptions
			&& methodParameters.Count(p => !p.IsException) <= PropertyLibrary.Logging.MaxNonExceptionParameters
			&& !methodParameters.Any(p => p.ExpandEnumerableAttribute != null)
			&& !methodParameters.Any(p => p.LogPropertiesAttribute != null);
	}

	static string GetLogName(
		string interfaceName,
		string className,
		LoggerAttributeData loggerAttribute,
		LogAttributeData? logAttribute,
		string methodName,
		int defaultPrefixType
	)
	{
		if (logAttribute?.Name is { } name)
			methodName = name;

		var prefixType = loggerAttribute.PrefixTypeOrNull ?? defaultPrefixType; // Default as LoggerGeneration level, or Default (0)

		if (prefixType == 1)
		{
			// Interface
			return $"{interfaceName}.{methodName}";
		}
		else if (prefixType == 2)
		{
			// Class
			return $"{className}.{methodName}";
		}
		else if (prefixType == 3)
		{
			// Custom
			if (!string.IsNullOrWhiteSpace(loggerAttribute.CustomPrefix))
				return $"{loggerAttribute.CustomPrefix}.{methodName}";
		}
		else if (prefixType == 4)
		{
			// TrimmedClassName
			if (interfaceName[0] == 'I')
				interfaceName = interfaceName.Substring(1);

			foreach (var suffix in SuffixesToRemove)
			{
				if (interfaceName.EndsWith(suffix, StringComparison.Ordinal) && interfaceName.Length > suffix.Length)
				{
					interfaceName = interfaceName.Substring(0, interfaceName.Length - suffix.Length);
					break;
				}
			}

			return $"{interfaceName}.{methodName}";
		}

		// This is the Default case or if it's Custom
		// and the CustomPrefix is null, empty or whitespace.
		return methodName;
	}

	static string GenerateTemplateMessage(
		string logEntryName,
		bool isScoped,
		ImmutableArray<LogParameterTarget> methodParameters
	)
	{
		StringBuilder builder = new();

		builder.Append(logEntryName);

		var count = methodParameters.Count(m => !m.IsException);
		if (count > 0)
			builder.Append(": ");

		var index = 0;
		foreach (var parameter in methodParameters)
		{
			if (!isScoped && parameter.IsException)
				continue;

			builder
				.Append(parameter.UpperCasedName)
				.Append(" = ")
				.Append('{')
				.Append(parameter.UpperCasedName)
				.Append("}, ");

			index++;
		}

		if (index > 0)
		{
			// Trim the last ", "
			builder.Remove(builder.Length - 2, 2);
		}

		return builder.ToString();
	}

	static ImmutableArray<LogParameterTarget> GetLogMethodParameters(
		IMethodSymbol method,
		Compilation compilation,
		ISourceGenLogger? logger,
		CancellationToken token,
		out bool hasError
	)
	{
		hasError = false;

		List<LogParameterTarget> parameters = [];
		var isFirstException = true;
		foreach (var parameter in method.Parameters)
		{
			token.ThrowIfCancellationRequested();

			// Skip Activity-related parameters and TagList - they are not valid for logging
			var parameterType = TypeReference.Create(parameter.Type);
			if (
				parameterType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.Activity)
				|| parameterType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.ActivityContext)
				|| parameterType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.ActivityLink)
				|| parameterType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.ActivityLinkArray)
				|| parameterType.Identity.Equals(TypeLibrary.System.TagList)
			)
			{
				logger?.Debug($"Skipping parameter '{parameter.Name}' of type '{parameterType}' from logging.");
				continue;
			}

			var logPropertiesAttribute = SharedHelpers.GetLogPropertiesAttribute(parameter, token);
			var expandEnumerableAttribute = SharedHelpers.GetExpandEnumerableAttribute(parameter, token);

			if (logPropertiesAttribute != null && expandEnumerableAttribute != null)
			{
				hasError = true;
				break;
			}

			List<LogPropertiesParameterDetails>? logProperties = null;
			if (logPropertiesAttribute != null)
			{
				// At this point, we know the caller wants to expand the properties for the given type.
				// So we can find the names of all the properties and their types.

				var type = parameter.Type;
				foreach (var property in type.GetMembers().OfType<IPropertySymbol>())
				{
					var propertyName = property.Name;
					if (
						Utilities.ContainsAttribute(
							property,
							TypeLibrary.Logging.MicrosoftExtensions.LogPropertyIgnoreAttribute,
							token
						)
					)
					{
						logger?.Debug(
							$"Skipping property {propertyName} on {parameter.Name} as it is marked with {TypeLibrary.Logging.MicrosoftExtensions.LogPropertyIgnoreAttribute}."
						);
						continue;
					}

					var isNullable =
						property.Type.IsReferenceType
						|| property.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

					logProperties ??= [];

					logProperties.Add(new(PropertyName: propertyName, IsNullable: isNullable));
				}
			}

			var logParameterType = TypeReference.Create(parameter.Type);
			var isException = parameter.Type.IsExceptionType();
			parameters.Add(
				new(
					Name: parameter.Name,
					UpperCasedName: Utilities.UppercaseFirstChar(parameter.Name),
					ParameterType: logParameterType,
					IsException: isException,
					IsFirstException: isException && isFirstException,
					IsIEnumerable: parameter.Type.IsIEnumerable(compilation),
					IsArray: parameter.Type.IsArray(),
					IsComplexType: parameter.Type.IsComplexType(),
					LogPropertiesAttribute: logPropertiesAttribute,
					LogProperties: logProperties != null ? new([.. logProperties]) : [],
					ExpandEnumerableAttribute: expandEnumerableAttribute,
					ExcludedTargets: SharedHelpers.GetExcludeTargetsAttribute(parameter, token)?.ExcludedTargets
						?? GenerationType.None
				)
			);

			if (isException)
				isFirstException = false;
		}

		logger?.Debug($"Found {parameters.Count} parameter(s) for {method.Name}.");

		return [.. parameters];
	}
}
