namespace Purview.Telemetry.SourceGenerator.Records;

/// <summary>
/// The instrument a metrics method declares: exactly one of the eight <c>System.Diagnostics.Metrics</c> instruments,
/// so the hierarchy is <c>closed</c> and a <c>switch</c> over it is exhaustive.
/// </summary>
/// <remarks>
/// Each case knows the <c>Meter</c> factory method that creates it, the instrument type it produces and how it
/// records. The emitters read those properties instead of building a factory name from an enum member's name, which
/// is what a rename used to break silently: the generator emitted <c>CreateRenamedInstrument</c> into the consumer's
/// build rather than failing here.
/// </remarks>
/// <param name="Name">The instrument name, or <see langword="null"/> to use the method name.</param>
/// <param name="Unit">The measurement unit.</param>
/// <param name="Description">The instrument description.</param>
/// <param name="AutoIncrement">Whether the counter adds the value without a measurement parameter.</param>
/// <param name="ThrowOnAlreadyInitialized">
/// Whether an observable instrument throws when it is initialised twice.
/// </param>
closed record InstrumentAttributeRecord(
	string? Name,
	string? Unit,
	string? Description,
	bool AutoIncrement,
	bool ThrowOnAlreadyInitialized
)
{
	public bool IsAutoIncrement => AutoIncrement;

	/// <summary>The <c>Meter</c> factory method that creates this instrument, for example <c>CreateCounter</c>.</summary>
	public abstract string CreationMethodName { get; }

	/// <summary>The instrument type, for example <c>System.Diagnostics.Metrics.Counter&lt;T&gt;</c>.</summary>
	public abstract TypeIdentity InstrumentType { get; }

	/// <summary>The instrument family name, as it appears in the API: <c>Counter</c>, <c>ObservableGauge</c>, ...</summary>
	public abstract string TypeName { get; }

	/// <summary>Whether the instrument is registered once and observed by a callback rather than recorded.</summary>
	public abstract bool IsObservable { get; }

	/// <summary>
	/// Whether the instrument records with <c>Record</c> rather than <c>Add</c>: gauges and histograms record,
	/// counters add.
	/// </summary>
	public abstract bool UsesRecordMethod { get; }
}

/// <summary>A <c>[Counter]</c> instrument, including the auto-incrementing <c>[AutoCounter]</c>.</summary>
sealed record CounterInstrumentAttributeRecord(string? Name, string? Unit, string? Description, bool AutoIncrement)
	: InstrumentAttributeRecord(Name, Unit, Description, AutoIncrement, false)
{
	public override string CreationMethodName => "CreateCounter";

	public override TypeIdentity InstrumentType => TypeLibrary.System.Diagnostics.Metrics.Counter;

	public override string TypeName => "Counter";

	public override bool IsObservable => false;

	public override bool UsesRecordMethod => false;
}

/// <summary>An <c>[UpDownCounter]</c> instrument.</summary>
sealed record UpDownCounterInstrumentAttributeRecord(string? Name, string? Unit, string? Description)
	: InstrumentAttributeRecord(Name, Unit, Description, false, false)
{
	public override string CreationMethodName => "CreateUpDownCounter";

	public override TypeIdentity InstrumentType => TypeLibrary.System.Diagnostics.Metrics.UpDownCounter;

	public override string TypeName => "UpDownCounter";

	public override bool IsObservable => false;

	public override bool UsesRecordMethod => false;
}

/// <summary>A <c>[Histogram]</c> instrument, optionally with bucket boundaries.</summary>
sealed record HistogramInstrumentAttributeRecord(
	string? Name,
	string? Unit,
	string? Description,
	EquatableArray<double>? HistogramBucketBoundaries = null,
	bool InvalidHistogramBucketBoundaries = false
) : InstrumentAttributeRecord(Name, Unit, Description, false, false)
{
	public override string CreationMethodName => "CreateHistogram";

	public override TypeIdentity InstrumentType => TypeLibrary.System.Diagnostics.Metrics.Histogram;

	public override string TypeName => "Histogram";

	public override bool IsObservable => false;

	public override bool UsesRecordMethod => true;
}

/// <summary>A <c>[Gauge]</c> instrument (.NET 9): records a value without accumulating it.</summary>
sealed record GaugeInstrumentAttributeRecord(string? Name, string? Unit, string? Description)
	: InstrumentAttributeRecord(Name, Unit, Description, false, false)
{
	public override string CreationMethodName => "CreateGauge";

	public override TypeIdentity InstrumentType => TypeLibrary.System.Diagnostics.Metrics.Gauge;

	public override string TypeName => "Gauge";

	public override bool IsObservable => false;

	public override bool UsesRecordMethod => true;
}

/// <summary>An <c>[ObservableCounter]</c> instrument.</summary>
sealed record ObservableCounterInstrumentAttributeRecord(
	string? Name,
	string? Unit,
	string? Description,
	bool ThrowOnAlreadyInitialized
) : InstrumentAttributeRecord(Name, Unit, Description, false, ThrowOnAlreadyInitialized)
{
	public override string CreationMethodName => "CreateObservableCounter";

	public override TypeIdentity InstrumentType => TypeLibrary.System.Diagnostics.Metrics.ObservableCounter;

	public override string TypeName => "ObservableCounter";

	public override bool IsObservable => true;

	public override bool UsesRecordMethod => false;
}

/// <summary>An <c>[ObservableGauge]</c> instrument.</summary>
sealed record ObservableGaugeInstrumentAttributeRecord(
	string? Name,
	string? Unit,
	string? Description,
	bool ThrowOnAlreadyInitialized
) : InstrumentAttributeRecord(Name, Unit, Description, false, ThrowOnAlreadyInitialized)
{
	public override string CreationMethodName => "CreateObservableGauge";

	public override TypeIdentity InstrumentType => TypeLibrary.System.Diagnostics.Metrics.ObservableGauge;

	public override string TypeName => "ObservableGauge";

	public override bool IsObservable => true;

	public override bool UsesRecordMethod => false;
}

/// <summary>An <c>[ObservableUpDownCounter]</c> instrument.</summary>
sealed record ObservableUpDownCounterInstrumentAttributeRecord(
	string? Name,
	string? Unit,
	string? Description,
	bool ThrowOnAlreadyInitialized
) : InstrumentAttributeRecord(Name, Unit, Description, false, ThrowOnAlreadyInitialized)
{
	public override string CreationMethodName => "CreateObservableUpDownCounter";

	public override TypeIdentity InstrumentType => TypeLibrary.System.Diagnostics.Metrics.ObservableUpDownCounter;

	public override string TypeName => "ObservableUpDownCounter";

	public override bool IsObservable => true;

	public override bool UsesRecordMethod => false;
}
