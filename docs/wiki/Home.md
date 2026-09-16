# Purview Telemetry Source Generator

Generates [`ActivitySource`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysource), [`ILogger`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger), and [`Metrics`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics) based telemetry from methods you define on an interface. Define the interface once and the generator produces the implementation, DI registration helpers, and multi-target generation — no runtime reflection, no boilerplate.

This approach allows for:

- **Zero boilerplate** — define methods on an interface, get the full telemetry implementation generated
- **Multi-target generation** — generate Activities, Logging, and Metrics from a single interface
- **Testable** — easy mocking/substitution for unit testing
- **DI-ready** — automatic dependency injection registration helpers
- **OpenTelemetry-aligned** — defaults to OpenTelemetry semantic conventions for better observability

## Supported frameworks

- .NET 8 or higher
- .NET Framework 4.8 or higher

**Build toolchain requirement:** Visual Studio 2026 (18.x) or the .NET 10 SDK (Roslyn 5.9.0+).

## Documentation

### Getting started

- [Getting Started](Getting-Started.md) — install, first interface, and common patterns
- [Installation](Installation.md) — supported frameworks, per-feature dependencies, and build configuration
- [Sample Application](Sample-Application.md) — full .NET Aspire demo application

### Core features

- [Activities](Activities.md) — distributed tracing generation with `ActivitySource`
- [Logging](Logging.md) — structured logging generation with `ILogger`
  - [Generation v2](Logging-Generation-v2.md) — state-based logging with `Microsoft.Extensions.Telemetry.Abstractions`
  - [Generation v1](Logging-Generation-v1.md) — legacy `LoggerMessage.Define` high-performance mode
- [Metrics](Metrics.md) — metrics generation (Counters, Histograms, Observables)
- [Multi-Targeting](Multi-Targeting.md) — combine Activities + Logging + Metrics in one interface

### Configuration and reference

- [Generation Options](Generation.md) — control class names, DI, and code generation
- [Tags, Baggage, and Parameter Attributes](Tags-and-Baggage.md) — adding tags/baggage/properties to telemetry
- [Generated Output](Generated-Output.md) — examples of generated code
- [Diagnostics](Diagnostics.md) — analyzer warnings and errors
- [FAQ](FAQ.md) — frequently asked questions and troubleshooting
- [Breaking Changes](Breaking-Changes.md) — migration guide for v3 → v4 → v5
- [Performance](Performance.md) — cross-runtime benchmark results

### Migrating

- [Refactorings](Refactorings.md) — the shipped IDE code refactorings
- [Migration from ILogger](Migration-From-ILogger.md)
- [Migration from Activities](Migration-From-Activities.md)

### Contributing

- [Testing](Testing.md) — mocking generated telemetry interfaces
- [Contributing](Contributing.md) — development setup, build commands, and conventions
- [Release Flow](Release-Flow.md) — how releases are produced

## Basic examples

Each generation target ([Activities](Activities.md), [Logging](Logging.md), and [Metrics](Metrics.md)) documents what can be inferred and what must be explicit. By default each interface used as a source for generation includes an extension method for registering it with an `IServiceCollection`; more details can be found in [Generation](Generation.md).

> [!TIP]
> You can mix-and-match generation targets within a single interface; this is called [multi-targeting](Multi-Targeting.md). When you do, inference is disabled and every method must declare its targets explicitly.

