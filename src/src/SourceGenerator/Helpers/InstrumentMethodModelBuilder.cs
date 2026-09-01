using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

/// <summary>
/// Builds the <see cref="InstrumentTarget"/> models for a meter interface.
/// </summary>
static class InstrumentMethodModelBuilder
{
	public static ImmutableArray<InstrumentTarget> BuildInstrumentationMethods(
		GenerationType generationType,
		MeterAttributeRecord meterAttribute,
		MeterGenerationAttributeRecord? meterGenerationAttribute,
		TelemetryGenerationAttributeRecord telemetryGeneration,
		string meterName,
		INamedTypeSymbol interfaceSymbol,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		// Get naming convention from TelemetryGenerationAttribute (default to OpenTelemetry = 1)
		var namingConvention = telemetryGeneration.NamingConvention;
		var lowercaseInstrumentName = meterAttribute.LowercaseInstrumentName;
		var isLegacy = namingConvention == 0;

		var prefix = GeneratePrefix(meterGenerationAttribute, meterAttribute, interfaceSymbol.Name, token);
		var lowercaseTagKeys = meterAttribute.LowercaseTagKeys;

		List<InstrumentTarget> methodTargets = [];
		foreach (var method in PipelineHelpers.GetAllInterfaceMethods(interfaceSymbol, token))
		{
			token.ThrowIfCancellationRequested();

			if (Utilities.ContainsAttribute(method, TemplateLibrary.Shared.ExcludeAttribute, token))
			{
				logger?.Debug($"Skipping {interfaceSymbol.Name}.{method.Name}, explicitly excluded.");
				continue;
			}

			if (method.Arity > 0)
				continue;

			logger?.Debug($"Found possible instrument method {interfaceSymbol.Name}.{method.Name}.");

			var instrumentAttribute = SharedHelpers.GetInstrumentAttribute(method, logger, token);
			var validAutoCounter =
				instrumentAttribute?.InstrumentType is InstrumentTypes.Counter && instrumentAttribute.IsAutoIncrement;

			var parameters = GetInstrumentParameters(
				method,
				lowercaseTagKeys,
				validAutoCounter,
				namingConvention,
				logger,
				token
			);
			var measurementParameters = parameters
				.Where(m => m.ParamDestination == InstrumentParameterDestination.Measurement)
				.ToImmutableArray();
			var tagParameters = parameters
				.Where(m => m.ParamDestination == InstrumentParameterDestination.Tag)
				.ToImmutableArray();
			var measurementParameter = measurementParameters.FirstOrDefault();

			var fieldName = $"_{Utilities.LowercaseFirstChar(method.Name)}Instrument";
			var instrumentName = instrumentAttribute?.Name;
			if (string.IsNullOrWhiteSpace(instrumentName))
				instrumentName = method.Name;

			(instrumentName, prefix) = ResolveInstrumentName(
				instrumentName!,
				prefix,
				lowercaseInstrumentName,
				isLegacy,
				meterGenerationAttribute,
				meterName
			);

			var returnsBool = method.ReturnType.SpecialType == SpecialType.System_Boolean;
			var targetGenerationState = Utilities.IsValidGenerationTarget(
				method,
				generationType,
				GenerationType.Metrics
			);

			if (!targetGenerationState.IsValid)
				LogTargetState(logger, interfaceSymbol.Name, method.Name, targetGenerationState);
			else if (instrumentAttribute == null)
				logger?.Warning("Missing instrument attribute.");

			var instrumentMeasurementType = measurementParameter?.InstrumentType ?? TypeLibrary.System.Int32;

			methodTargets.Add(
				new(
					MethodName: method.Name,
					ReturnType: TypeReference.Create(method.ReturnType),
					ReturnsBool: returnsBool,
					IsNullableReturn: method.ReturnType.NullableAnnotation == NullableAnnotation.Annotated,
					FieldName: fieldName,
					InstrumentMeasurementType: instrumentMeasurementType,
					IsObservable: instrumentAttribute?.IsObservable == true,
					MetricName: prefix + instrumentName!,
					InstrumentAttribute: instrumentAttribute,
					Parameters: parameters,
					Tags: tagParameters,
					MeasurementParameter: measurementParameter,
					TargetGenerationState: targetGenerationState
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

	static (string InstrumentName, string? Prefix) ResolveInstrumentName(
		string instrumentName,
		string? prefix,
		bool lowercaseInstrumentName,
		bool isLegacy,
		MeterGenerationAttributeRecord? meterGenerationAttribute,
		string meterName
	)
	{
		if (!lowercaseInstrumentName)
			return (instrumentName, prefix);

		if (isLegacy)
		{
			// Legacy: Just lowercase without word-boundary splitting
#pragma warning disable CA1308 // Intentional lowercase for legacy compatibility
			return (instrumentName.ToLowerInvariant(), prefix?.ToLowerInvariant());
#pragma warning restore CA1308
		}

		// OpenTelemetry: Convert PascalCase to snake_case (underscores separate words)
		// Per OTel semantic conventions: dots separate namespace hierarchy, underscores separate words
		// Example: dotnet.gc.last_collection.memory.committed_size
		instrumentName = Utilities.ConvertToSeparatedLowercase(instrumentName, '_');
		if (!string.IsNullOrEmpty(prefix))
			prefix = Utilities.ConvertToSeparatedLowercase(prefix!, '_');

		// For OpenTelemetry convention only: Prepend meter name as namespace with dot separator
		// e.g., meter "myapp.products" + instrument "pricing_page_requests" -> "myapp_products.pricing_page_requests"
		var meterNameGenType = meterGenerationAttribute?.MeterNameGenerationType ?? 1; // Default to DotNet
		if (meterNameGenType == 0 && !string.IsNullOrWhiteSpace(meterName)) // OpenTelemetry only
		{
			var meterPrefix = Utilities.ConvertToSeparatedLowercase(meterName, '_');
			instrumentName = $"{meterPrefix}.{instrumentName}";
		}

		return (instrumentName, prefix);
	}

	static void LogTargetState(
		ISourceGenLogger? logger,
		string interfaceName,
		string methodName,
		TargetGeneration targetGenerationState
	)
	{
		if (targetGenerationState.IsValid)
			return;

		if (targetGenerationState.RaiseMultiGenerationTargetsNotSupported)
			logger?.Debug($"Identified {interfaceName}.{methodName} as problematic as it has another target types.");
		else if (targetGenerationState.RaiseInferenceNotSupportedWithMultiTargeting)
			logger?.Debug($"Identified {interfaceName}.{methodName} as problematic as it is inferred.");
		else if (targetGenerationState.RaiseMissingInterfaceSource)
			logger?.Debug(
				$"Identified {interfaceName}.{methodName} as problematic as the interface is missing source attribute(s) for the method's target(s)."
			);
	}

	static ImmutableArray<InstrumentParameterTarget> GetInstrumentParameters(
		IMethodSymbol method,
		bool lowercaseTagKeys,
		bool isAutoCounter,
		int namingConvention,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		List<InstrumentParameterTarget> parameterTargets = [];
		foreach (var parameter in method.Parameters)
		{
			token.ThrowIfCancellationRequested();

			// Skip Activity-related parameters - they are not valid for metrics
			var paramType = TypeReference.Create(parameter.Type);
			if (
				paramType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.Activity)
				|| paramType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.ActivityContext)
				|| paramType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.ActivityLink)
				|| paramType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.ActivityLinkArray)
			)
			{
				logger?.Debug($"Skipping Activity-related parameter '{parameter.Name}' from metrics.");
				continue;
			}

			var tagAttribute = GetDestination(parameter, logger, token, out var destination);

			var isFuncType = false;
			var isIEnumerableType = false;
			var isMeasurementType = false;
			var isValidInstrumentType = false;
			TypeReference? instrumentType = null;

			if (destination != InstrumentParameterDestination.Tag)
			{
				(isFuncType, isIEnumerableType, isMeasurementType, isValidInstrumentType, instrumentType) =
					TryResolveMeasurementType(parameter.Type, isAutoCounter, logger);
				if (instrumentType != null)
					destination = InstrumentParameterDestination.Measurement;
			}

			if (destination == InstrumentParameterDestination.Unknown)
			{
				logger?.Debug($"Unable to match parameter {parameter.Name}, inferring tag.");
				destination = InstrumentParameterDestination.Tag;
			}

			var parameterName = parameter.Name;
			var generatedName = PipelineHelpers.GenerateParameterName(
				tagAttribute?.Name ?? parameterName,
				null,
				lowercaseTagKeys,
				namingConvention
			);

			// Check for ExcludeTargetsAttribute
			var excludeTargets = SharedHelpers.GetExcludeTargetsAttribute(parameter, token);

			parameterTargets.Add(
				new(
					ParameterName: parameterName,
					ParameterType: TypeReference.Create(parameter.Type),
					IsFunc: isFuncType,
					IsIEnumerable: isIEnumerableType,
					IsMeasurement: isMeasurementType,
					IsValidInstrumentType: isValidInstrumentType,
					InstrumentType: instrumentType,
					GeneratedName: generatedName,
					ParamDestination: destination,
					SkipOnNullOrEmpty: PipelineHelpers.GetSkipOnNullOrEmptyValue(tagAttribute),
					ExcludedTargets: excludeTargets?.ExcludedTargets ?? GenerationType.None
				)
			);
		}

		return [.. parameterTargets];
	}

