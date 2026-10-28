---
name: purview-telemetry-quickstart
description: Get started with Purview.Telemetry.SourceGenerator in a C# project. Use when adding the package, creating a first telemetry interface, registering it in DI, or asking how to begin with Purview telemetry.
license: MIT
compatibility: C# projects using Purview.Telemetry.SourceGenerator.
metadata:
  author: Purview-Dev
  version: "1.0"
---

# Quickstart: Purview Telemetry Source Generator

This skill helps you bootstrap telemetry generation in a C# project using `Purview.Telemetry.SourceGenerator`.

## Installation

Add the analyzer package to your project. `dotnet add package` writes the latest version; never copy a version
number from documentation, it goes stale:

```bash
dotnet add package Purview.Telemetry.SourceGenerator
```

Then make the reference analyzer-only (keep the `Version` that `dotnet add package` wrote, or set it in
`Directory.Packages.props` under Central Package Management):

```xml
<PackageReference Include="Purview.Telemetry.SourceGenerator">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>analyzers</IncludeAssets>
</PackageReference>
```

Consumers can target .NET Framework 4.8 or .NET 8 and later, .NET 11 included, and the generated code works under
Native AOT and in trimmed or AOT-compiled Blazor WebAssembly. Runtime dependencies for the telemetry types you use:

- Activities: `System.Diagnostics.DiagnosticSource` (in-box on .NET).
- Logging: `Microsoft.Extensions.Logging.Abstractions`. It is in the shared framework on .NET 11, so do not reference
  it there (NuGet reports `NU1510`). Add `Microsoft.Extensions.Telemetry.Abstractions` for the faster v2 logging.
- Metrics: `System.Diagnostics.Metrics` (in-box on .NET 8+). Register `services.AddMetrics()` for the `IMeterFactory`.
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

On .NET 11 you can opt in to creating activity sources from the injected `ActivitySourceFactory`. This changes the
generated constructor, so also register the factory:

```csharp
[assembly: ActivitySourceGeneration("MyApp.Orders", UseActivitySourceFactory = true)]

// in the host
services.AddTracing();
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

The generator records the generated meter and activity source names in every assembly's metadata via `[assembly: GeneratedTelemetryNames]`. In the project that registers them with OpenTelemetry, opt into the `TelemetryNames` class — it aggregates that project's names with the names recorded by every assembly it references:

```csharp
// Program.cs or Properties/AssemblyInfo.cs of the host project
[assembly: TelemetryGeneration(GenerateTelemetryNamesClass = true)]
```

```csharp
builder.AddServiceDefaults(TelemetryNames.MeterNames, TelemetryNames.ActivitySourceNames);
```

Libraries need no configuration: they publish their names through the attribute, and the host picks them up. A project referenced *by* the telemetry owners (an Aspire-style `ServiceDefaults`) cannot aggregate them — generate the class in the host and pass the arrays down.

## Next steps

- To migrate existing `ILogger`, `ActivitySource`, or metrics code, use the `purview-telemetry-migration` skill.
- For design guidance and best practices, use the `purview-telemetry-design` skill.