> [!NOTE]
> In .NET, Activities, Events, and Metrics capture additional properties at creation, recording, or observation time as **tags**. OpenTelemetry calls these **attributes**. Because this source generator makes extensive use of marker attributes to control code generation, these docs use *tags* for the properties and *attributes* for the .NET [`Attribute`](https://learn.microsoft.com/en-us/dotnet/api/system.attribute) type.

All marker attributes are generated as `[Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]`, so they are not present in your build unless you define the `PURVIEW_TELEMETRY_ATTRIBUTES` constant. They are generated as internal to avoid exposing them outside the assembly.

### Activities

Basic example of an activity-based telemetry interface. There is one Activity (`GettingItemFromCache`) and four events. Calling these adds an `ActivityEvent` to the `activity` parameter; if no Activity is passed in, `Activity.Current` is used instead. There is also a context method that adds its parameters as either tags or baggage to the current Activity.

```csharp
using Purview.Telemetry;

[ActivitySource("some-activity")]
interface IActivityTelemetry
{
    [Activity]
    Activity? GettingItemFromCache([Baggage]string key, [Tag]string itemType);

    [Event("cachemiss")]
    void Miss(Activity? activity);

    [Event("cachehit")]
    void Hit(Activity? activity);

    [Event]
    void Error(Activity? activity, Exception ex);

    [Event]
    void Finished(Activity? activity, [Tag]TimeSpan duration);

    [Context]
    void AdditionalInfo(Activity? activity, string state);
}
```

More information can be found in [Activities](Activities.md).

### Logging

Basic example of a structured logging-based interface. `ProcessingWorkItem` returns an `IDisposable?`, which creates a scoped log entry. All parameters are passed into the logger methods as properties.

```csharp
using Purview.Telemetry;

[Logger]
interface ILoggingTelemetry
{
    [Log]
    IDisposable? ProcessingWorkItem(Guid id);

    [Log(LogLevel.Trace)]
    void ProcessingItemType(ItemTypes itemType);

    [Error]
    void FailedToProcessWorkItem(Exception ex);

    [Info]
    void ProcessingComplete(bool success, TimeSpan duration);
}
```

More information can be found in [Logging](Logging.md), including the two generation modes and how to disable logging generation when the `Microsoft.Extensions.Logging` types are unavailable.

### Metrics

This example shows each meter type currently supported. The `Counter` attribute is demonstrated twice: once with `AutoIncrement = true` (the measurement value is set to 1 per call) and once with the measurement specified explicitly as a parameter.

> [!IMPORTANT]
> Non-auto-increment instruments must specify a measurement of one of the supported types: `byte`, `short`, `int`, `long`, `float`, `double`, or `decimal`.

> [!NOTE]
> Observable instruments must always have a `System.Func<>` parameter with one of the following shapes:
>
> - Any supported measurement type (`byte`, `short`, `int`, `long`, `float`, `double`, or `decimal`)
> - `Measurement<T>` where `T` is a supported measurement type
> - `IEnumerable<Measurement<T>>` where `T` is a supported measurement type

As with activities, `[Tag]` parameters are included at recording time for the instrument.

```csharp
using Purview.Telemetry;

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

    [ObservableCounter]
    void ObservableCounterMeter(Func<float> measurement, [Tag]double someValue);

    [ObservableGauge]
    void ObservableGaugeMeter(Func<Measurement<float>> measurement, [Tag]double someValue);

    [ObservableUpDownCounter]
    void ObservableUpDownCounter(Func<IEnumerable<Measurement<byte>>> measurement, [Tag]double someValue);

    [UpDownCounter]
    void UpDownCounterMeter([InstrumentMeasurement]decimal measurement, [Tag]byte someValue);
}
```

More information can be found in [Metrics](Metrics.md).

## Multi-targeting

In this example all method-level targets are set explicitly — inferring usage is not supported when multi-targeting.

```csharp
using Purview.Telemetry;

[ActivitySource("multi-targeting")]
[Logger]
[Meter]
interface IServiceTelemetry
{
    [Activity]
    [Trace]
    Activity? StartAnActivity(string tagStringParam, [Baggage]int entityId);

    [Event]
    [Info]
    void AnInterestingEvent(Activity? activity, float aTagValue);

    [Error]
    [Event]
    [AutoCounter]
    void AnError(Activity? activity, Exception ex);

    [Context]
    [AutoCounter]
    [Debug]
    void InterestingInfo(Activity? activity, float anotherTagValue, int intTagValue);

    [Histogram]
    [Trace]
    void ProcessingEntity(int entityId, string property1);

    [Info]
    [Counter]
    void ACounter([Tag]int value);
}
```

More information can be found in [Multi-Targeting](Multi-Targeting.md).