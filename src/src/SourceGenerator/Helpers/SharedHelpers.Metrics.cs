using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class SharedHelpers
{
	public static MeterGenerationAttributeRecord? GetMeterGenerationAttribute(
		SemanticModel semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token
	) => GetMeterGenerationAttribute(semanticModel.Compilation.Assembly, semanticModel, logger, token);

	public static MeterAttributeRecord? GetMeterAttribute(
		ISymbol symbol,
		SemanticModel? semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				TemplateLibrary.Metrics.MeterAttribute,
				token,
				out var attributeData
			)
		)
		{
			return null;
		}

		return new(
			Name: GetAttributeStringValue(attributeData!, "name"),
			InstrumentPrefix: GetAttributeStringValue(attributeData!, "instrumentPrefix"),
			IncludeAssemblyInstrumentPrefix: GetAttributeValue<bool>(
				attributeData!,
				"includeAssemblyInstrumentPrefix",
				true
			),
			LowercaseInstrumentName: GetAttributeValue<bool>(
				attributeData!,
				"lowercaseInstrumentName",
				PropertyLibrary.Metrics.LowercaseInstrumentNameDefault
			),
			LowercaseTagKeys: GetAttributeValue<bool>(
				attributeData!,
				"lowercaseTagKeys",
				PropertyLibrary.Metrics.LowercaseTagKeysDefault
			)
		);
	}

	public static MeterGenerationAttributeRecord? GetMeterGenerationAttribute(
		ISymbol symbol,
		SemanticModel? semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				TemplateLibrary.Metrics.MeterGenerationAttribute,
				token,
				out var attributeData
			)
		)
		{
			return null;
		}

		return new(
			InstrumentPrefix: GetAttributeStringValue(attributeData!, "instrumentPrefix"),
			InstrumentSeparator: GetAttributeStringValue(
				attributeData!,
				"instrumentSeparator",
				PropertyLibrary.Metrics.InstrumentSeparatorDefault
			),
			LowercaseInstrumentName: GetAttributeValue<bool>(
				attributeData!,
				"lowercaseInstrumentName",
				PropertyLibrary.Metrics.LowercaseInstrumentNameDefault
			),
			LowercaseTagKeys: GetAttributeValue<bool>(
				attributeData!,
				"lowercaseTagKeys",
				PropertyLibrary.Metrics.LowercaseTagKeysDefault
			),
			MeterName: GetAttributeStringValue(attributeData!, "meterName"),
			MeterNameGenerationType: GetAttributeValue<int>(attributeData!, "meterNameGenerationType", 1)
		);
	}

	public static InstrumentAttributeRecord? GetInstrumentAttribute(
		ISymbol symbol,
		SemanticModel? semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		AttributeData? attributeData = null;
		foreach (var instrumentAttribute in TemplateLibrary.Metrics.ValidInstrumentAttributes)
		{
			if (Utilities.TryContainsAttribute(symbol, instrumentAttribute, token, out attributeData))
			{
				break;
			}
		}

		if (attributeData?.AttributeClass == null)
			return null;

		var autoIncrement = GetAttributeValue<bool>(attributeData, "autoIncrement");
		InstrumentTypes instrumentType;
		var attributeType = TypeReference.Create(attributeData.AttributeClass);
		var isAutoCounter = TemplateLibrary.Metrics.AutoCounterAttribute == attributeType;
		if (isAutoCounter || TemplateLibrary.Metrics.CounterAttribute == attributeType)
		{
			instrumentType = InstrumentTypes.Counter;

			if (isAutoCounter)
				autoIncrement = new(true);
		}
		else if (TemplateLibrary.Metrics.HistogramAttribute == attributeType)
		{
			instrumentType = InstrumentTypes.Histogram;
		}
		else if (TemplateLibrary.Metrics.UpDownCounterAttribute == attributeType)
		{
			instrumentType = InstrumentTypes.UpDownCounter;
		}
		else if (TemplateLibrary.Metrics.ObservableCounterAttribute == attributeType)
		{
			instrumentType = InstrumentTypes.ObservableCounter;
		}
		else if (TemplateLibrary.Metrics.ObservableUpDownCounterAttribute == attributeType)
		{
			instrumentType = InstrumentTypes.ObservableUpDownCounter;
		}
		else if (TemplateLibrary.Metrics.ObservableGaugeAttribute == attributeType)
		{
			instrumentType = InstrumentTypes.ObservableGauge;
		}
		else
		{
			logger?.Fatal($"Unknown instrument type {attributeType}.");
			return null;
		}

		return new(
			Name: GetAttributeStringValue(attributeData, "name"),
			Unit: GetAttributeStringValue(attributeData, "unit"),
			Description: GetAttributeStringValue(attributeData, "description"),
			AutoIncrement: autoIncrement,
			ThrowOnAlreadyInitialized: GetAttributeValue<bool>(attributeData, "throwOnAlreadyInitialized"),
			InstrumentType: instrumentType
		);
	}

	public static bool IsValidMeasurementValueType(ITypeSymbol type) =>
		Array.Exists(PropertyLibrary.Metrics.ValidMeasurementSpecialTypes, m => m == type.SpecialType);
}
