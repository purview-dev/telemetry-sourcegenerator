---
name: telemetry-sourcegenerator-migration
description: Migrate existing ILogger, ActivitySource, or System.Diagnostics.Metrics instrument code to Purview.Telemetry.SourceGenerator interfaces. Use when converting hand-written telemetry to generated telemetry interfaces, or when a user wants to replace ILogger, ActivitySource, or Counter/Histogram/UpDownCounter usage with Purview telemetry.
license: ISC
compatibility: C# projects using Purview.Telemetry.SourceGenerator.
metadata:
  author: Purview-Dev
  version: "1.0"
---

# Migrate to Purview Telemetry Source Generator

Convert existing telemetry code to generated Purview telemetry interfaces.

## Primary path: use the IDE refactorings

The source generator ships with Roslyn code refactorings. Right-click a class containing hand-written telemetry and choose:

- **Convert ILogger to I<ClassName>Logs** — replaces `ILogger` fields and constructor parameters with a generated logging interface.
- **Convert ActivitySource to I<ClassName>Tracing** — replaces `ActivitySource` fields and `StartActivity` calls with a generated tracing interface.
- **Convert Metrics to I<ClassName>Metrics** — replaces `Counter<T>`, `Histogram<T>`, and `UpDownCounter<T>` fields and calls with a generated metrics interface.
- **Convert all telemetry to I<ClassName>Telemetry** — combines all three into one interface when the class uses multiple telemetry types.

Each refactoring offers scopes: **class**, **document**, **project**, and **solution**.

## What the refactorings produce

The generated interface uses attributes from the `Purview.Telemetry` namespace:

```csharp
using Purview.Telemetry;

[Logger]
public interface IOrderServiceLogs
{
    [Info]
    void OrderPlaced(int orderId, string customerName);
}
```

The original class is rewritten to use the interface:

```csharp
public class OrderService(IOrderServiceLogs logger)
{
    public void PlaceOrder(int orderId, string customerName)
    {
        logger.OrderPlaced(orderId, customerName);
    }
}
```

## Manual fallback

If the refactoring does not cover a call pattern, convert it manually:

1. Identify the telemetry type (Logging, Activity, Metric).
2. Create a new interface with the matching class-level attribute (`[Logger]`, `[ActivitySource]`, `[Meter]`).
3. Add a method for each distinct operation, using the method-level attribute from the mapping below.
4. Replace the hand-written call with the interface method.
5. Register the interface in DI with `services.Add{InterfaceNameWithoutI}()`.

## Attribute mapping

| Hand-written telemetry | Generated attribute |
|---|---|
| `ILogger.LogInformation(...)` | `[Info]` |
| `ILogger.LogDebug(...)` | `[Debug]` |
| `ILogger.LogTrace(...)` | `[Trace]` |
| `ILogger.LogWarning(...)` | `[Warning]` |
| `ILogger.LogError(...)` | `[Error]` |
| `ILogger.LogCritical(...)` | `[Critical]` |
| `ActivitySource.StartActivity(...)` | `[Activity]` |
| `activity.AddEvent(...)` | `[Event]` |
| `activity.AddBaggage(...)` / `SetBaggage(...)` | `[Context]` with `[Baggage]` parameter |
| `activity.SetTag(...)` | `[Context]` with `[Tag]` parameter |
| `Counter<T>.Add(...)` | `[Counter]` or `[AutoCounter]` for `Add(1)` |
| `Histogram<T>.Record(...)` | `[Histogram]` |
| `UpDownCounter<T>.Add(...)` | `[UpDownCounter]` |

See [references/REFERENCE.md](references/REFERENCE.md) for detailed before/after examples and common conversions.
