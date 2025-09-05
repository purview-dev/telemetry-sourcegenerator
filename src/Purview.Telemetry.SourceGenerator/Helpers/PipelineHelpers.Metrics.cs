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
			logger?.Error(
				$"Could not find interface syntax from the target node '{context.TargetNode.Flatten()}'."
			);
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
			return MeterTarget.Failed(
				TelemetryDiagnostics.General.GenericInterfacesNotSupported,
				interfaceSymbol.Locations
			);
		}

		// If multi-target generation is enabled and this interface contains [Telemetry] methods,
		// skip single-target metrics generation to avoid mixed outputs.
		{
			var assembly = context.SemanticModel.Compilation.Assembly;
			foreach (
				var m in GetAllInterfaceMethods(
					interfaceSymbol,
					context.SemanticModel.Compilation,
					token
				)
			)
			{
				var mt = Utilities.GetMultiTargetConfiguration(m, assembly);
				if (mt.IsMultiTargetEnabled)
				{
					logger?.Debug(
						$"Skipping Meter generation for {interfaceSymbol.Name} due to multi-target usage."
					);
					return null;
				}
			}
		}

		var semanticModel = context.SemanticModel;
		var meterAttribute = SharedHelpers.GetMeterAttribute(
			context.TargetSymbol,
			semanticModel,
			logger,
			token
		);
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
		var className = telemetryGeneration.ClassName.IsSet
			? telemetryGeneration.ClassName.Value!
			: GenerateClassName(interfaceSymbol.Name);

		var generationType = SharedHelpers.GetGenerationTypes(interfaceSymbol, token);
		var meterGenerationAttribute = SharedHelpers.GetMeterGenerationAttribute(
			semanticModel,
			logger,
			token
		);
		var fullNamespace = Utilities.GetFullNamespace(interfaceDeclaration, true);
		var instrumentMethods = BuildInstrumentationMethods(
			generationType,
			meterAttribute,
			meterGenerationAttribute,
			semanticModel,
			interfaceSymbol,
			logger,
			token,
			out var methodDiagnostics
		);

		var meterName = meterAttribute.Name.Value;
		if (string.IsNullOrWhiteSpace(meterName))
		{
			meterName = interfaceSymbol.Name;
			if (meterName[0] == 'I')
				meterName = meterName.Substring(1);
		}

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
			InstrumentationMethods: instrumentMethods,
			DuplicateMethods: BuildDuplicateMethods(interfaceSymbol, semanticModel, token),
			Failures: methodDiagnostics?.ToImmutableArray()
		);
	}

	static ImmutableArray<InstrumentTarget> BuildInstrumentationMethods(
		GenerationType generationType,
		MeterAttributeRecord meterAttribute,
		MeterGenerationAttributeRecord? meterGenerationAttribute,
		SemanticModel semanticModel,
		INamedTypeSymbol interfaceSymbol,
		GenerationLogger? logger,
		CancellationToken token,
		out (TelemetryDiagnosticDescriptor, ImmutableArray<Location>)[]? methodDiagnostics
	)
	{
		token.ThrowIfCancellationRequested();

		List<(TelemetryDiagnosticDescriptor, ImmutableArray<Location>)>? methodDiagnosticsList =
			null;
		var lowercaseInstrumentName = meterAttribute.LowercaseInstrumentName.IsSet
			? meterAttribute.LowercaseInstrumentName.Value!.Value
			: (meterGenerationAttribute?.LowercaseInstrumentName?.IsSet) != true
				|| meterGenerationAttribute.LowercaseInstrumentName.Value!.Value;

		var prefix = GeneratePrefix(meterGenerationAttribute, meterAttribute, token);
		var lowercaseTagKeys = meterAttribute.LowercaseTagKeys!.Value!.Value;

		List<InstrumentTarget> methodTargets = [];
		foreach (
			var method in GetAllInterfaceMethods(interfaceSymbol, semanticModel.Compilation, token)
		)
		{
			token.ThrowIfCancellationRequested();

			if (Utilities.ContainsAttribute(method, Constants.Shared.ExcludeAttribute, token))
			{
				logger?.Debug(
					$"Skipping {interfaceSymbol.Name}.{method.Name}, explicitly excluded."
				);
				continue;
			}

			if (method.Arity > 0)
			{
				methodDiagnosticsList ??= [];
				methodDiagnosticsList.Add(
					(TelemetryDiagnostics.General.GenericMethodsNotSupported, method.Locations)
				);
				continue;
			}

			logger?.Debug(
				$"Found possible instrument method {interfaceSymbol.Name}.{method.Name}."
			);

			var instrumentAttribute = SharedHelpers.GetInstrumentAttribute(
				method,
				semanticModel,
				logger,
				token
			);
			var validAutoCounter =
				instrumentAttribute?.InstrumentType is InstrumentTypes.Counter
				&& instrumentAttribute.IsAutoIncrement;

			var parameters = GetInstrumentParameters(
				method,
				lowercaseTagKeys,
				validAutoCounter,
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

			var fieldName = $"_{Utilities.LowerCaseFirstChar(method.Name)}Instrument";
			var instrumentName = instrumentAttribute?.Name?.Value;
			if (string.IsNullOrWhiteSpace(instrumentName))
				instrumentName = method.Name;

			if (lowercaseInstrumentName)
			{
#pragma warning disable CA1308 // Normalize strings to uppercase
				instrumentName = instrumentName!.ToLowerInvariant();
				prefix = prefix?.ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
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
					methodDiagnosticsList ??= [];
					methodDiagnosticsList.Add(
						(
							TelemetryDiagnostics.General.MultiGenerationTargetsNotSupported,
							method.Locations
						)
					);
				}
				else if (targetGenerationState.RaiseInferenceNotSupportedWithMultiTargeting)
				{
					logger?.Debug(
						$"Identified {interfaceSymbol.Name}.{method.Name} as problematic as it is inferred."
					);
					methodDiagnosticsList ??= [];
					methodDiagnosticsList.Add(
						(
							TelemetryDiagnostics.General.InferenceNotSupportedWithMultiTargeting,
							method.Locations
						)
					);
				}
			}
			else
			{
				if (instrumentAttribute == null)
				{
					logger?.Warning("Missing instrument attribute.");
					methodDiagnosticsList ??= [];
					methodDiagnosticsList.Add(
						(TelemetryDiagnostics.Metrics.NoInstrumentDefined, method.Locations)
					);
				}
				else if (!validAutoCounter && measurementParameter == null)
				{
					methodDiagnosticsList ??= [];
					methodDiagnosticsList.Add(
						(TelemetryDiagnostics.Metrics.NoMeasurementValueDefined, method.Locations)
					);
				}
				else
				{
					if (validAutoCounter)
					{
						if (measurementParameters.Length > 0)
						{
							methodDiagnosticsList ??= [];
							methodDiagnosticsList.Add(
								(
									TelemetryDiagnostics
										.Metrics
										.AutoIncrementCountAndMeasurementParam,
									measurementParameters
										.SelectMany(m => m.Locations)
										.ToImmutableArray()
								)
							);
						}
					}
					else
					{
						// Validate the parameters and type.
						if (instrumentAttribute.IsObservable)
						{
							if (!measurementParameter!.IsFunc)
							{
								methodDiagnosticsList ??= [];
								methodDiagnosticsList.Add(
									(
										TelemetryDiagnostics.Metrics.ObservableRequiredFunc,
										measurementParameter.Locations
									)
								);
							}
						}
						else
						{
							if (measurementParameters.Length != 1)
							{
								methodDiagnosticsList ??= [];
								methodDiagnosticsList.Add(
									(
										TelemetryDiagnostics
											.Metrics
											.MoreThanOneMeasurementValueDefined,
										measurementParameters
											.SelectMany(m => m.Locations)
											.ToImmutableArray()
									)
								);
							}
						}
					}
				}

				if (instrumentAttribute != null)
				{
					if (!method.ReturnsVoid && !returnsBool)
					{
						methodDiagnosticsList ??= [];
						methodDiagnosticsList.Add(
							(
								TelemetryDiagnostics.Metrics.DoesNotReturnVoid,
								method.ReturnType.Locations
							)
						);
					}
				}
			}

			var instrumentMeasurementType =
				measurementParameter?.InstrumentType ?? Constants.System.BuiltInTypes.Int32;
			if (measurementParameter?.IsValidInstrumentType == false)
			{
				methodDiagnosticsList ??= [];
				methodDiagnosticsList.Add(
					(
						TelemetryDiagnostics.Metrics.InvalidMeasurementType,
						measurementParameter.Locations
					)
				);
			}

			methodTargets.Add(
				new(
					MethodName: method.Name,
					ReturnType: PurviewTypeFactory.Create(method.ReturnType),
					ReturnsBool: returnsBool,
					IsNullableReturn: method.ReturnType.NullableAnnotation
						== NullableAnnotation.Annotated,
					FieldName: fieldName,
					InstrumentMeasurementType: instrumentMeasurementType,
					IsObservable: instrumentAttribute?.IsObservable == true,
					MetricName: prefix + instrumentName!,
					Locations: method.Locations,
					InstrumentAttribute: instrumentAttribute,
					Parameters: parameters,
					Tags: tagParameters,
					MeasurementParameter: measurementParameter,
					TargetGenerationState: targetGenerationState
				)
			);
		}

		methodDiagnostics = methodDiagnosticsList?.ToArray();

		return [.. methodTargets];
	}

	static ImmutableArray<InstrumentParameterTarget> GetInstrumentParameters(
		IMethodSymbol method,
		bool lowercaseTagKeys,
		bool isAutoCounter,
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		List<InstrumentParameterTarget> parameterTargets = [];
		foreach (var parameter in method.Parameters)
		{
			token.ThrowIfCancellationRequested();

			TagOrBaggageAttributeRecord? tagAttribute = null;
			var destination = InstrumentParameterDestination.Unknown;
			if (
				Utilities.TryContainsAttribute(
					parameter,
					Constants.Shared.TagAttribute,
					token,
					out var attribute
				)
			)
			{
				logger?.Debug($"Found explicit tag: {parameter.Name}.");
				destination = InstrumentParameterDestination.Tag;

				tagAttribute = SharedHelpers.GetTagOrBaggageAttribute(
					attribute!,
					semanticModel,
					logger,
					token
				);
			}
			else if (
				Utilities.ContainsAttribute(
					parameter,
					Constants.Metrics.InstrumentMeasurementAttribute,
					token
				)
			)
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
						parameterType.ConstructedFrom.ToString()
						== Constants.System.Func.MakeGeneric(false, "TResult");
					if (isFuncType)
					{
						// For observable instruments.
						if (parameterType.TypeArguments[0] is INamedTypeSymbol typeArg)
						{
							isIEnumerableType = Constants.System.GenericIEnumerable.Equals(
								typeArg.ConstructedFrom
							);
							if (isIEnumerableType)
							{
								if (
									parameterType.TypeArguments[0]
									is INamedTypeSymbol enumerableType
								)
								{
									if (
										Constants.Metrics.SystemDiagnostics.Measurement.Equals(
											enumerableType.TypeArguments[0]
										)
									)
									{
										if (
											enumerableType.TypeArguments[0]
											is INamedTypeSymbol measurementType
										)
										{
											isMeasurementType = true;
											isValidInstrumentType =
												SharedHelpers.IsValidMeasurementValueType(
													measurementType.TypeArguments[0]
												);
											if (isValidInstrumentType)
											{
												instrumentType = PurviewTypeFactory.Create(
													measurementType.TypeArguments[0]
												);
												destination =
													InstrumentParameterDestination.Measurement;

												logger?.Debug(
													$"Found valid instrument type: Func -> IEnumerable -> Measurement -> {instrumentType}"
												);
											}
										}
									}
								}
							}
							else if (
								Constants.Metrics.SystemDiagnostics.Measurement.Equals(
									typeArg.ConstructedFrom
								)
							)
							{
								isMeasurementType = true;
								isValidInstrumentType = SharedHelpers.IsValidMeasurementValueType(
									typeArg.TypeArguments[0]
								);
								if (isValidInstrumentType)
								{
									instrumentType = PurviewTypeFactory.Create(
										typeArg.TypeArguments[0]
									);
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

								logger?.Debug(
									$"Found valid instrument type: Func -> {instrumentType}"
								);
							}
						}
						else
						{
							isValidInstrumentType = SharedHelpers.IsValidMeasurementValueType(
								parameterType.TypeArguments[0]
							);
							if (isValidInstrumentType)
							{
								instrumentType = PurviewTypeFactory.Create(
									parameterType.TypeArguments[0]
								);
								destination = InstrumentParameterDestination.Measurement;

								logger?.Debug(
									$"Found valid instrument type: Func -> {instrumentType}"
								);
							}
						}
					}
					else
					{
						// For non-observable instruments.
						isValidInstrumentType = SharedHelpers.IsValidMeasurementValueType(
							parameterType
						);
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
				lowercaseTagKeys
			);

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
					Locations: parameter.Locations
				)
			);
		}

		return [.. parameterTargets];
	}

	static string? GeneratePrefix(
		MeterGenerationAttributeRecord? meterGenerationAttribute,
		MeterAttributeRecord meterAttribute,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		string? prefix = null;
		var separator = meterGenerationAttribute?.InstrumentSeparator.Or(
			Constants.Metrics.InstrumentSeparatorDefault
		);

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

		if (!string.IsNullOrWhiteSpace(meterAttribute.InstrumentPrefix.Value))
			prefix += meterAttribute.InstrumentPrefix.Value! + separator;

		return prefix;
	}
}
