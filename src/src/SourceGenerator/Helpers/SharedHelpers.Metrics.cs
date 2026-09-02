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
		if (!Utilities.TryContainsAttribute(symbol, TypeLibrary.Metrics.MeterAttribute, token, out var attributeData))
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
				TypeLibrary.Metrics.MeterGenerationAttribute,
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

	public static InstrumentAttributeRecord? GetInstrumentAttribute(ISymbol symbol, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		if (CounterAttributeData.TryFromAttributeData(symbol, out var counterAttribute))
			return ToRecord(counterAttribute, InstrumentTypes.Counter);
		if (AutoCounterAttributeData.TryFromAttributeData(symbol, out var autoCounterAttribute))
			return ToRecord(autoCounterAttribute, InstrumentTypes.Counter);
		if (UpDownCounterAttributeData.TryFromAttributeData(symbol, out var upDownCounterAttribute))
			return ToRecord(upDownCounterAttribute, InstrumentTypes.UpDownCounter);
		if (HistogramAttributeData.TryFromAttributeData(symbol, out var histogramAttribute))
			return ToRecord(histogramAttribute, InstrumentTypes.Histogram);
		if (ObservableCounterAttributeData.TryFromAttributeData(symbol, out var observableCounterAttribute))
			return ToRecord(observableCounterAttribute, InstrumentTypes.ObservableCounter);
		if (ObservableUpDownCounterAttributeData.TryFromAttributeData(symbol, out var observableUpDownCounterAttribute))
			return ToRecord(observableUpDownCounterAttribute, InstrumentTypes.ObservableUpDownCounter);
		if (ObservableGaugeAttributeData.TryFromAttributeData(symbol, out var observableGaugeAttribute))
			return ToRecord(observableGaugeAttribute, InstrumentTypes.ObservableGauge);

		// No matching instrument attribute found
		return null;
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
		PropertyLibrary.Metrics.ValidMeasurementKeywordTypes.Any(m => m.SpecialType == type.SpecialType);
}
