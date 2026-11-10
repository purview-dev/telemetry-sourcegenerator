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
				TypeLibrary.Purview.Telemetry.MeterAttribute,
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
				TypeLibrary.Purview.Telemetry.MeterGenerationAttribute,
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

		// The attribute present decides the instrument, so the record this returns is already the right case.
		if (CounterAttributeData.TryFromAttributeData(symbol, out var counterAttribute))
			return new CounterInstrumentAttributeRecord(
				NullIfWhitespace(counterAttribute.Name),
				NullIfWhitespace(counterAttribute.Unit),
				NullIfWhitespace(counterAttribute.Description),
				counterAttribute.AutoIncrement
			);
		if (AutoCounterAttributeData.TryFromAttributeData(symbol, out var autoCounterAttribute))
			return new CounterInstrumentAttributeRecord(
				NullIfWhitespace(autoCounterAttribute.Name),
				NullIfWhitespace(autoCounterAttribute.Unit),
				NullIfWhitespace(autoCounterAttribute.Description),
				AutoIncrement: true
			);
		if (UpDownCounterAttributeData.TryFromAttributeData(symbol, out var upDownCounterAttribute))
			return new UpDownCounterInstrumentAttributeRecord(
				NullIfWhitespace(upDownCounterAttribute.Name),
				NullIfWhitespace(upDownCounterAttribute.Unit),
				NullIfWhitespace(upDownCounterAttribute.Description)
			);
		if (HistogramAttributeData.TryFromAttributeData(symbol, out var histogramAttribute))
		{
			var boundaries = ParseHistogramBucketBoundaries(histogramAttribute.HistogramBucketBoundaries);

			return new HistogramInstrumentAttributeRecord(
				NullIfWhitespace(histogramAttribute.Name),
				NullIfWhitespace(histogramAttribute.Unit),
				NullIfWhitespace(histogramAttribute.Description),
				HistogramBucketBoundaries: boundaries,
				InvalidHistogramBucketBoundaries: boundaries is null
					&& !string.IsNullOrWhiteSpace(histogramAttribute.HistogramBucketBoundaries)
			);
		}
		if (GaugeAttributeData.TryFromAttributeData(symbol, out var gaugeAttribute))
			return new GaugeInstrumentAttributeRecord(
				NullIfWhitespace(gaugeAttribute.Name),
				NullIfWhitespace(gaugeAttribute.Unit),
				NullIfWhitespace(gaugeAttribute.Description)
			);
		if (ObservableCounterAttributeData.TryFromAttributeData(symbol, out var observableCounterAttribute))
			return new ObservableCounterInstrumentAttributeRecord(
				NullIfWhitespace(observableCounterAttribute.Name),
				NullIfWhitespace(observableCounterAttribute.Unit),
				NullIfWhitespace(observableCounterAttribute.Description),
				observableCounterAttribute.ThrowOnAlreadyInitialized
			);
		if (ObservableUpDownCounterAttributeData.TryFromAttributeData(symbol, out var observableUpDownCounterAttribute))
			return new ObservableUpDownCounterInstrumentAttributeRecord(
				NullIfWhitespace(observableUpDownCounterAttribute.Name),
				NullIfWhitespace(observableUpDownCounterAttribute.Unit),
				NullIfWhitespace(observableUpDownCounterAttribute.Description),
				observableUpDownCounterAttribute.ThrowOnAlreadyInitialized
			);
		if (ObservableGaugeAttributeData.TryFromAttributeData(symbol, out var observableGaugeAttribute))
			return new ObservableGaugeInstrumentAttributeRecord(
				NullIfWhitespace(observableGaugeAttribute.Name),
				NullIfWhitespace(observableGaugeAttribute.Unit),
				NullIfWhitespace(observableGaugeAttribute.Description),
				observableGaugeAttribute.ThrowOnAlreadyInitialized
			);

		// No matching instrument attribute found
		return null;
	}

	public static bool IsValidMeasurementValueType(ITypeSymbol type) =>
		PropertyLibrary.Metrics.ValidMeasurementKeywordTypes.Any(m => m.SpecialType == type.SpecialType);

	/// <summary>
	/// Parses <c>HistogramBucketBoundaries</c>: a comma-separated list of invariant-culture, ascending, positive
	/// numbers. A value that does not parse yields <see langword="null"/>, so no advice is emitted and
	/// <c>TSG4012</c> reports it.
	/// </summary>
	internal static EquatableArray<double>? ParseHistogramBucketBoundaries(string? boundaries)
	{
		if (string.IsNullOrWhiteSpace(boundaries))
			return null;

		List<double> values = [];
		foreach (var part in boundaries!.Split(','))
		{
			if (
				!double.TryParse(
					part.Trim(),
					System.Globalization.NumberStyles.Float,
					System.Globalization.CultureInfo.InvariantCulture,
					out var value
				)
				|| value <= 0
			)
				return null;

			if (values.Count > 0 && value <= values[values.Count - 1])
				return null;

			values.Add(value);
		}

		return values.Count == 0 ? null : new([.. values]);
	}
}
