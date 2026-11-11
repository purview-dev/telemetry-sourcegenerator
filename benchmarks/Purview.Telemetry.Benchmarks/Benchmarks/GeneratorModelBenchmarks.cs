using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Purview.Telemetry.Benchmarks.Benchmarks;

/// <summary>
/// Measures the cost of the two modelling strategies the generator's pipeline can use for the tagged hierarchies
/// (<c>closed record InstrumentAttributeRecord</c> / <c>ActivityMethodTarget</c>).
/// </summary>
/// <remarks>
/// The generator's own models are internal to the analyzer assembly, which a benchmark project cannot reference, so the
/// two shapes are replicated here exactly as the generator declares them: an abstract record with one sealed case per
/// instrument and virtual properties, and a <c>readonly struct</c> carrying a tag plus the case payloads with a
/// non-boxing <c>TryGetValue</c> accessor. Both build the same number of models and read the same four facts off each
/// one, which is what a generation pass does per method.
/// <para>
/// The result answers one question: is the per-method allocation and virtual dispatch of the record hierarchy a
/// measurable part of a generation pass? If the delta is a handful of nanoseconds per method, replacing the records
/// cannot move generator throughput, because a pass also parses every symbol, builds the message templates and writes
/// the source text.
/// </para>
/// </remarks>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
[SimpleJob(RuntimeMoniker.Net10_0)]
[SimpleJob(RuntimeMoniker.Net11_0)]
[SimpleJob(RuntimeMoniker.Net48)]
public class GeneratorModelBenchmarks
{
	[Params(8, 64)]
	public int MethodCount { get; set; }

	[Benchmark(Baseline = true, Description = "closed record hierarchy: build + dispatch")]
	public int ClosedRecords_BuildAndRead()
	{
		List<InstrumentRecord> models = new(MethodCount);
		for (var i = 0; i < MethodCount; i++)
		{
			models.Add(
				(i % 7) switch
				{
					0 => new CounterRecord("name", "unit", "description", AutoIncrement: true),
					1 => new UpDownCounterRecord("name", "unit", "description"),
					2 => new HistogramRecord("name", "unit", "description"),
					3 => new GaugeRecord("name", "unit", "description"),
					4 => new ObservableCounterRecord("name", "unit", "description", ThrowOnAlreadyInitialized: false),
					5 => new ObservableGaugeRecord("name", "unit", "description", ThrowOnAlreadyInitialized: false),
					_ => new ObservableUpDownCounterRecord(
						"name",
						"unit",
						"description",
						ThrowOnAlreadyInitialized: false
					),
				}
			);
		}

		var total = 0;
		foreach (var model in models)
		{
			total += model.CreationMethodName.Length;
			total += model.TypeName.Length;
			total += model.IsObservable ? 1 : 0;
			total += model.UsesRecordMethod ? 1 : 0;
		}

		return total;
	}

	[Benchmark(Description = "value-based struct union: build + dispatch")]
	public int StructUnion_BuildAndRead()
	{
		List<InstrumentUnion> models = new(MethodCount);
		for (var i = 0; i < MethodCount; i++)
		{
			models.Add(
				(i % 7) switch
				{
					0 => InstrumentUnion.ForCounter(new("name", "unit", "description", true, false)),
					1 => InstrumentUnion.ForUpDownCounter(new("name", "unit", "description", false, false)),
					2 => InstrumentUnion.ForHistogram(new("name", "unit", "description", false, false)),
					3 => InstrumentUnion.ForGauge(new("name", "unit", "description", false, false)),
					4 => InstrumentUnion.ForObservableCounter(new("name", "unit", "description", false, false)),
					5 => InstrumentUnion.ForObservableGauge(new("name", "unit", "description", false, false)),
					_ => InstrumentUnion.ForObservableUpDownCounter(new("name", "unit", "description", false, false)),
				}
			);
		}

		var total = 0;
		foreach (var model in models)
		{
			total += model.CreationMethodName.Length;
			total += model.TypeName.Length;
			total += model.IsObservable ? 1 : 0;
			total += model.UsesRecordMethod ? 1 : 0;
		}

		return total;
	}
}

internal abstract record InstrumentRecord(
	string? Name,
	string? Unit,
	string? Description,
	bool AutoIncrement,
	bool ThrowOnAlreadyInitialized
)
{
	public abstract string CreationMethodName { get; }

	public abstract string TypeName { get; }

	public abstract bool IsObservable { get; }

	public abstract bool UsesRecordMethod { get; }
}

internal sealed record CounterRecord(string? Name, string? Unit, string? Description, bool AutoIncrement)
	: InstrumentRecord(Name, Unit, Description, AutoIncrement, false)
{
	public override string CreationMethodName => "CreateCounter";

	public override string TypeName => "Counter";

	public override bool IsObservable => false;

	public override bool UsesRecordMethod => false;
}

