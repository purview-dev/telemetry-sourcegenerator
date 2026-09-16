# Metrics

> [!IMPORTANT]
> All attributes are now in the unified `Purview.Telemetry` namespace. See [Breaking Changes](Breaking-Changes.md#namespace-consolidation) for migration details.

All metric-related attributes live in the `Purview.Telemetry` namespace. To signal an interface for meter generation, decorate it with the `[Meter]` attribute.

When creating the meter types, the [`IMeterFactory`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.imeterfactory) is used when available (it is not available on .NET Framework 4.8, where a `new Meter(...)` is created instead).

## Meter naming

The meter name is resolved in this order:

1. `MeterAttribute.Name` (interface)
2. `MeterGenerationAttribute.MeterName` (assembly)
3. The assembly name

```csharp
[Meter("InventoryService")]
interface IInventoryMetrics { }
```

When the `NamingConvention.OpenTelemetry` naming convention is combined with `MeterNameGenerationType.OpenTelemetry`, instrument names are generated with the meter name as a dot-separated prefix (for example, `myapp.products.record.count`).

## Initialisation

During initialisation you can implement a partial method to provide additional tags to any meters created by the `IMeterFactory`:

```csharp
partial void PopulateMeterTags(System.Collections.Generic.Dictionary<string, object?> meterTags)
{
    meterTags["environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
}
```

Any tags added to `meterTags` are included on every meter created.

## Instrument types

Each instrument is determined by its corresponding attribute:

- `[AutoCounter]` and `[Counter]` generate the [`Counter<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.counter-1) instrument.
- `[Histogram]` generates the [`Histogram<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.histogram-1) instrument.
- `[UpDownCounter]` generates the [`UpDownCounter<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.updowncounter-1) instrument.
- `[ObservableCounter]` generates the [`ObservableCounter<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.observablecounter-1) instrument.
- `[ObservableGauge]` generates the [`ObservableGauge<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.observablegauge-1) instrument.
- `[ObservableUpDownCounter]` generates the [`ObservableUpDownCounter<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.observableupdowncounter-1) instrument.

The measurement type must be one of `byte`, `short`, `int`, `long`, `float`, `double`, or `decimal`.

## The measurement value

The measurement parameter is the parameter decorated with `[InstrumentMeasurement]`. For non-auto-increment instruments, if no parameter is decorated the first parameter with a valid measurement type that is not a `[Tag]`/`[Baggage]` is used.

- `[AutoCounter]` and `[Counter(AutoIncrement = true)]` increment by **1** each time the method is called and must **not** declare a measurement parameter (`TSG4002`).
- `[Counter]`, `[Histogram]`, and `[UpDownCounter]` require a measurement value (`TSG4004`).
- Observable instruments must declare a `Func<T>` parameter (`TSG4005`).

```csharp
[Meter]
interface IMeterTelemetry
{
    [AutoCounter]
    void AutoIncrementMeter([Tag]string someValue);

    [Counter(AutoIncrement = true)]
    void AutoIncrementCounterMeter([Tag]string someValue);

    [Counter]
    void CounterMeter([InstrumentMeasurement]int measurement, [Tag]float someValue);

    [Histogram]
    void HistogramMeter([InstrumentMeasurement]int measurement, [Tag]int someValue, [Tag]bool anotherValue);

    [UpDownCounter]
    void UpDownCounterMeter([InstrumentMeasurement]decimal measurement, [Tag]byte someValue);
}
```

### Observable instruments

Observable instruments always take a `System.Func<>` parameter with one of the following shapes:

- Any supported measurement type: `byte`, `short`, `int`, `long`, `float`, `double`, or `decimal`
- [`Measurement<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.measurement-1) where `T` is a supported measurement type
- `IEnumerable<Measurement<T>>` where `T` is a supported measurement type

```csharp
[ObservableCounter]
void Counter(Func<int> func);

[ObservableGauge]
void Gauge(Func<Measurement<int>> func);

[ObservableUpDownCounter]
void UpDownCounter(Func<IEnumerable<Measurement<int>>> func);
```

### Tags

Other parameters on the method are used as tags. This is implicit for non-measurement parameters, but can also be made explicit with [`[Tag]`](Tags-and-Baggage.md). When there are four or more tags, the generated code uses a stack-allocated [`TagList`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.taglist).

## Attribute reference

### `[Meter]`

Defines and controls the generation of meters and instruments on an interface, overriding assembly-level defaults.

| Property | Type | Description |
| --- | --- | --- |
| `Name` | `string?` | The name of the meter, used to group instruments. If not specified, `MeterGenerationAttribute.MeterName` is used, then the assembly name. Default `null`. |
| `InstrumentPrefix` | `string?` | The prefix used when generating instrument names. Default `null`. |
| `IncludeAssemblyInstrumentPrefix` | `bool` | Whether the `MeterGenerationAttribute.InstrumentPrefix` prefix is included in instrument names. Default `true`. |
| `LowercaseInstrumentName` | `bool` | Whether instrument names (including prefix) are lower-cased. Default `true`. |
| `LowercaseTagKeys` | `bool` | Whether tag names are lower-cased. Default `true`. |

### `[MeterGeneration]`

Controls meter and instrument defaults at the assembly level.

| Property | Type | Description |
| --- | --- | --- |
| `MeterName` | `string?` | The default meter name when none is defined on an interface. Available on construction. |
| `MeterNameGenerationType` | `MeterNameGenerationType` | Whether the meter name is used as a dot-separated prefix for instrument names. `OpenTelemetry` (0) adds the prefix; `DotNet` (1, default) does not. Available on construction. |
| `InstrumentPrefix` | `string?` | The prefix used when generating instrument names. Default `null`. |
| `InstrumentSeparator` | `string` | The separator used when generating prefixes. Default `.`. |
| `LowercaseInstrumentName` | `bool` | Whether instrument names (including prefix) are lower-cased. Default `true`. |
| `LowercaseTagKeys` | `bool` | Whether tag names are lower-cased. Default `true`. |

### `[InstrumentMeasurement]`

Marks the parameter used as the instrument measurement value. Not supported with `[AutoCounter]` or `[Counter(AutoIncrement = true)]`.

## Instrument attributes

### `[AutoCounter]`

Creates a `Counter<int>` that increments by 1 per call.

| Property | Type | Description |
| --- | --- | --- |
| `Name` | `string?` | Instrument name; defaults to the method name. Available on construction. Default `null`. |
| `Unit` | `string?` | The unit used during meter generation. Available on construction. Default `null`. |
| `Description` | `string?` | The description used during meter generation. Available on construction. Default `null`. |

### `[Counter]`

Creates a `Counter<T>`.

| Property | Type | Description |
| --- | --- | --- |
| `AutoIncrement` | `bool` | When `true`, generates an auto-incrementing counter instead of accepting a measurement value from a parameter. Available on construction. Default `false`. |
| `Name` | `string?` | Instrument name; defaults to the method name. Available on construction. Default `null`. |
| `Unit` | `string?` | The unit used during meter generation. Available on construction. Default `null`. |
| `Description` | `string?` | The description used during meter generation. Available on construction. Default `null`. |

When `AutoIncrement` is `true` and `[InstrumentMeasurement]` is used, the `TSG4002` diagnostic is raised.

### `[Histogram]` and `[UpDownCounter]`

Create a `Histogram<T>` or `UpDownCounter<T>`. Both share the `Name`, `Unit`, and `Description` properties described above.

### Observable attributes

`[ObservableCounter]`, `[ObservableGauge]`, and `[ObservableUpDownCounter]` share the `Name`, `Unit`, and `Description` properties, plus:

| Property | Type | Description |
| --- | --- | --- |
| `ThrowOnAlreadyInitialized` | `bool` | Whether the method throws when called more than once. Available on construction. Default `false`. |

## Next steps

- [Tags, Baggage, and Parameter Attributes](Tags-and-Baggage.md) — tagging parameters
- [Multi-Targeting](Multi-Targeting.md) — combining Metrics with Activities and Logging
- [Diagnostics](Diagnostics.md) — the `TSG4xxx` Metrics rules