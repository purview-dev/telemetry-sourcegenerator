using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class SharedHelpers
{
	public static MeterGenerationAttributeRecord? GetMeterGenerationAttribute(
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken token
	) =>
		GetMeterGenerationAttribute(
			semanticModel.Compilation.Assembly,
			semanticModel,
			logger,
			token
		);

	public static MeterAttributeRecord? GetMeterAttribute(
		ISymbol symbol,
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				Constants.Metrics.MeterAttribute,
				token,
				out var attributeData
			)
		)
		{
			return null;
		}

		AttributeStringValue? nameValue = null;
		AttributeStringValue? instrumentPrefix = null;
		AttributeValue<bool>? includeAssemblyInstrumentPrefix = null;
		AttributeValue<bool>? lowercaseInstrumentName = null;
		AttributeValue<bool>? lowercaseTagKeys = null;

		if (
			!AttributeParser(
				attributeData!,
				(name, value) =>
				{
					if (
						name.Equals(
							nameof(MeterAttributeRecord.Name),
							StringComparison.OrdinalIgnoreCase
						)
					)
					{
						nameValue = new((string)value);
					}
					else if (
											name.Equals(
												nameof(MeterAttributeRecord.InstrumentPrefix),
												StringComparison.OrdinalIgnoreCase
											)
										)
					{
						instrumentPrefix = new((string)value);
					}
					else if (
											name.Equals(
												nameof(MeterAttributeRecord.IncludeAssemblyInstrumentPrefix),
												StringComparison.OrdinalIgnoreCase
											)
										)
					{
						includeAssemblyInstrumentPrefix = new((bool)value);
					}
					else if (
											name.Equals(
												nameof(MeterAttributeRecord.LowercaseInstrumentName),
												StringComparison.OrdinalIgnoreCase
											)
										)
					{
						lowercaseInstrumentName = new((bool)value);
					}
					else if (
											name.Equals(
												nameof(MeterAttributeRecord.LowercaseTagKeys),
												StringComparison.OrdinalIgnoreCase
											)
										)
					{
						lowercaseTagKeys = new((bool)value);
					}
				},
				semanticModel,
				logger,
				token
			)
		)
		{
			// Failed to parse correctly, so null it out.
			return null;
		}

		return new(
			Name: nameValue ?? new(),
			InstrumentPrefix: instrumentPrefix ?? new(),
			IncludeAssemblyInstrumentPrefix: includeAssemblyInstrumentPrefix ?? new(true),
			LowercaseInstrumentName: lowercaseInstrumentName
				?? new(Constants.Metrics.LowercaseInstrumentNameDefault),
			LowercaseTagKeys: lowercaseTagKeys ?? new(Constants.Metrics.LowercaseTagKeysDefault)
		);
	}

	public static MeterGenerationAttributeRecord? GetMeterGenerationAttribute(
		ISymbol symbol,
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				Constants.Metrics.MeterGenerationAttribute,
				token,
				out var attributeData
			)
		)
		{
			return null;
		}

		AttributeStringValue? instrumentPrefix = null;
		AttributeStringValue? instrumentSeparator = null;
		AttributeValue<bool>? lowercaseInstrumentName = null;
		AttributeValue<bool>? lowercaseTagKeys = null;

		if (
			!AttributeParser(
				attributeData!,
				(name, value) =>
				{
					if (
						name.Equals(
							nameof(MeterGenerationAttributeRecord.InstrumentPrefix),
							StringComparison.OrdinalIgnoreCase
						)
					)
					{
						instrumentPrefix = new((string)value);
					}
					else if (
											name.Equals(
												nameof(MeterGenerationAttributeRecord.InstrumentSeparator),
												StringComparison.OrdinalIgnoreCase
											)
										)
					{
						instrumentSeparator = new((string)value);
					}
					else if (
											name.Equals(
												nameof(MeterGenerationAttributeRecord.LowercaseInstrumentName),
												StringComparison.OrdinalIgnoreCase
											)
										)
					{
						lowercaseInstrumentName = new((bool)value);
					}
					else if (
											name.Equals(
												nameof(MeterGenerationAttributeRecord.LowercaseTagKeys),
												StringComparison.OrdinalIgnoreCase
											)
										)
					{
						lowercaseTagKeys = new((bool)value);
					}
				},
				semanticModel,
				logger,
				token
			)
		)
		{
			// Failed to parse correctly, so null it out.
			return null;
		}

		return new(
			InstrumentPrefix: instrumentPrefix ?? new(),
			InstrumentSeparator: instrumentSeparator
				?? new(Constants.Metrics.InstrumentSeparatorDefault),
			LowercaseInstrumentName: lowercaseInstrumentName
				?? new(Constants.Metrics.LowercaseInstrumentNameDefault),
			LowercaseTagKeys: lowercaseTagKeys ?? new(Constants.Metrics.LowercaseTagKeysDefault)
		);
	}

	public static InstrumentAttributeRecord? GetInstrumentAttribute(
		ISymbol symbol,
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		AttributeData? attributeData = null;
		foreach (var instrumentAttribute in Constants.Metrics.ValidInstrumentAttributes)
		{
			if (
				Utilities.TryContainsAttribute(
					symbol,
					instrumentAttribute,
					token,
					out attributeData
				)
			)
			{
				break;
			}
		}

		if (attributeData?.AttributeClass == null)
			return null;

		AttributeStringValue? nameValue = null;
		AttributeStringValue? unit = null;
		AttributeStringValue? description = null;
		AttributeValue<bool>? autoIncrement = null;
		AttributeValue<bool>? throwOnAlreadyInitialized = null;

		if (
			!AttributeParser(
				attributeData,
				(name, value) =>
				{
					if (
						name.Equals(
							nameof(InstrumentAttributeRecord.Name),
							StringComparison.OrdinalIgnoreCase
						)
					)
					{
						nameValue = new((string)value);
					}
					else if (
											name.Equals(
												nameof(InstrumentAttributeRecord.Unit),
												StringComparison.OrdinalIgnoreCase
											)
										)
					{
						unit = new((string)value);
					}
					else if (
											name.Equals(
												nameof(InstrumentAttributeRecord.Description),
												StringComparison.OrdinalIgnoreCase
											)
										)
					{
						description = new((string)value);
					}
					else if (
											name.Equals(
												nameof(InstrumentAttributeRecord.AutoIncrement),
												StringComparison.OrdinalIgnoreCase
											)
										)
					{
						autoIncrement = new((bool)value);
					}
					else if (
											name.Equals(
												nameof(InstrumentAttributeRecord.ThrowOnAlreadyInitialized),
												StringComparison.OrdinalIgnoreCase
											)
										)
					{
						throwOnAlreadyInitialized = new((bool)value);
					}
				},
				semanticModel,
				logger,
				token
			)
		)
		{
			// Failed to parse correctly, so null it out.
			return null;
		}

		InstrumentTypes instrumentType;
		var attributeType = PurviewTypeFactory.Create(attributeData.AttributeClass);
		var isAutoCounter = Constants.Metrics.AutoCounterAttribute == attributeType;
		if (isAutoCounter || Constants.Metrics.CounterAttribute == attributeType)
		{
			instrumentType = InstrumentTypes.Counter;

			if (isAutoCounter)
				autoIncrement = new(true);
		}
		else if (Constants.Metrics.HistogramAttribute == attributeType)
		{
			instrumentType = InstrumentTypes.Histogram;
		}
		else if (Constants.Metrics.UpDownCounterAttribute == attributeType)
		{
			instrumentType = InstrumentTypes.UpDownCounter;
		}
		else if (Constants.Metrics.ObservableCounterAttribute == attributeType)
		{
			instrumentType = InstrumentTypes.ObservableCounter;
		}
		else if (Constants.Metrics.ObservableUpDownCounterAttribute == attributeType)
		{
			instrumentType = InstrumentTypes.ObservableUpDownCounter;
		}
		else if (Constants.Metrics.ObservableGaugeAttribute == attributeType)
		{
			instrumentType = InstrumentTypes.ObservableGauge;
		}
		else
		{
			logger?.Error($"Unknown instrument type {attributeType}.");
			return null;
		}

		return new(
			Name: nameValue ?? new(),
			Unit: unit ?? new(),
			Description: description ?? new(),
			AutoIncrement: autoIncrement ?? new(),
			ThrowOnAlreadyInitialized: throwOnAlreadyInitialized ?? new(),
			InstrumentType: instrumentType
		);
	}

	public static bool IsValidMeasurementValueType(ITypeSymbol type) =>
		Array.FindIndex(Constants.Metrics.ValidMeasurementSpecialTypes, m => m == type.SpecialType)
		> -1;
}
