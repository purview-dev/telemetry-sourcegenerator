# Getting Started

This guide gets you up and running with the Purview Telemetry Source Generator in minutes. You will add the package, define a telemetry interface, register it with dependency injection, and start emitting Activities, structured logs, and Metrics.

## Installation

Add the analyzer package to your project:

```bash
dotnet add package Purview.Telemetry.SourceGenerator
```

Or reference it in your `.csproj` or `Directory.Build.props`:

```xml
<PackageReference Include="Purview.Telemetry.SourceGenerator">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>analyzers</IncludeAssets>
</PackageReference>
```

You may also need runtime dependencies depending on which telemetry types you use:

- `System.Diagnostics.DiagnosticSource` for Activities
- `Microsoft.Extensions.Logging.Abstractions` for `ILogger`
- `System.Diagnostics.Metrics` for metrics (built into .NET 8+)

See [Installation](Installation.md) for the full dependency matrix.

## Define a telemetry interface

Create a `public interface` and decorate it with class-level attributes. The generator creates the implementation and a DI registration extension.

### Single-target examples

```csharp
using Purview.Telemetry;

[Logger]
public interface IOrderServiceLogs
{
    [Info]
    void OrderPlaced(int orderId, string customerName);

    [Warning]
    void OrderNotFound(int orderId);
}
```

```csharp
[ActivitySource]
public interface IOrderServiceTracing
{
    [Activity]
    Activity? PlacingOrder(int orderId);

    [Event]
    void OrderValidated(Activity? activity, int orderId);
}
```

```csharp
[Meter]
public interface IOrderServiceMetrics
{
    [Counter]
    void OrderPlaced(int itemsInOrder);

    [Histogram]
    void OrderProcessingTime(long milliseconds);
}
```

### Multi-target example

One interface can generate Activities, Logging, and Metrics from the same methods:

```csharp
[ActivitySource]
[Logger]
[Meter]
public interface IOrderServiceTelemetry
{
    [Activity]
    [Info]
    [AutoCounter]
    Activity? PlacingOrder(int orderId, [Baggage] string region);

    [Event]
    [Trace]
    void OrderProcessed(Activity? activity, long durationMs);

    [Warning]
    void OrderFailed(int orderId, Exception exception);
}
```

## Register with DI

The generator creates an extension method named `Add{InterfaceNameWithoutI}()`:

```csharp
services.AddOrderServiceTelemetry();
```

Inject the interface into your service:

```csharp
public class OrderService(IOrderServiceTelemetry telemetry)
{
    public void PlaceOrder(int orderId)
    {
        using var activity = telemetry.PlacingOrder(orderId, "EMEA");
        // ...
        telemetry.OrderProcessed(activity, stopwatch.ElapsedMilliseconds);
    }
}
```

A single method call can emit an Activity, a log entry, and a metric simultaneously. See [Generation](Generation.md) for how registration helpers are produced and how to control them.

## Register names with OpenTelemetry

The generator also produces a `TelemetryNames` static class containing the meter and activity source names:

```csharp
builder.AddServiceDefaults(TelemetryNames.MeterNames, TelemetryNames.ActivitySourceNames);
```

## Common patterns

### Pattern 1: Scoped logging

Return `IDisposable?` from a log method to create scoped log entries:

```csharp
[Logger]
interface IOrderTelemetry
{
    [Info]
    IDisposable? ProcessingOrder(Guid orderId);  // Logs at start and end of scope

    [Error]
    void OrderFailed(Exception ex, Guid orderId);
}

public async Task ProcessOrderAsync(Guid orderId)
{
    using (telemetry.ProcessingOrder(orderId))
    {
        // Processing logic here
    }
}
```

### Pattern 2: Activity with multiple events

Track multiple stages within a single activity:

```csharp
[ActivitySource("ShippingService")]
interface IShippingTelemetry
{
    [Activity]
    Activity? ShippingPackage([Baggage]string trackingNumber);

    [Event]
    void PackageLabeled(Activity? activity);

    [Event]
    void PackageWeighed(Activity? activity, [Tag]decimal weight);

    [Event]
    void PackageShipped(Activity? activity, [Tag]string carrier);
}
```

### Pattern 3: Auto-incrementing counters

Use `[AutoCounter]` for simple counting without a measurement parameter — every call increments by 1:

```csharp
[Meter("ApiService")]
interface IApiMetrics
{
    [AutoCounter]
    void RequestReceived([Tag]string endpoint, [Tag]string method);

    [AutoCounter]
    void RequestFailed([Tag]string endpoint, [Tag]int statusCode);
}
```

## Viewing generated code

To inspect the generated source, add this to your `.csproj`:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

Generated files appear under `obj/Debug|Release/<tfm>/generated/Purview.Telemetry.SourceGenerator/Purview.Telemetry.SourceGenerator.TelemetrySourceGenerator/`.

## Tips and best practices

1. **Return `Activity?`** from Activity-starting methods, not `Activity` or `void`, so callers can dispose and reuse the activity.
2. **Pass activities explicitly** — pass the `Activity?` from the starting method to event/context methods rather than relying on `Activity.Current`.
3. **One namespace** — use the single `using Purview.Telemetry;` import.
4. **OpenTelemetry naming** — v5 defaults to OpenTelemetry conventions (snake_case tags, hierarchical metrics). See [Naming conventions](Generation.md#naming-conventions).
5. **DI registration** — use the generated `Add{InterfaceName}()` extension methods.
6. **Testing** — telemetry interfaces are easy to mock in unit tests. See [Testing](Testing.md).

## Next steps

- [Activities](Activities.md) — deep dive into distributed tracing with Activities, Events, and Context
- [Logging](Logging.md) — structured logging and the v1/v2 generation modes
- [Metrics](Metrics.md) — counters, histograms, and observable instruments
- [Multi-Targeting](Multi-Targeting.md) — combine multiple telemetry types in one interface
- [Generation Options](Generation.md) — control code generation, DI, and naming
- [Generated Output](Generated-Output.md) — see what code is actually generated
- [Migration](Refactorings.md) — convert existing `ILogger`/`ActivitySource`/metrics code using the IDE refactorings