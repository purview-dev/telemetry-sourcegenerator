using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class PipelineHelpers
{
	static readonly string[] SuffixesToRemove = ["Logs", "Logger", "Telemetry"];

	public static bool HasLoggerTargetAttribute(SyntaxNode _, CancellationToken __) => true;

	public static LoggerTarget? BuildLoggerTransform(
		GeneratorAttributeSyntaxContext context,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		var iLoggerTypeSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName(
			Constants.Logging.MicrosoftExtensions.ILogger.FullyQualifiedName
		);
		if (iLoggerTypeSymbol is null)
		{
			logger?.Diagnostic(
				$"Requested a Logger target to be generated, but could not find the ILogger symbol referenced '{context.TargetNode.Flatten()}'."
			);
			return null;
		}

		if (context.TargetNode is not InterfaceDeclarationSyntax interfaceDeclaration)
		{
			logger?.Error($"Could not find interface syntax from the target node '{context.TargetNode.Flatten()}'.");
			return null;
		}

		if (context.TargetSymbol is not INamedTypeSymbol interfaceSymbol)
		{
			logger?.Error($"Could not find interface symbol '{interfaceDeclaration.Flatten()}'.");
			return null;
		}

		if (interfaceSymbol.Arity > 0)
		{
			logger?.Diagnostic(
				$"Cannot generate a Logger target for a generic interface '{interfaceDeclaration.Flatten()}'."
			);
			return null;
		}

		var semanticModel = context.SemanticModel;
		var loggerAttribute = SharedHelpers.GetLoggerAttribute(context.TargetSymbol, semanticModel, logger, token);
		if (loggerAttribute == null)
		{
			logger?.Error(
				$"Could not find {Constants.Logging.LoggerAttribute} when one was expected '{interfaceDeclaration.Flatten()}'."
			);
			return null;
		}

		var telemetryGeneration = SharedHelpers.GetTelemetryGenerationAttribute(
			interfaceSymbol,
			semanticModel,
			logger,
			token
		);
		var className = telemetryGeneration.ClassName.IsSet
			? telemetryGeneration.ClassName.Value!
			: GenerateClassName(interfaceSymbol.Name);

		var loggerGenerationAttribute = SharedHelpers.GetLoggerGenerationAttribute(semanticModel, logger, token);
		var defaultLogLevel = loggerGenerationAttribute?.DefaultLevel?.Value ?? Constants.Logging.DefaultLevel;
		var defaultPrefixType =
			loggerGenerationAttribute?.DefaultPrefixType.IsSet == true
				? loggerGenerationAttribute.DefaultPrefixType.Value!.Value
				: 0;

		// Resolve the effective generation mode using priority:
		// interface GenerationMode > assembly GenerationMode > Auto (per-method decision)
		var interfaceGenerationMode =
			loggerAttribute.GenerationMode.IsSet ? loggerAttribute.GenerationMode.Value!.Value
			: loggerGenerationAttribute?.GenerationMode.IsSet == true
				? loggerGenerationAttribute.GenerationMode.Value!.Value
			: 0; // Auto

		var generationType = SharedHelpers.GetGenerationTypes(interfaceSymbol, token);
		var fullNamespace = Utilities.GetFullNamespace(interfaceDeclaration, true);
		var logMethods = BuildLogMethods(
			generationType,
			className,
			defaultLogLevel,
			defaultPrefixType,
			loggerAttribute,
			context,
			semanticModel,
			interfaceSymbol,
			logger,
			interfaceGenerationMode: interfaceGenerationMode,
			token
		);

		return new(
			TelemetryGeneration: telemetryGeneration,
			GenerationType: generationType,
			ClassNameToGenerate: className,
			ClassNamespace: Utilities.GetNamespace(interfaceDeclaration),
			ParentClasses: Utilities.GetParentClasses(interfaceDeclaration),
			FullNamespace: fullNamespace,
			FullyQualifiedName: fullNamespace + className,
			InterfaceType: PurviewTypeFactory.Create(interfaceSymbol),
			LoggerAttribute: loggerAttribute,
			DefaultLevel: defaultLogLevel,
			LogMethods: logMethods,
			UseMSLoggingTelemetryBasedGeneration: interfaceGenerationMode != 1 // false only when V1 forced
		);
	}

	static ImmutableArray<LogMethodTarget> BuildLogMethods(
		GenerationType generationType,
		string className,
		int defaultLogLevel,
		int defaultPrefixType,
		LoggerAttributeRecord loggerTarget,
		GeneratorAttributeSyntaxContext _,
		SemanticModel semanticModel,
		INamedTypeSymbol interfaceSymbol,
		GenerationLogger? logger,
		int interfaceGenerationMode,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		List<LogMethodTarget> methodTargets = [];
		foreach (var method in GetAllInterfaceMethods(interfaceSymbol, semanticModel.Compilation, token))
		{
			if (Utilities.ContainsAttribute(method, Constants.Shared.ExcludeAttribute, token))
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
				var hasLoggingAttribute = SharedHelpers.GetLogAttribute(method, semanticModel, logger, token) != null;

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
			var invalidReturnType = ValidateLogReturnType(method, semanticModel, logger, token).HasValue;

			var isScoped = Constants.System.IDisposable.Equals(method.ReturnType);
			var methodParameters = GetLogMethodParameters(
				method,
				semanticModel,
				logger,
				token,
				out var hasParameterError
			);
			if (hasParameterError)
			{
				// LogProperties + ExpandEnumerable conflict: add invalid stub so emitter can report TSG2006
				var stubParams = ImmutableArray.CreateRange(
					method.Parameters,
					p => new LogParameterTarget(
						Name: p.Name,
						UpperCasedName: Utilities.UppercaseFirstChar(p.Name),
						ParameterType: PurviewTypeFactory.Create(p.Type),
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
				methodTargets.Add(
					new(
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
						MSLevel: Constants.Logging.LogLevelTypeMap[defaultLogLevel],
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
					)
				);
				continue;
			}

			var logAttribute = SharedHelpers.GetLogAttribute(method, semanticModel, logger, token);
			var hasExplicitLevel = logAttribute?.Level.IsSet ?? false;

			var isKnownReturnType = !invalidReturnType;
			var loggerActionFieldName = $"_{Utilities.LowercaseFirstChar(method.Name)}Action";

			var logName = GetLogName(
				interfaceSymbol.Name,
				className,
				loggerTarget,
				logAttribute,
				method.Name,
				defaultPrefixType
			);
			var messageTemplate =
				logAttribute?.MessageTemplate.Value ?? GenerateTemplateMessage(logName, isScoped, methodParameters);
			var hasMultipleExceptions = !isScoped && methodParameters.Count(m => m.IsException) > 1;
			var exceptionParam =
				hasMultipleExceptions ? null
				: isScoped ? null
				: methodParameters.FirstOrDefault(m => m.IsException);

			var inferredErrorLevel = exceptionParam != null;
			if (logAttribute?.Level.IsSet ?? false)
				inferredErrorLevel = false;

			var level = (
				logAttribute?.Level.IsSet == true ? logAttribute.Level.Value!.Value
				: exceptionParam == null ? defaultLogLevel
				: 4 // Error
			)!;

			var messageTemplateMatches = MessageTemplateHole.FromMatches(
				Constants.MessageTemplateMatcher.Matches(messageTemplate)
			);

			var templateIsOrdinalBased = false;
			var templateIsNamedBased = false;
			if (!messageTemplateMatches.IsEmpty)
			{
				// Validate ordinal positions (not greater than number of params)
				// of the template properties and named properties exist.
				// ... we don't support both at the same time.
				templateIsOrdinalBased = messageTemplateMatches.Any(m => m.Ordinal.HasValue);
				templateIsNamedBased = messageTemplateMatches.Any(m => m.Name != null);
				if (templateIsOrdinalBased && templateIsNamedBased)
				{
					continue;
				}

				var maxOrdinalValue = messageTemplateMatches.Any(m => m.IsPositional)
					? messageTemplateMatches.Where(m => m.IsPositional).Max(m => m.Ordinal!.Value)
					: 0;
				if (maxOrdinalValue > methodParameters.Length)
				{
					continue;
				}

				var paramsBuilder = ImmutableArray.CreateBuilder<LogParameterTarget>(methodParameters.Length);
				for (var i = 0; i < methodParameters.Length; i++)
				{
					var param = methodParameters[i];
					// Is it used in the template?
					// ... as an ordinal, or as a named property?
					// Remember, it may match more than one hole.

					var holes = messageTemplateMatches
						.Where(m =>
							(m.IsPositional && m.Ordinal == i)
							|| (m.Name?.Equals(param.Name, StringComparison.OrdinalIgnoreCase) == true)
						)
						.ToImmutableArray();

					paramsBuilder.Add(holes.Length > 0 ? param with { ReferencedHoles = holes } : param);
				}

				methodParameters = paramsBuilder.MoveToImmutable();

				// Re-acquire exceptionParam from rebuilt methodParameters so ReferencedHoles are up-to-date
				if (exceptionParam != null)
					exceptionParam = methodParameters.FirstOrDefault(m => m.IsException);
			}

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
			bool useV1Generation;
			var methodGenMode =
				logAttribute?.GenerationMode.IsSet == true ? logAttribute.GenerationMode.Value!.Value : 0; // Auto at method level - inherit from interface

			if (methodGenMode == 1) // V1 forced at method level
			{
				useV1Generation = true;
			}
			else if (methodGenMode == 2) // V2 forced at method level
			{
				useV1Generation = false;
			}
			else if (interfaceGenerationMode == 1) // V1 forced at interface/assembly level
			{
				useV1Generation = true;
			}
			else if (interfaceGenerationMode == 2) // V2 forced at interface/assembly level
			{
				useV1Generation = false;
			}
			else
			{
				// Auto: choose v1 when the method's parameters allow it (best performance).
				// v1 requires: ≤6 non-exception parameters, single exception, no [ExpandEnumerable], no [LogProperties].
				useV1Generation =
					!hasMultipleExceptions
					&& methodParameters.Count(p => !p.IsException) <= Constants.Logging.MaxNonExceptionParameters
					&& !methodParameters.Any(p => p.ExpandEnumerableAttribute != null)
					&& !methodParameters.Any(p => p.LogPropertiesAttribute != null);
			}

			methodTargets.Add(
				new(
					MethodName: method.Name,
					IsScoped: isScoped,
					LoggerActionFieldName: loggerActionFieldName,
					UnknownReturnType: !isKnownReturnType,
					LogName: logName, // This includes any prefix information
					EventId: logAttribute?.EventId.Value,
					MessageTemplate: messageTemplate,
					TemplateProperties: messageTemplateMatches,
					TemplateIsOrdinalBased: templateIsOrdinalBased,
					TemplateIsNamedBased: templateIsNamedBased,
					MSLevel: Constants.Logging.LogLevelTypeMap[level],
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

	static string GetLogName(
		string interfaceName,
		string className,
		LoggerAttributeRecord loggerAttribute,
		LogAttributeRecord? logAttribute,
		string methodName,
		int defaultPrefixType
	)
	{
		if (logAttribute?.Name.IsSet == true)
			methodName = logAttribute!.Name.Value!;

		var prefixType = loggerAttribute.PrefixType.IsSet ? loggerAttribute.PrefixType.Value : defaultPrefixType; // Default as LoggerGeneration level, or Default (0)

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
			if (!string.IsNullOrWhiteSpace(loggerAttribute.CustomPrefix.Value))
				return $"{loggerAttribute.CustomPrefix.Value}.{methodName}";
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
		SemanticModel semanticModel,
		GenerationLogger? logger,
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
			var parameterType = PurviewTypeFactory.Create(parameter.Type);
			if (
				Constants.Activities.SystemDiagnostics.Activity.Equals(parameterType)
				|| Constants.Activities.SystemDiagnostics.ActivityContext.Equals(parameterType)
				|| Constants.Activities.SystemDiagnostics.ActivityLink.Equals(parameterType)
				|| Constants.Activities.SystemDiagnostics.ActivityLinkArray.Equals(parameterType)
				|| Constants.System.TagList.Equals(parameterType)
			)
			{
				logger?.Debug($"Skipping parameter '{parameter.Name}' of type '{parameterType}' from logging.");
				continue;
			}

			var logPropertiesAttribute = SharedHelpers.GetLogPropertiesAttribute(
				parameter,
				semanticModel,
				logger,
				token
			);
			var expandEnumerableAttribute = SharedHelpers.GetExpandEnumerableAttribute(
				parameter,
				semanticModel,
				logger,
				token
			);

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
							Constants.Logging.MicrosoftExtensions.LogPropertyIgnoreAttribute,
							token
						)
					)
					{
						logger?.Debug(
							$"Skipping property {propertyName} on {parameter.Name} as it is marked with {Constants.Logging.MicrosoftExtensions.LogPropertyIgnoreAttribute}."
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

			var logParameterType = PurviewTypeFactory.Create(parameter.Type);
			var isException = parameter.Type.IsExceptionType();
			parameters.Add(
				new(
					Name: parameter.Name,
					UpperCasedName: Utilities.UppercaseFirstChar(parameter.Name),
					ParameterType: logParameterType,
					IsException: isException,
					IsFirstException: isException && isFirstException,
					IsIEnumerable: parameter.Type.IsIEnumerable(semanticModel.Compilation),
					IsArray: parameter.Type.IsArray(),
					IsComplexType: parameter.Type.IsComplexType(),
					LogPropertiesAttribute: logPropertiesAttribute,
					LogProperties: logProperties != null
						? new EquatableArray<LogPropertiesParameterDetails>(logProperties.ToImmutableArray())
						: null,
					ExpandEnumerableAttribute: expandEnumerableAttribute,
					ExcludedTargets: SharedHelpers
						.GetExcludeTargetsAttribute(parameter, semanticModel, logger, token)
						?.ExcludedTargets
						?? GenerationType.None
				)
			);

			if (isException)
				isFirstException = false;
		}

		logger?.Debug($"Found {parameters.Count} parameter(s) for {method.Name}.");

		return [.. parameters];
	}

	static (TelemetryDiagnosticDescriptor, ImmutableArray<Location>)? ValidateLogReturnType(
		IMethodSymbol method,
		SemanticModel _, // semanticModel
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		// Valid return types for logging:
		// - void (non-scoped)
		// - IDisposable or IDisposable? (scoped)
		// - Activity? (multi-target with Activity - Activity return type takes priority)
		// Everything else is invalid

		var isVoid = method.ReturnsVoid;

		// Check if return type is IDisposable (handle both nullable and non-nullable)
		var returnType = method.ReturnType;
		var isIDisposable = Constants.System.IDisposable.Equals(returnType);

		// Also check if it's nullable IDisposable (IDisposable?)
		if (!isIDisposable && returnType.NullableAnnotation == NullableAnnotation.Annotated)
		{
			// Get the underlying type without the nullable annotation
			if (returnType is INamedTypeSymbol namedType && !namedType.IsValueType)
			{
				isIDisposable =
					Constants.System.IDisposable.Equals(namedType.OriginalDefinition)
					|| Constants.System.IDisposable.FullyQualifiedName == namedType.ConstructedFrom.ToString();
			}
		}

		// Check if return type is Activity? (allowed for multi-target with Activity attributes)
		var isActivity = Constants.Activities.SystemDiagnostics.Activity.Equals(returnType);

		// If it's Activity, check if this is a valid multi-target scenario
		// (method has Activity attribute that determines return type)
		if (isActivity && SharedHelpers.IsActivityMethod(method, token))
		{
			// Multi-target with Activity - Activity wins return type
			return null;
		}

		// If it's one of the valid types, allow it
		if (isVoid || isIDisposable)
		{
			return null;
		}

		// Everything else is invalid
		logger?.Diagnostic(
			$"Log method {method.Name} must return void or IDisposable (for scoped logs), but returns {method.ReturnType}."
		);

		return (TelemetryDiagnostics.Logging.LogMustReturnVoidOrAsync, method.ReturnType.Locations);
	}
}
