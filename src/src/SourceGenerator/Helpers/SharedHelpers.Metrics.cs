using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class SharedHelpers
{
	/// <summary>
	/// Merges the assembly-level and interface-level <c>Tags</c> settings into the ordered
	/// <c>key=value</c> list a target carries. Both arrive as a comma-separated string because an attribute
	/// property cannot be an array (the attribute-data model rejects <c>string[]</c> with <c>ADM0002</c>), so the
	/// parse lives here once for both <c>MeterOptions.Tags</c> and <c>ActivitySourceOptions.Tags</c>. The
	/// interface-level entry replaces the assembly-level one for the same key; a malformed entry is skipped.
	/// </summary>
	public static EquatableArray<string> MergeTags(string? assemblyTags, string? interfaceTags)
	{
		List<(string Key, string Value)> merged = [];
		AddTagEntries(assemblyTags, merged);
		AddTagEntries(interfaceTags, merged);

		if (merged.Count == 0)
			return new EquatableArray<string>([]);

		List<string> rendered = [];
		foreach (var entry in merged)
			rendered.Add(entry.Key + "=" + entry.Value);

		return new EquatableArray<string>([.. rendered]);
	}

	/// <summary>The entries in a <c>Tags</c> string that are not <c>key=value</c>, for diagnostics.</summary>
	public static EquatableArray<string> GetMalformedTags(string? tags)
	{
		if (string.IsNullOrWhiteSpace(tags))
			return new EquatableArray<string>([]);

		List<string> malformed = [];

		// netstandard2.0 reference assemblies do not annotate IsNullOrWhiteSpace, so the null state is restated.
		var tagList = tags!;

		foreach (var entry in tagList.Split(','))
		{
			var trimmed = entry.Trim();
			if (trimmed.Length == 0)
				continue;

			var separator = trimmed.IndexOf('=');
			if (separator <= 0 || separator == trimmed.Length - 1 || trimmed.Substring(0, separator).Trim().Length == 0)
				malformed.Add(trimmed);
		}

		return malformed.Count == 0 ? new EquatableArray<string>([]) : new EquatableArray<string>([.. malformed]);
	}

	static void AddTagEntries(string? tags, List<(string Key, string Value)> entries)
	{
		if (string.IsNullOrWhiteSpace(tags))
			return;

		// netstandard2.0 reference assemblies do not annotate IsNullOrWhiteSpace, so the null state is restated.
		var tagList = tags!;

		foreach (var entry in tagList.Split(','))
		{
			var trimmed = entry.Trim();
			if (trimmed.Length == 0)
				continue;

			var separator = trimmed.IndexOf('=');
			if (separator <= 0 || separator == trimmed.Length - 1)
				continue;

			var key = trimmed.Substring(0, separator).Trim();
			var value = trimmed.Substring(separator + 1).Trim();
			if (key.Length == 0)
				continue;

			var existing = entries.FindIndex(candidate => candidate.Key == key);
			if (existing >= 0)
				entries[existing] = (key, value);
			else
				entries.Add((key, value));
		}
	}

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
