# Frequently Asked Questions (FAQ)

Common questions and answers about the Purview Telemetry Source Generator.

## General questions

### What is the Purview Telemetry Source Generator?

A .NET incremental source generator that produces implementation code for Activities (distributed tracing), Logging (structured logs), and Metrics from interface definitions you create. Instead of writing boilerplate telemetry code, you define methods on an interface and the generator creates the implementation, DI registration helpers, and multi-target generation.

### Why use a source generator for telemetry?

- **Zero boilerplate** — no manual implementation code to write or maintain
- **Type safety** — compile-time validation of telemetry code
- **Testability** — easy to mock interfaces in unit tests
- **Consistency** — all telemetry follows the same patterns
- **DI-ready** — automatic dependency injection registration
- **Performance** — generated code is optimized
- **Maintainability** — changes to interfaces propagate automatically

### What .NET versions are supported?

- .NET 8 or higher
- .NET Framework 4.8 or higher

### Is this compatible with OpenTelemetry?

Yes. v4+ defaults to OpenTelemetry semantic conventions for generated names. The generated Activities, Logs, and Metrics work with OpenTelemetry exporters and collectors.

## Installation & setup

### How do I install the package?

Add the NuGet package to your `.csproj`:

```xml
<PackageReference Include="Purview.Telemetry.SourceGenerator">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>analyzers</IncludeAssets>
</PackageReference>
```

See [Getting Started](Getting-Started.md) for more details.

### Why do I need `PrivateAssets` and `IncludeAssets`?

- `PrivateAssets="all"` — prevents the package from being exposed to consuming projects
- `IncludeAssets="analyzers"` — includes only what is needed for source generation

### Do I need any other packages?

Depends on what you generate:

- **Activities** — no additional packages (uses `System.Diagnostics.DiagnosticSource`)
- **Logging** — `Microsoft.Extensions.Logging.Abstractions`; optionally `Microsoft.Extensions.Telemetry.Abstractions` for v2 features
- **Metrics** — no additional packages (uses `System.Diagnostics.Metrics`)
- **DI** — `Microsoft.Extensions.DependencyInjection.Abstractions` (usually already present)

### How do I view the generated code?

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

Generated files appear in `obj/Debug|Release/<tfm>/generated/Purview.Telemetry.SourceGenerator/Purview.Telemetry.SourceGenerator.TelemetrySourceGenerator/`.

## Activities

### When should I use Activities vs Logging vs Metrics?

- **Activities** — distributed operations across services, end-to-end latency, trace spans
- **Logging** — events, state changes, debugging with structured data, error details
- **Metrics** — counting occurrences, distributions, gauges for dashboards

**Pro tip:** use [Multi-Targeting](Multi-Targeting.md) to combine all three.

### Why does my Activity method return `Activity?` instead of `Activity`?

Activities can be `null` when no listeners are subscribed to the ActivitySource or sampling determines the activity should not be recorded. Always return `Activity?` and guard against `null`.

### Should I use `Activity.Current` or pass Activity parameters?

**Always pass Activity parameters explicitly.** `Activity.Current` may not be the activity you expect, especially in async code or with nested activities.

```csharp
// Good
[Event]
void OrderProcessed(Activity? activity, int orderId);

// Avoid
[Event]
void OrderProcessed(int orderId); // uses Activity.Current implicitly
```

### What's the difference between `[Tag]` and `[Baggage]`?

- **`[Tag]`** — added as tags to the Activity or ActivityEvent; recorded with the activity but not automatically propagated to child activities
- **`[Baggage]`** — added as baggage; automatically propagated to child activities and across service boundaries

Use baggage sparingly as it increases overhead; use tags for most properties.

## Logging

### What's the difference between Generation v1 and v2?

See [Logging](Logging.md) for the full comparison. In brief:

- **Generation v2** — state-based output resembling the built-in `[LoggerMessage]` generator; supports dynamic message templates, `[ExpandEnumerable]`, and `[LogProperties]`; requires `Microsoft.Extensions.Telemetry.Abstractions`.
- **Generation v1** — `LoggerMessage.Define` high-performance mode; limited to 6 non-exception parameters plus one `Exception`; no `[ExpandEnumerable]`/`[LogProperties]`.

The default `LoggerGenerationMode.Auto` selects the best mode **per method**.

### How do I create scoped logs?

Return `IDisposable?` from a log method:

```csharp
[Logger]
interface IOrderTelemetry
{
    [Info]
    IDisposable? ProcessingOrder(Guid orderId);
}

using (telemetry.ProcessingOrder(orderId))
{
    // Duration logged automatically when disposed
}
```

### Can I customize log message templates?

Yes — `[Log].MessageTemplate` sets the template. Placeholders map to method parameters:

```csharp
[Info("Order {OrderId} placed for {CustomerName}")]
void OrderPlaced(int orderId, string customerName);
```

If no template is specified, one is generated from the method name and parameters.

### How do I disable logging generation?

```xml
<PropertyGroup>
  <DefineConstants>EXCLUDE_PURVIEW_TELEMETRY_LOGGING</DefineConstants>
</PropertyGroup>
```

Useful when `Microsoft.Extensions.Logging` types are not available.

## Metrics

### What's the difference between Counter and AutoCounter?

- **`[Counter]`** — you specify the measurement value:

  ```csharp
  [Counter]
  void ItemsProcessed([InstrumentMeasurement]int count, [Tag]string type);
  ```

- **`[AutoCounter]`** — automatically increments by 1 per call:

  ```csharp
  [AutoCounter]
  void ItemProcessed([Tag]string type);
  ```

### When should I use Histogram vs Counter?

