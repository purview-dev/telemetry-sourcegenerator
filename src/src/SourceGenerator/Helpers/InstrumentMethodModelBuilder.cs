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
		MeterAttributeData meterAttribute,
		MeterGenerationAttributeData? meterGenerationAttribute,
		TelemetryGenerationAttributeData telemetryGeneration,
		string meterName,
		INamedTypeSymbol interfaceSymbol,
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

			if (TypeHelpers.HasAttribute(method, TypeLibrary.TelemetryShared.ExcludeAttribute))
				continue;

			if (method.Arity > 0)
				continue;

			var instrumentAttribute = SharedHelpers.GetInstrumentAttribute(method, token);
			var validAutoCounter =
				instrumentAttribute?.InstrumentType is InstrumentTypes.Counter && instrumentAttribute.IsAutoIncrement;

			var parameters = GetInstrumentParameters(
				method,
				lowercaseTagKeys,
				validAutoCounter,
				namingConvention,
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

			var instrumentMeasurementType = measurementParameter?.InstrumentType ?? PurviewTypeLibrary.System.Int32;

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
		MeterGenerationAttributeData? meterGenerationAttribute,
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

	static ImmutableArray<InstrumentParameterTarget> GetInstrumentParameters(
		IMethodSymbol method,
		bool lowercaseTagKeys,
		bool isAutoCounter,
		int namingConvention,
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
				continue;
			}

			var tagAttribute = GetDestination(parameter, token, out var destination);

			var isFuncType = false;
			var isIEnumerableType = false;
			var isMeasurementType = false;
			var isValidInstrumentType = false;
			TypeReference? instrumentType = null;

			if (destination != InstrumentParameterDestination.Tag)
			{
				(isFuncType, isIEnumerableType, isMeasurementType, isValidInstrumentType, instrumentType) =
					TryResolveMeasurementType(parameter.Type, isAutoCounter);
				if (instrumentType != null)
					destination = InstrumentParameterDestination.Measurement;
			}

			if (destination == InstrumentParameterDestination.Unknown)
				destination = InstrumentParameterDestination.Tag;

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
		CancellationToken token,
		out InstrumentParameterDestination destination
	)
	{
		destination = InstrumentParameterDestination.Unknown;

		if (
			Utilities.TryContainsAttribute(
				parameter,
				TypeLibrary.TelemetryShared.TagAttribute,
				token,
				out var attribute
			)
		)
		{
			destination = InstrumentParameterDestination.Tag;
			return SharedHelpers.GetTagOrBaggageAttribute(attribute!, token);
		}

		if (TypeHelpers.HasAttribute(parameter, TypeLibrary.Metrics.InstrumentMeasurementAttribute))
			destination = InstrumentParameterDestination.Measurement;

		return null;
	}

	static (
		bool IsFunc,
		bool IsIEnumerable,
		bool IsMeasurement,
		bool IsValidInstrumentType,
		TypeReference? InstrumentType
	) TryResolveMeasurementType(ITypeSymbol parameterType, bool isAutoCounter)
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
				return (true, false, true, true, typeRef);
			}

			return (true, false, true, false, null);
		}

		// Func<T> directly.
		var isValidDirect = SharedHelpers.IsValidMeasurementValueType(funcArg);
		if (isValidDirect)
		{
			var directRef = TypeReference.Create(funcArg);
			return (true, false, false, true, directRef);
		}

		return (true, false, false, false, null);
	}

	static string? GeneratePrefix(
		MeterGenerationAttributeData? meterGenerationAttribute,
		MeterAttributeData meterAttribute,
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
			prefix = meterGenerationAttribute!.Value.InstrumentPrefix! + separator;

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
