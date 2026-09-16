# Refactorings

The NuGet package ships four Visual Studio code refactorings alongside the source generator. Right-click a class containing hand-written telemetry and choose the relevant conversion — the refactoring creates a generated telemetry interface and rewrites the class to use it.

## Available refactorings

| Refactoring | Converts | Generated interface |
| --- | --- | --- |
| **Convert ILogger to I{ClassName}Logs** | Hand-written `ILogger`/`ILogger<T>` fields, properties, constructor parameters, and `Log*`/`Log` calls | `[Logger]` interface |
| **Convert ActivitySource to I{ClassName}Tracing** | `ActivitySource` fields/properties/parameters and `StartActivity(...)` calls | `[ActivitySource]` interface |
| **Convert Metrics to I{ClassName}Metrics** | `Counter<T>`, `Histogram<T>`, `UpDownCounter<T>` fields/properties and `.Add(...)`/`.Record(...)` calls | `[Meter]` interface |
| **Convert all telemetry to I{ClassName}Telemetry** | Any combination of the above in the same class | Single `[ActivitySource]`+`[Logger]`+`[Meter]` interface |

Each refactoring offers scope options:

- **In this class** — converts the selected class only
- **In this document** — converts all matching classes in the file
- **In this project** — converts all matching classes in the project
- **In this solution** — converts all matching classes in the solution

## Convert ILogger to I{ClassName}Logs

- `LogTrace` → `[Trace]`, `LogDebug` → `[Debug]`, `LogInformation` → `[Info]`, `LogWarning` → `[Warning]`, `LogError` → `[Error]`, `LogCritical` → `[Critical]`; `Log(LogLevel.X, ...)` maps to the matching semantic attribute (unmapped levels fall back to `[Log(LogLevel.X, ...)]`).
- Literal message templates are embedded in the attribute: `[Info("Getting weather for {City}")]`.
- Literal `int` event IDs are embedded: `[Info(42, "...")]`.
- `Exception` parameters become an `exception` parameter of type `System.Exception`.
- Method names derive from the message-template words (PascalCased), deduplicated with numeric suffixes.
- Multiple logger fields/parameters on one constructor are consolidated into a single canonical injection.

The refactoring fires when a class contains at least one `ILogger`/`ILogger<T>` member **and** at least one recognized `Log*`/`Log` call on it.

## Convert ActivitySource to I{ClassName}Tracing

- Each `StartActivity("name")` call becomes an interface method returning `Activity?`, decorated `[Activity]` (for kind `Internal`/unspecified) or `[Activity(ActivityKind.X)]` for explicit kinds.
- Method names derive from the activity-name string, PascalCased (for example, `"get-weather"` → `GetWeather`); duplicates get numeric suffixes.
- `ActivitySource`-typed fields/properties/parameters are rewritten to the interface type, and `StartActivity(...)` invocations become interface method calls.

The refactoring fires when a class has at least one `ActivitySource` member **and** at least one `.StartActivity(...)` call on it.

## Convert Metrics to I{ClassName}Metrics

- `Counter<T>.Add(1)` (literal 1) → `[AutoCounter]` method with no parameters.
- `Counter<T>.Add(value)` → `[Counter] void Method(T value)`.
- `Histogram<T>.Record(value)` → `[Histogram] void Method(T value)`.
- `UpDownCounter<T>.Add(value)` → `[UpDownCounter] void Method(T value)`.
- Additional tag arguments become `string tag1`, `string tag2`, ... parameters.
- Method names derive from the field name with instrument suffixes (`UpDownCounter`, `Histogram`, `Counter`, `Gauge`, `Meter`) stripped, then PascalCased.
- The `Meter` itself is not converted — the constructor's `Meter`/`IMeterFactory` usage and `CreateCounter<T>` registration calls remain.

The refactoring fires when a class has at least one metrics instrument member **and** at least one `.Add(...)`/`.Record(...)` call on it.

> [!NOTE]
> `System.Diagnostics.Metrics` is not available on .NET Framework, so the metrics refactoring is not available for those target frameworks.

## Convert all telemetry to I{ClassName}Telemetry

Composes the per-family conversions into a single interface decorated with `[ActivitySource]`, `[Logger]`, and/or `[Meter]` for only the families actually present (multi-targeting). It fires when a class uses any combination of logger/ActivitySource/metrics members **and** has at least one corresponding call.

## What the refactorings produce

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

If a refactoring does not cover a call pattern, convert it manually:

1. Identify the telemetry type (Logging, Activity, Metric).
2. Create a new interface with the matching class-level attribute (`[Logger]`, `[ActivitySource]`, `[Meter]`).
3. Add a method for each distinct operation, using the method-level attribute from the mapping below.
4. Replace the hand-written call with the interface method.
5. Register the interface in DI with `services.Add{InterfaceNameWithoutI}()`.

| Hand-written telemetry | Generated attribute |
| --- | --- |
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
| `Counter<T>.Add(...)` | `[Counter]`, or `[AutoCounter]` for `Add(1)` |
| `Histogram<T>.Record(...)` | `[Histogram]` |
| `UpDownCounter<T>.Add(...)` | `[UpDownCounter]` |

## See also

- [Migration from ILogger](Migration-From-ILogger.md)
- [Migration from Activities](Migration-From-Activities.md)