	static TagOrBaggageAttributeRecord? GetDestination(
		IParameterSymbol parameter,
		ISourceGenLogger? logger,
		CancellationToken token,
		out InstrumentParameterDestination destination
	)
	{
		destination = InstrumentParameterDestination.Unknown;

		if (Utilities.TryContainsAttribute(parameter, TemplateLibrary.Shared.TagAttribute, token, out var attribute))
		{
			logger?.Debug($"Found explicit tag: {parameter.Name}.");
			destination = InstrumentParameterDestination.Tag;
			return SharedHelpers.GetTagOrBaggageAttribute(attribute!, token);
		}

		if (Utilities.ContainsAttribute(parameter, TemplateLibrary.Metrics.InstrumentMeasurementAttribute, token))
		{
			logger?.Debug($"Found explicit instrument measurement: {parameter.Name}.");
			destination = InstrumentParameterDestination.Measurement;
		}

		return null;
	}

	static (
		bool IsFunc,
		bool IsIEnumerable,
		bool IsMeasurement,
		bool IsValidInstrumentType,
		TypeReference? InstrumentType
	) TryResolveMeasurementType(ITypeSymbol parameterType, bool isAutoCounter, ISourceGenLogger? logger)
	{
		if (parameterType is not INamedTypeSymbol namedParameterType)
			return (false, false, false, false, null);

		var isFunc = new TypeIdentity(typeof(Func<>)).Matches(namedParameterType);
		if (!isFunc)
		{
			// For non-observable instruments.
			var isValid = SharedHelpers.IsValidMeasurementValueType(namedParameterType);
			if (isValid && !isAutoCounter)
			{
				var typeRef = TypeReference.Create(namedParameterType);
				logger?.Debug($"Found valid instrument type: {typeRef}");
				return (false, false, false, true, typeRef);
			}

			return (false, false, false, isValid, null);
		}

		// For observable instruments: Func<...>.
		var funcArg = namedParameterType.TypeArguments[0];

		if (
			funcArg is INamedTypeSymbol enumerableType
			&& TypeLibrary.System.GenericIEnumerable.Equals(enumerableType.ConstructedFrom)
		)
		{
			// Func<IEnumerable<...>>
			var enumerableArg = enumerableType.TypeArguments[0];
			if (
				enumerableArg is INamedTypeSymbol measurementContainer
				&& TypeLibrary.Metrics.SystemDiagnostics.Measurement.Equals(measurementContainer.ConstructedFrom)
			)
			{
				// Func<IEnumerable<Measurement<T>>>
				var valueType = measurementContainer.TypeArguments[0];
				var isValid = SharedHelpers.IsValidMeasurementValueType(valueType);
				if (isValid)
				{
					var typeRef = TypeReference.Create(valueType);
					logger?.Debug($"Found valid instrument type: Func -> IEnumerable -> Measurement -> {typeRef}");
					return (true, true, true, true, typeRef);
				}

				return (true, true, true, false, null);
			}

			return (true, true, false, false, null);
		}

		if (
			funcArg is INamedTypeSymbol funcMeasurementType
			&& TypeLibrary.Metrics.SystemDiagnostics.Measurement.Equals(funcMeasurementType.ConstructedFrom)
		)
		{
			// Func<Measurement<T>>
			var isValid = SharedHelpers.IsValidMeasurementValueType(funcMeasurementType.TypeArguments[0]);
			if (isValid)
			{
				var typeRef = TypeReference.Create(funcMeasurementType.TypeArguments[0]);
				logger?.Debug($"Found valid instrument type: Func -> Measurement -> {typeRef}");
				return (true, false, true, true, typeRef);
			}

			return (true, false, true, false, null);
		}

		// Func<T> directly.
		var isValidDirect = SharedHelpers.IsValidMeasurementValueType(funcArg);
		if (isValidDirect)
		{
			var directRef = TypeReference.Create(funcArg);
			logger?.Debug($"Found valid instrument type: Func -> {directRef}");
			return (true, false, false, true, directRef);
		}

		return (true, false, false, false, null);
	}

	static string? GeneratePrefix(
		MeterGenerationAttributeRecord? meterGenerationAttribute,
		MeterAttributeRecord meterAttribute,
		string interfaceName,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		string? prefix = null;
		var separator =
			meterGenerationAttribute?.InstrumentSeparator ?? PropertyLibrary.Metrics.InstrumentSeparatorDefault;

		if (
			meterAttribute.IncludeAssemblyInstrumentPrefix
			&& !string.IsNullOrWhiteSpace(meterGenerationAttribute?.InstrumentPrefix)
		)
			prefix = meterGenerationAttribute!.InstrumentPrefix! + separator;

		// Check if interface-level prefix is explicitly set
		if (!string.IsNullOrWhiteSpace(meterAttribute.InstrumentPrefix))
		{
			prefix += meterAttribute.InstrumentPrefix! + separator;
		}
		else
		{
			// Auto-generate prefix from interface name if not explicitly set
			var autoPrefix = Utilities.GenerateInstrumentPrefixFromInterfaceName(interfaceName);
			if (!string.IsNullOrWhiteSpace(autoPrefix))
				prefix += autoPrefix + separator;
		}

		return prefix;
	}
}
