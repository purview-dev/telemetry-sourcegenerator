---
name: telemetry-sourcegenerator-quickstart
description: Get started with Purview.Telemetry.SourceGenerator in a C# project. Use when adding the package, creating a first telemetry interface, registering it in DI, or asking how to begin with Purview telemetry.
license: ISC
compatibility: C# projects using Purview.Telemetry.SourceGenerator.
metadata:
  author: Purview-Dev
  version: "1.0"
---

# Quickstart: Purview Telemetry Source Generator

This skill helps you bootstrap telemetry generation in a C# project using `Purview.Telemetry.SourceGenerator`.

## Installation

Add the analyzer package to your project:

```xml
<PackageReference Include="Purview.Telemetry.SourceGenerator" Version="4.3.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>analyzers</IncludeAssets>
</PackageReference>
```

Also add runtime dependencies for the telemetry types you use:

- `System.Diagnostics.DiagnosticSource` for Activities
- `Microsoft.Extensions.Logging.Abstractions` for ILogger
- `System.Diagnostics.Metrics` for metrics (built-in on .NET 6+)

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

## Register names with OpenTelemetry

The generator also produces a `TelemetryNames` static class containing the meter and activity source names:

```csharp
builder.AddServiceDefaults(TelemetryNames.MeterNames, TelemetryNames.ActivitySourceNames);
```

## Next steps

- To migrate existing `ILogger`, `ActivitySource`, or metrics code, use `telemetry-sourcegenerator-migration`.
- For design guidance and best practices, use `telemetry-sourcegenerator-design`.
