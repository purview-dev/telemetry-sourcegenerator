namespace Purview.Telemetry.SourceGenerator.Records;

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