internal sealed record UpDownCounterRecord(string? Name, string? Unit, string? Description)
	: InstrumentRecord(Name, Unit, Description, false, false)
{
	public override string CreationMethodName => "CreateUpDownCounter";

	public override string TypeName => "UpDownCounter";

	public override bool IsObservable => false;

	public override bool UsesRecordMethod => false;
}

internal sealed record HistogramRecord(string? Name, string? Unit, string? Description)
	: InstrumentRecord(Name, Unit, Description, false, false)
{
	public override string CreationMethodName => "CreateHistogram";

	public override string TypeName => "Histogram";

	public override bool IsObservable => false;

	public override bool UsesRecordMethod => true;
}

internal sealed record GaugeRecord(string? Name, string? Unit, string? Description)
	: InstrumentRecord(Name, Unit, Description, false, false)
{
	public override string CreationMethodName => "CreateGauge";

	public override string TypeName => "Gauge";

	public override bool IsObservable => false;

	public override bool UsesRecordMethod => true;
}

internal sealed record ObservableCounterRecord(
	string? Name,
	string? Unit,
	string? Description,
	bool ThrowOnAlreadyInitialized
) : InstrumentRecord(Name, Unit, Description, false, ThrowOnAlreadyInitialized)
{
	public override string CreationMethodName => "CreateObservableCounter";

	public override string TypeName => "ObservableCounter";

	public override bool IsObservable => true;

	public override bool UsesRecordMethod => false;
}

internal sealed record ObservableGaugeRecord(
	string? Name,
	string? Unit,
	string? Description,
	bool ThrowOnAlreadyInitialized
) : InstrumentRecord(Name, Unit, Description, false, ThrowOnAlreadyInitialized)
{
	public override string CreationMethodName => "CreateObservableGauge";

	public override string TypeName => "ObservableGauge";

	public override bool IsObservable => true;

	public override bool UsesRecordMethod => false;
}

internal sealed record ObservableUpDownCounterRecord(
	string? Name,
	string? Unit,
	string? Description,
	bool ThrowOnAlreadyInitialized
) : InstrumentRecord(Name, Unit, Description, false, ThrowOnAlreadyInitialized)
{
	public override string CreationMethodName => "CreateObservableUpDownCounter";

	public override string TypeName => "ObservableUpDownCounter";

	public override bool IsObservable => true;

	public override bool UsesRecordMethod => false;
}

// --- The value-based union equivalent: tag plus payload, no boxing, value equality. ---

internal enum InstrumentKind
{
	Counter,
	UpDownCounter,
	Histogram,
	Gauge,
	ObservableCounter,
	ObservableGauge,
	ObservableUpDownCounter,
}

internal readonly record struct InstrumentPayload(
	string? Name,
	string? Unit,
	string? Description,
	bool AutoIncrement,
	bool ThrowOnAlreadyInitialized
);

internal readonly record struct InstrumentUnion(InstrumentKind Kind, InstrumentPayload Payload)
{
	public static InstrumentUnion ForCounter(InstrumentPayload payload) => new(InstrumentKind.Counter, payload);

	public static InstrumentUnion ForUpDownCounter(InstrumentPayload payload) =>
		new(InstrumentKind.UpDownCounter, payload);

	public static InstrumentUnion ForHistogram(InstrumentPayload payload) => new(InstrumentKind.Histogram, payload);

	public static InstrumentUnion ForGauge(InstrumentPayload payload) => new(InstrumentKind.Gauge, payload);

	public static InstrumentUnion ForObservableCounter(InstrumentPayload payload) =>
		new(InstrumentKind.ObservableCounter, payload);

	public static InstrumentUnion ForObservableGauge(InstrumentPayload payload) =>
		new(InstrumentKind.ObservableGauge, payload);

	public static InstrumentUnion ForObservableUpDownCounter(InstrumentPayload payload) =>
		new(InstrumentKind.ObservableUpDownCounter, payload);

	public string CreationMethodName =>
		Kind switch
		{
			InstrumentKind.Counter => "CreateCounter",
			InstrumentKind.UpDownCounter => "CreateUpDownCounter",
			InstrumentKind.Histogram => "CreateHistogram",
			InstrumentKind.Gauge => "CreateGauge",
			InstrumentKind.ObservableCounter => "CreateObservableCounter",
			InstrumentKind.ObservableGauge => "CreateObservableGauge",
			_ => "CreateObservableUpDownCounter",
		};

	public string TypeName =>
		Kind switch
		{
			InstrumentKind.Counter => "Counter",
			InstrumentKind.UpDownCounter => "UpDownCounter",
			InstrumentKind.Histogram => "Histogram",
			InstrumentKind.Gauge => "Gauge",
			InstrumentKind.ObservableCounter => "ObservableCounter",
			InstrumentKind.ObservableGauge => "ObservableGauge",
			_ => "ObservableUpDownCounter",
		};

	public bool IsObservable =>
		Kind
			is InstrumentKind.ObservableCounter
				or InstrumentKind.ObservableGauge
				or InstrumentKind.ObservableUpDownCounter;

	public bool UsesRecordMethod => Kind is InstrumentKind.Histogram or InstrumentKind.Gauge;
}
