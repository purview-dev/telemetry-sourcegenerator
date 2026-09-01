namespace Purview.Telemetry.SourceGenerator.Records;

sealed record MeterGenerationAttributeRecord(
	string? InstrumentPrefix,
	string? InstrumentSeparator,
	bool LowercaseInstrumentName,
	bool LowercaseTagKeys,
	string? MeterName,
	int MeterNameGenerationType
);

sealed record MeterAttributeRecord(
	string? Name,
	string? InstrumentPrefix,
	bool IncludeAssemblyInstrumentPrefix,
	bool LowercaseInstrumentName,
	bool LowercaseTagKeys
);

sealed record InstrumentAttributeRecord(
	string? Name,
	string? Unit,
	string? Description,
	bool AutoIncrement,
	bool ThrowOnAlreadyInitialized,
	InstrumentTypes InstrumentType
)
{
	public bool IsAutoIncrement => AutoIncrement;

	public bool IsObservable =>
		InstrumentType
			is InstrumentTypes.ObservableCounter
				or InstrumentTypes.ObservableGauge
				or InstrumentTypes.ObservableUpDownCounter;
}