- **Counter** — discrete events that only go up (requests, errors, completions)
- **Histogram** — distributions where percentiles matter (latency, size, duration)

### What are observable metrics and when should I use them?

Observable instruments (`[ObservableCounter]`, `[ObservableGauge]`, `[ObservableUpDownCounter]`) are pull-based — the collector calls your `Func<T>` to get the current value. Use them for values that already exist (memory usage, queue depth, cache size) or expensive calculations you don't want to run on every update.

```csharp
[ObservableGauge]
void QueueDepth(Func<int> measurement);

telemetry.QueueDepth(() => _queue.Count);
```

### When do metric names include the meter name?

Instrument names are generated with the meter name as a lowercase dot-separated prefix when using `NamingConvention.OpenTelemetry` combined with `MeterNameGenerationType.OpenTelemetry`:

```csharp
[Meter("MyApp.Orders")]
interface IOrderMetrics
{
    [Counter]
    void OrderProcessed([InstrumentMeasurement]int count);
}

// Generated name: "myapp.orders.order.processed"
//                  ^^^^^^^^^^^^^^^ meter prefix (lowercase)
//                                 ^^^^^^^^^^^^^^^^ instrument name
```

The default `MeterNameGenerationType.DotNet` does not add the meter-name prefix. See [Metrics](Metrics.md#meter-naming).

## Multi-targeting

### Can I generate Activities, Logging, AND Metrics from one interface?

Yes — this is called [Multi-Targeting](Multi-Targeting.md):

```csharp
[ActivitySource("MyApp")]
[Logger]
[Meter("MyApp")]
interface IMyTelemetry
{
    [Activity]
    [Info]
    [AutoCounter]
    Activity? ProcessingRequest([Baggage]string requestId);
}
```

### What's the difference between single-target and multi-target interfaces?

- **Single-target** — one class-level attribute; method-level inference is supported.
- **Multi-target** — multiple class-level attributes; every method must declare its targets explicitly (no inference).

## Configuration & generation

### How do I control the generated class name?

```csharp
[TelemetryGeneration(ClassName = "MyCustomTelemetry")]
[ActivitySource("MyApp")]
interface IMyTelemetry { }
```

### How do I disable dependency injection generation?

```csharp
[TelemetryGeneration(GenerateDependencyExtension = false)]
[ActivitySource("MyApp")]
interface IMyTelemetry { }
```

### How do I exclude a method from generation?

Use `[Exclude]` and implement it manually in a partial class:

```csharp
[ActivitySource("MyApp")]
interface IMyTelemetry
{
    [Activity]
    Activity? NormalMethod(int id);

    [Exclude]
    void CustomMethod(int id);
}

partial class MyTelemetryCore
{
    public void CustomMethod(int id)
    {
        // your custom implementation
    }
}
```

## Migration

### What are the breaking changes in v4 and v5?

Two major v4 changes — **namespace consolidation** into `Purview.Telemetry` and **OpenTelemetry naming** — plus v5 changes including the meter default-name resolution. See [Breaking Changes](Breaking-Changes.md).

### How do I keep v3 naming?

```csharp
[assembly: TelemetryGeneration(NamingConvention = NamingConvention.Legacy)]
```

See [OpenTelemetry-Aligned Naming](Breaking-Changes.md#opentelemetry-aligned-naming).

### Can I use v4 alongside v3?

Not in the same project. In a multi-project solution different projects can use different versions, though that is not recommended.

## Troubleshooting

### The generator isn't producing any code

1. The interface has a class-level attribute (`[ActivitySource]`, `[Logger]`, or `[Meter]`)
2. Methods have appropriate method-level attributes
3. The interface is not generic (generics are not supported)
4. Rebuild the project to trigger generation
5. Check the Error List for `TSG` diagnostics

### I'm getting TSG diagnostic errors

See [Diagnostics](Diagnostics.md) for the complete list of codes and meanings.

### Generated code doesn't compile

Common causes: missing packages (`Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Telemetry.Abstractions` for v2), wrong parameter types, incorrect attribute usage, or generic interfaces/methods.

### Can I use async methods?

The generated methods are synchronous, but your calling code can be async — telemetry operations are lightweight and non-blocking.

## Performance

### Does using a source generator impact performance?

No runtime impact — the code is generated at compile time. The generated code is as fast as hand-written telemetry with minimal allocations. See [Performance](Performance.md).

## Advanced topics

### Can I customize the generated code?

Not directly, but you can use `[Exclude]` with partial-class implementations, control naming via `[TelemetryGeneration]`, and configure defaults through the assembly-level generation attributes.

### Does this work with .NET Native AOT?

The source generator itself works with AOT. Generated code uses no reflection and is trimmable; validate your full scenario with AOT analysis enabled.

### Can I use this in a library?

Yes. Generated implementation and DI classes are internal by default, so they don't leak to consumers. Make the DI class public with `[TelemetryGeneration(DependencyInjectionClassIsPublic = true)]` if you need public registration helpers.

### How do I test code that uses telemetry interfaces?

Just mock the interface — see [Testing](Testing.md):

```csharp
var mockTelemetry = Substitute.For<IOrderTelemetry>();
var service = new OrderService(mockTelemetry);
```

## Getting help

- **Documentation** — [purview.dev](https://purview.dev/docs/telemetry-sourcegenerator/)
- **Issues** — [GitHub Issues](https://github.com/purview-dev/telemetry-sourcegenerator/issues)
- **Sample** — [Sample Application](Sample-Application.md)

When reporting a bug, include the package version, .NET version, a minimal repro, expected vs actual behaviour, and any `TSG` diagnostics.