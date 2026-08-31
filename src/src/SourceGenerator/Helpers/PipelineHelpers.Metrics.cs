using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class PipelineHelpers
{
	public static bool HasMeterTargetAttribute(SyntaxNode _, CancellationToken __) => true;

	public static MeterTarget? BuildMeterTransform(
		GeneratorAttributeSyntaxContext context,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

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
				$"Cannot generate a Meter target for a generic interface '{interfaceDeclaration.Flatten()}'."
			);
			return null;
		}

		var semanticModel = context.SemanticModel;
		var meterAttribute = SharedHelpers.GetMeterAttribute(context.TargetSymbol, semanticModel, logger, token);
		if (meterAttribute == null)
		{
			logger?.Error(
				$"Could not find {Constants.Metrics.MeterAttribute} when one was expected '{interfaceDeclaration.Flatten()}'."
			);
			return null;
		}

		var telemetryGeneration = SharedHelpers.GetTelemetryGenerationAttribute(
			interfaceSymbol,
			semanticModel,
			logger,
			token
		);
		var className = telemetryGeneration.ClassName.Or(GenerateClassName(interfaceSymbol.Name));
		var generationType = SharedHelpers.GetGenerationTypes(interfaceSymbol, token);
		var meterGenerationAttribute = SharedHelpers.GetMeterGenerationAttribute(semanticModel, logger, token);
		var fullNamespace = Utilities.GetFullNamespace(interfaceDeclaration, true);

		var meterName = meterAttribute.Name.Value;
		if (string.IsNullOrWhiteSpace(meterName))
		{
			// First check assembly-wide MeterName from MeterGenerationAttribute
			meterName = meterGenerationAttribute?.MeterName.Value;

			if (string.IsNullOrWhiteSpace(meterName))
			{
				// Fall back to assembly name with generation type convention
				var assemblyName = semanticModel.Compilation.Assembly.Name;
				var meterNameGenType = meterGenerationAttribute?.MeterNameGenerationType.Value ?? 1; // Default to DotNet

				if (meterNameGenType == 0) // OpenTelemetry
				{
					// OpenTelemetry: lowercase assembly name
#pragma warning disable CA1308 // Intentional lowercase for OpenTelemetry convention
					meterName = assemblyName.ToLowerInvariant();
#pragma warning restore CA1308
				}
				else // DotNet
				{
					// .NET: preserve assembly name as-is
					meterName = assemblyName;
				}
			}
		}

		var instrumentMethods = BuildInstrumentationMethods(
			generationType,
			meterAttribute,
			meterGenerationAttribute,
			telemetryGeneration,
			meterName!,
			semanticModel,
			interfaceSymbol,
			logger,
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
			MeterName: meterName,
			MeterGeneration: meterGenerationAttribute,
			InstrumentationMethods: instrumentMethods
		);
	}

	static ImmutableArray<InstrumentTarget> BuildInstrumentationMethods(
		GenerationType generationType,
		MeterAttributeRecord meterAttribute,
		MeterGenerationAttributeRecord? meterGenerationAttribute,
		TelemetryGenerationAttributeRecord telemetryGeneration,
		string meterName,
		SemanticModel semanticModel,
		INamedTypeSymbol interfaceSymbol,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		// Get naming convention from TelemetryGenerationAttribute (default to OpenTelemetry = 1)
		var namingConvention = telemetryGeneration?.NamingConvention.Value ?? 1;

		var lowercaseInstrumentName = meterAttribute.LowercaseInstrumentName.IsSet
			? meterAttribute.LowercaseInstrumentName.Value!.Value
			: (meterGenerationAttribute?.LowercaseInstrumentName?.IsSet) != true
				|| meterGenerationAttribute.LowercaseInstrumentName.Value!.Value;

		var prefix = GeneratePrefix(meterGenerationAttribute, meterAttribute, interfaceSymbol.Name, token);
		var lowercaseTagKeys =
			meterAttribute.LowercaseTagKeys?.IsSet == true && meterAttribute.LowercaseTagKeys.Value!.Value;

		List<InstrumentTarget> methodTargets = [];
		foreach (var method in GetAllInterfaceMethods(interfaceSymbol, semanticModel.Compilation, token))
		{
			token.ThrowIfCancellationRequested();

			if (Utilities.ContainsAttribute(method, Constants.Shared.ExcludeAttribute, token))
			{
				logger?.Debug($"Skipping {interfaceSymbol.Name}.{method.Name}, explicitly excluded.");
				continue;
			}

			if (method.Arity > 0)
			{
				continue;
			}

			logger?.Debug($"Found possible instrument method {interfaceSymbol.Name}.{method.Name}.");

			var instrumentAttribute = SharedHelpers.GetInstrumentAttribute(method, semanticModel, logger, token);
			var validAutoCounter =
				instrumentAttribute?.InstrumentType is InstrumentTypes.Counter && instrumentAttribute.IsAutoIncrement;

			var parameters = GetInstrumentParameters(
				method,
				lowercaseTagKeys,
				validAutoCounter,
				namingConvention,
				semanticModel,
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
			var instrumentName = instrumentAttribute?.Name?.Value;
			if (string.IsNullOrWhiteSpace(instrumentName))
				instrumentName = method.Name;

			var isLegacy = namingConvention == 0;

			if (lowercaseInstrumentName)
			{
				if (!isLegacy)
				{
					// OpenTelemetry: Convert PascalCase to snake_case (underscores separate words)
					// Per OTel semantic conventions: dots separate namespace hierarchy, underscores separate words
					// Example: dotnet.gc.last_collection.memory.committed_size
					instrumentName = Utilities.ConvertToSeparatedLowercase(instrumentName!, '_');
					if (!string.IsNullOrEmpty(prefix))
					{
						// Convert prefix components while preserving separator structure
						// This handles both explicitly-set prefixes (e.g., "This.Is.A.Prefix")
						// and auto-generated prefixes (already in snake_case, won't be affected)
						prefix = Utilities.ConvertToSeparatedLowercase(prefix!, '_');
					}

					// For OpenTelemetry convention only: Prepend meter name as namespace with dot separator
					// Check if we're using OpenTelemetry meter name generation (lowercase assembly name)
					// e.g., meter "myapp.products" + instrument "pricing_page_requests"
					//       -> "myapp_products.pricing_page_requests"
					var meterNameGenType = meterGenerationAttribute?.MeterNameGenerationType.Value ?? 1; // Default to DotNet
					if (meterNameGenType == 0 && !string.IsNullOrWhiteSpace(meterName)) // OpenTelemetry only
					{
						var meterPrefix = Utilities.ConvertToSeparatedLowercase(meterName, '_');
						instrumentName = $"{meterPrefix}.{instrumentName}";
					}
				}
				else
				{
					// Legacy: Just lowercase without word-boundary splitting
#pragma warning disable CA1308 // Intentional lowercase for legacy compatibility
					instrumentName = instrumentName!.ToLowerInvariant();
					prefix = prefix?.ToLowerInvariant();
#pragma warning restore CA1308
				}
			}

			var returnsBool = method.ReturnType.SpecialType == SpecialType.System_Boolean;
			var targetGenerationState = Utilities.IsValidGenerationTarget(
				method,
				generationType,
				GenerationType.Metrics
			);
			if (!targetGenerationState.IsValid)
			{
				if (targetGenerationState.RaiseMultiGenerationTargetsNotSupported)
				{
					logger?.Debug(
						$"Identified {interfaceSymbol.Name}.{method.Name} as problematic as it has another target types."
					);
				}
				else if (targetGenerationState.RaiseInferenceNotSupportedWithMultiTargeting)
				{
					logger?.Debug($"Identified {interfaceSymbol.Name}.{method.Name} as problematic as it is inferred.");
				}
				else if (targetGenerationState.RaiseMissingInterfaceSource)
				{
					logger?.Debug(
						$"Identified {interfaceSymbol.Name}.{method.Name} as problematic as the interface is missing source attribute(s) for the method's target(s)."
					);
				}
			}
			else
			{
				if (instrumentAttribute == null)
				{
					logger?.Warning("Missing instrument attribute.");
				}
				else if (!validAutoCounter && measurementParameter == null)
				{
					// No measurement value defined
				}
				else
				{
					if (!validAutoCounter)
					{
						// Validate the parameters and type.
						if (!instrumentAttribute.IsObservable)
						{
							// Multiple measurement parameters or other issues are informational only
						}
					}

					// Check if this is multi-target with Activity (Activity return type is allowed)
					var isMultiTargetWithActivity =
						targetGenerationState.IsMultiTarget
						&& targetGenerationState.MethodTargets.HasFlag(GenerationType.Activities);
					var returnsActivity = Constants.Activities.SystemDiagnostics.Activity.Equals(method.ReturnType);
					_ = isMultiTargetWithActivity;
					_ = returnsActivity;
				}
			}

			var instrumentMeasurementType = measurementParameter?.InstrumentType ?? Constants.System.BuiltInTypes.Int32;

			methodTargets.Add(
				new(
					MethodName: method.Name,
					ReturnType: PurviewTypeFactory.Create(method.ReturnType),
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

	static ImmutableArray<InstrumentParameterTarget> GetInstrumentParameters(
		IMethodSymbol method,
		bool lowercaseTagKeys,
		bool isAutoCounter,
		int namingConvention,
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		List<InstrumentParameterTarget> parameterTargets = [];
		foreach (var parameter in method.Parameters)
		{
			token.ThrowIfCancellationRequested();

			// Skip Activity-related parameters - they are not valid for metrics
			var paramType = PurviewTypeFactory.Create(parameter.Type);
			if (
				Constants.Activities.SystemDiagnostics.Activity.Equals(paramType)
				|| Constants.Activities.SystemDiagnostics.ActivityContext.Equals(paramType)
				|| Constants.Activities.SystemDiagnostics.ActivityLink.Equals(paramType)
				|| Constants.Activities.SystemDiagnostics.ActivityLinkArray.Equals(paramType)
			)
			{
				logger?.Debug($"Skipping Activity-related parameter '{parameter.Name}' from metrics.");
				continue;
			}

			TagOrBaggageAttributeRecord? tagAttribute = null;
			var destination = InstrumentParameterDestination.Unknown;
			if (Utilities.TryContainsAttribute(parameter, Constants.Shared.TagAttribute, token, out var attribute))
			{
				logger?.Debug($"Found explicit tag: {parameter.Name}.");
				destination = InstrumentParameterDestination.Tag;

				tagAttribute = SharedHelpers.GetTagOrBaggageAttribute(attribute!, semanticModel, logger, token);
			}
			else if (Utilities.ContainsAttribute(parameter, Constants.Metrics.InstrumentMeasurementAttribute, token))
			{
				logger?.Debug($"Found explicit instrument measurement: {parameter.Name}.");
				destination = InstrumentParameterDestination.Measurement;
			}

			var isFuncType = false;
			var isIEnumerableType = false;
			var isMeasurementType = false;
			var isValidInstrumentType = false;

			PurviewTypeInfo? instrumentType = null;
			if (destination != InstrumentParameterDestination.Tag)
			{
				if (parameter.Type is INamedTypeSymbol parameterType)
				{
					isFuncType =
						parameterType.ConstructedFrom.ToString() == Constants.System.Func.MakeGeneric(false, "TResult");
					if (isFuncType)
					{
						// For observable instruments.
						if (parameterType.TypeArguments[0] is INamedTypeSymbol typeArg)
						{
							isIEnumerableType = Constants.System.GenericIEnumerable.Equals(typeArg.ConstructedFrom);
							if (isIEnumerableType)
							{
								if (parameterType.TypeArguments[0] is INamedTypeSymbol enumerableType)
								{
									if (
										Constants.Metrics.SystemDiagnostics.Measurement.Equals(
											enumerableType.TypeArguments[0]
										)
									)
									{
										if (enumerableType.TypeArguments[0] is INamedTypeSymbol measurementType)
										{
											isMeasurementType = true;
											isValidInstrumentType = SharedHelpers.IsValidMeasurementValueType(
												measurementType.TypeArguments[0]
											);
											if (isValidInstrumentType)
											{
												instrumentType = PurviewTypeFactory.Create(
													measurementType.TypeArguments[0]
												);
												destination = InstrumentParameterDestination.Measurement;

												logger?.Debug(
													$"Found valid instrument type: Func -> IEnumerable -> Measurement -> {instrumentType}"
												);
											}
										}
									}
								}
							}
							else if (Constants.Metrics.SystemDiagnostics.Measurement.Equals(typeArg.ConstructedFrom))
							{
								isMeasurementType = true;
								isValidInstrumentType = SharedHelpers.IsValidMeasurementValueType(
									typeArg.TypeArguments[0]
								);
								if (isValidInstrumentType)
								{
									instrumentType = PurviewTypeFactory.Create(typeArg.TypeArguments[0]);
									destination = InstrumentParameterDestination.Measurement;

									logger?.Debug(
										$"Found valid instrument type: Func -> Measurement -> {instrumentType}"
									);
								}
							}
							else if (SharedHelpers.IsValidMeasurementValueType(typeArg))
							{
								isValidInstrumentType = true;

								instrumentType = PurviewTypeFactory.Create(typeArg);
								destination = InstrumentParameterDestination.Measurement;

								logger?.Debug($"Found valid instrument type: Func -> {instrumentType}");
							}
						}
						else
						{
							isValidInstrumentType = SharedHelpers.IsValidMeasurementValueType(
								parameterType.TypeArguments[0]
							);
							if (isValidInstrumentType)
							{
								instrumentType = PurviewTypeFactory.Create(parameterType.TypeArguments[0]);
								destination = InstrumentParameterDestination.Measurement;

								logger?.Debug($"Found valid instrument type: Func -> {instrumentType}");
							}
						}
					}
					else
					{
						// For non-observable instruments.
						isValidInstrumentType = SharedHelpers.IsValidMeasurementValueType(parameterType);
						if (isValidInstrumentType && !isAutoCounter)
						{
							instrumentType = PurviewTypeFactory.Create(parameterType);
							destination = InstrumentParameterDestination.Measurement;

							logger?.Debug($"Found valid instrument type: {instrumentType}");
						}
					}
				}
			}

			if (destination == InstrumentParameterDestination.Unknown)
			{
				logger?.Debug($"Unable to match parameter {parameter.Name}, inferring tag.");
				destination = InstrumentParameterDestination.Tag;
			}

			var parameterName = parameter.Name;
			var generatedName = GenerateParameterName(
				tagAttribute?.Name.Value ?? parameterName,
				null,
				lowercaseTagKeys,
				namingConvention
			);

			// Check for ExcludeTargetsAttribute
			var excludeTargets = SharedHelpers.GetExcludeTargetsAttribute(parameter, semanticModel, logger, token);

			parameterTargets.Add(
				new(
					ParameterName: parameterName,
					ParameterType: PurviewTypeFactory.Create(parameter.Type),
					IsFunc: isFuncType,
					IsIEnumerable: isIEnumerableType,
					IsMeasurement: isMeasurementType,
					IsValidInstrumentType: isValidInstrumentType,
					InstrumentType: instrumentType,
					GeneratedName: generatedName,
					ParamDestination: destination,
					SkipOnNullOrEmpty: GetSkipOnNullOrEmptyValue(tagAttribute),
					ExcludedTargets: excludeTargets?.ExcludedTargets ?? GenerationType.None
				)
			);
		}

		return [.. parameterTargets];
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
			meterGenerationAttribute?.InstrumentSeparator.Or(Constants.Metrics.InstrumentSeparatorDefault)
			?? Constants.Metrics.InstrumentSeparatorDefault;

		if (meterAttribute.IncludeAssemblyInstrumentPrefix.Value == true)
		{
			if (
				meterGenerationAttribute?.InstrumentPrefix.IsSet == true
				&& !string.IsNullOrWhiteSpace(meterGenerationAttribute?.InstrumentPrefix.Value)
			)
			{
				prefix = meterGenerationAttribute!.InstrumentPrefix.Value! + separator;
			}
		}

		// Check if interface-level prefix is explicitly set
		if (!string.IsNullOrWhiteSpace(meterAttribute.InstrumentPrefix.Value))
		{
			prefix += meterAttribute.InstrumentPrefix.Value! + separator;
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
