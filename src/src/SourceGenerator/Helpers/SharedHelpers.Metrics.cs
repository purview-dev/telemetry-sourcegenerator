using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class SharedHelpers
{
	public static MeterGenerationAttributeData? GetMeterGenerationAttribute(
		Compilation compilation,
		CancellationToken token
	) => GetMeterGenerationAttribute(compilation.Assembly, token);

	public static MeterAttributeData? GetMeterAttribute(ISymbol symbol, CancellationToken token)
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

		var data = MeterAttributeData.FromAttributeData(attributeData!);
		return data.Exists
			? data with
			{
				Name = NullIfWhitespace(data.Name),
				InstrumentPrefix = NullIfWhitespace(data.InstrumentPrefix),
			}
			: null;
	}

	public static MeterGenerationAttributeData? GetMeterGenerationAttribute(ISymbol symbol, CancellationToken token)
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

		var data = MeterGenerationAttributeData.FromAttributeData(attributeData!);
		return data.Exists
			? data with
			{
				InstrumentPrefix = NullIfWhitespace(data.InstrumentPrefix),
				InstrumentSeparator = NullIfWhitespace(data.InstrumentSeparator),
				MeterName = NullIfWhitespace(data.MeterName),
			}
			: null;
	}

	public static InstrumentAttributeRecord? GetInstrumentAttribute(
		ISymbol symbol,
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

		var attributeType = TypeReference.Create(attributeData.AttributeClass);

		var record = attributeType switch
		{
			_ when TemplateLibrary.Metrics.CounterAttribute == attributeType => ToRecord(
				CounterAttributeData.FromAttributeData(attributeData),
				InstrumentTypes.Counter
			),
			_ when TemplateLibrary.Metrics.AutoCounterAttribute == attributeType => ToRecord(
				AutoCounterAttributeData.FromAttributeData(attributeData),
				InstrumentTypes.Counter
			),
			_ when TemplateLibrary.Metrics.UpDownCounterAttribute == attributeType => ToRecord(
				UpDownCounterAttributeData.FromAttributeData(attributeData),
				InstrumentTypes.UpDownCounter
			),
			_ when TemplateLibrary.Metrics.HistogramAttribute == attributeType => ToRecord(
				HistogramAttributeData.FromAttributeData(attributeData),
				InstrumentTypes.Histogram
			),
			_ when TemplateLibrary.Metrics.ObservableCounterAttribute == attributeType => ToRecord(
				ObservableCounterAttributeData.FromAttributeData(attributeData),
				InstrumentTypes.ObservableCounter
			),
			_ when TemplateLibrary.Metrics.ObservableUpDownCounterAttribute == attributeType => ToRecord(
				ObservableUpDownCounterAttributeData.FromAttributeData(attributeData),
				InstrumentTypes.ObservableUpDownCounter
			),
			_ when TemplateLibrary.Metrics.ObservableGaugeAttribute == attributeType => ToRecord(
				ObservableGaugeAttributeData.FromAttributeData(attributeData),
				InstrumentTypes.ObservableGauge
			),
			_ => null,
		};

		if (record is null)
			logger?.Fatal($"Unknown instrument type {attributeType}.");

		return record;
	}

	static InstrumentAttributeRecord ToRecord(
		string? name,
		string? unit,
		string? description,
		bool autoIncrement,
		bool throwOnAlreadyInitialized,
		InstrumentTypes instrumentType
	) =>
		new(
			Name: NullIfWhitespace(name),
			Unit: NullIfWhitespace(unit),
			Description: NullIfWhitespace(description),
			AutoIncrement: autoIncrement,
			ThrowOnAlreadyInitialized: throwOnAlreadyInitialized,
			InstrumentType: instrumentType
		);

	static InstrumentAttributeRecord ToRecord(CounterAttributeData data, InstrumentTypes type) =>
		ToRecord(data.Name, data.Unit, data.Description, data.AutoIncrement, false, type);

	static InstrumentAttributeRecord ToRecord(AutoCounterAttributeData data, InstrumentTypes type) =>
		ToRecord(data.Name, data.Unit, data.Description, true, false, type);

	static InstrumentAttributeRecord ToRecord(UpDownCounterAttributeData data, InstrumentTypes type) =>
		ToRecord(data.Name, data.Unit, data.Description, false, false, type);

	static InstrumentAttributeRecord ToRecord(HistogramAttributeData data, InstrumentTypes type) =>
		ToRecord(data.Name, data.Unit, data.Description, false, false, type);

	static InstrumentAttributeRecord ToRecord(ObservableCounterAttributeData data, InstrumentTypes type) =>
		ToRecord(data.Name, data.Unit, data.Description, false, data.ThrowOnAlreadyInitialized, type);

	static InstrumentAttributeRecord ToRecord(ObservableUpDownCounterAttributeData data, InstrumentTypes type) =>
		ToRecord(data.Name, data.Unit, data.Description, false, data.ThrowOnAlreadyInitialized, type);

	static InstrumentAttributeRecord ToRecord(ObservableGaugeAttributeData data, InstrumentTypes type) =>
		ToRecord(data.Name, data.Unit, data.Description, false, data.ThrowOnAlreadyInitialized, type);

	public static bool IsValidMeasurementValueType(ITypeSymbol type) =>
		Array.Exists(PropertyLibrary.Metrics.ValidMeasurementSpecialTypes, m => m == type.SpecialType);
}
