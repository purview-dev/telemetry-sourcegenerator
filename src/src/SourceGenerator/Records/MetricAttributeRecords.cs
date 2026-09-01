namespace Purview.Telemetry.SourceGenerator.Records;

sealed record MeterGenerationAttributeRecord(
	AttributeStringValue InstrumentPrefix,
	AttributeStringValue InstrumentSeparator,
	AttributeValue<bool> LowercaseInstrumentName,
	AttributeValue<bool> LowercaseTagKeys,
	AttributeStringValue MeterName,
	AttributeValue<int> MeterNameGenerationType
);

sealed record MeterAttributeRecord(
	AttributeStringValue Name,
	AttributeStringValue InstrumentPrefix,
	AttributeValue<bool> IncludeAssemblyInstrumentPrefix,
	AttributeValue<bool> LowercaseInstrumentName,
	AttributeValue<bool> LowercaseTagKeys
);

sealed record InstrumentAttributeRecord(
	AttributeStringValue Name,
	AttributeStringValue? Unit,
	AttributeStringValue? Description,
	AttributeValue<bool>? AutoIncrement,
	AttributeValue<bool>? ThrowOnAlreadyInitialized,
	InstrumentTypes InstrumentType
)
{
	public bool IsAutoIncrement => AutoIncrement?.Value ?? false;

	public bool IsObservable =>
		InstrumentType
			is InstrumentTypes.ObservableCounter
				or InstrumentTypes.ObservableGauge
				or InstrumentTypes.ObservableUpDownCounter;
}
