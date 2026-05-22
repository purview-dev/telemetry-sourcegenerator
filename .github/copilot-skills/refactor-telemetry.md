# Refactor Telemetry to Purview Interfaces

Refactor raw `ILogger`/`ILogger<T>`, `ActivitySource`, and .NET Metrics instruments (`Counter<T>`, `Histogram<T>`, `UpDownCounter<T>`) into strongly-typed Purview Telemetry Source Generator interfaces.

## When to Use This Skill

Use this skill when you see any of the following in a class:

- `ILogger` or `ILogger<T>` fields/properties with `.Log*` calls
- `ActivitySource` fields/properties with `.StartActivity(...)` calls
- `Counter<T>`, `Histogram<T>`, or `UpDownCounter<T>` fields with `.Add(...)` or `.Record(...)` calls

## Naming Conventions

| Mode     | Interface Name(s)                                              |
|----------|---------------------------------------------------------------|
| Combined | `I{ClassName}Telemetry`                                       |
| Logging  | `I{ClassName}Logs`                                            |
| Tracing  | `I{ClassName}Tracing`                                         |
| Metrics  | `I{ClassName}Metrics`                                         |

## Interface Structure

### Combined Interface

When all three telemetry types are present, generate a single multi-target interface:

```csharp
using Purview.Telemetry;

[ActivitySource]
[Logger]
[Meter]
public interface I{ClassName}Telemetry
{
    // Activity methods first
    [Activity]
    System.Diagnostics.Activity? StartSomeOperation(string param);

    // Log methods second
    [Info("Something happened with {Param}")]
    void SomethingHappened(string param);

    // Metrics methods third
    [AutoCounter]
    void IncrementCounter();
}
```

### Split Interfaces

When refactoring individual types separately:

```csharp
[ActivitySource]
public interface I{ClassName}Tracing { ... }

[Logger]
public interface I{ClassName}Logs { ... }

[Meter]
public interface I{ClassName}Metrics { ... }
```

## Mapping Rules

### ILogger → `[Logger]` Interface

| Source call | Generated method |
|---|---|
| `_logger.LogTrace("Message {P}", p)` | `[Trace("Message {P}")] void Message(T p);` |
| `_logger.LogDebug("Message {P}", p)` | `[Debug("Message {P}")] void Message(T p);` |
| `_logger.LogInformation("Message {P}", p)` | `[Info("Message {P}")] void Message(T p);` |
| `_logger.LogWarning("Message {P}", p)` | `[Warning("Message {P}")] void Message(T p);` |
| `_logger.LogError(ex, "Message {P}", p)` | `[Error("Message {P}")] void Message(Exception exception, T p);` |
| `_logger.LogCritical("Message {P}", p)` | `[Critical("Message {P}")] void Message(T p);` |

**Method name derivation**: Split message template on words and PascalCase each word.
`"Getting weather for {City}"` → `GettingWeatherFor(string city)`

**Class rewrite**: Replace `ILogger<T>` field type with `I{ClassName}Logs`. Replace constructor parameter type. Replace all `.LogX(...)` call sites with `_logger.MethodName(args)`.

### ActivitySource → `[ActivitySource]` Interface

| Source call | Generated method |
|---|---|
| `_source.StartActivity("name")` | `[Activity] Activity? Name(...)` |
| `_source.StartActivity("name", ActivityKind.Client)` | `[Activity(ActivityKind.Client)] Activity? Name(...)` |

**Method name derivation**: Split activity name on separators/camel-case, then PascalCase.
`"get-weather"` → `GetWeather()`, `"processPayment"` → `ProcessPayment()`

**Class rewrite**: Replace `ActivitySource` field type with `I{ClassName}Tracing`. Replace `.StartActivity(name, kind?)` calls with `_source.MethodName()`. Add `using System.Diagnostics;`.

### Metrics → `[Meter]` Interface

| Source call | Generated method |
|---|---|
| `_counter.Add(1)` | `[AutoCounter] void Counter();` |
| `_counter.Add(count)` | `[Counter] void Counter(int count);` |
| `_counter.Add(n, tag1, tag2)` | `[Counter] void Counter(int n, string tag1, string tag2);` |
| `_histogram.Record(ms)` | `[Histogram] void DurationMs(double ms);` |
| `_upDown.Add(1)` | `[UpDownCounter] void UpDown(int value);` |

**Auto-counter detection**: If the first argument to `.Add(...)` is the literal `1`, generate `[AutoCounter]` (no value parameter). Otherwise generate `[Counter]` with the measurement value as first parameter.

**Method name derivation**: PascalCase the field name, stripping leading `_` and suffixes like `Counter`/`Histogram`/`Meter`.

**Class rewrite**: Replace `Counter<T>` / `Histogram<T>` / `UpDownCounter<T>` field types with the interface. Rewrite call sites to use the interface method. For `[AutoCounter]` calls, remove all arguments.

## Step-by-Step Refactoring Instructions

### Step 1: Identify Telemetry in the Class

Look at the class fields, properties, and primary constructor parameters. Find:

1. Any `ILogger` or `ILogger<T>` → logging candidate
2. Any `ActivitySource` → tracing candidate
3. Any `Counter<T>`, `Histogram<T>`, `UpDownCounter<T>` → metrics candidate

### Step 2: Choose Combined vs Split

- **Combined** (`I{ClassName}Telemetry`): when the class uses 2 or more telemetry types
- **Split**: when only one type is present, or when the user prefers separate interfaces

### Step 3: Generate the Interface

For each telemetry type detected, generate interface methods using the mapping rules above.

Example — a class with all three:

```csharp
// Before
public class WeatherService(
    ILogger<WeatherService> logger,
    ActivitySource activitySource,
    Counter<int> requestCounter)
{
    public async Task<Weather> GetWeatherAsync(string city)
    {
        using var activity = activitySource.StartActivity("get-weather");
        logger.LogInformation("Getting weather for {City}", city);
        requestCounter.Add(1);
        // ...
    }
}
```

```csharp
// After
using Purview.Telemetry;
using System.Diagnostics;

[ActivitySource]
[Logger]
[Meter]
public interface IWeatherServiceTelemetry
{
    [Activity]
    Activity? GetWeather();

    [Info("Getting weather for {City}")]
    void GettingWeatherFor(string city);

    [AutoCounter]
    void RequestCounter();
}

public class WeatherService(IWeatherServiceTelemetry telemetry)
{
    public async Task<Weather> GetWeatherAsync(string city)
    {
        using var activity = telemetry.GetWeather();
        telemetry.GettingWeatherFor(city);
        telemetry.RequestCounter();
        // ...
    }
}
```

### Step 4: Register in DI

After generating the interface, add DI registration:

```csharp
// In Program.cs or wherever services are configured
services.AddWeatherServiceTelemetry(); // auto-generated extension method
```

The source generator creates a `services.Add{ClassName}Telemetry()` extension for each generated implementation.

### Step 5: Update Using Directives

Ensure these usings are present in files that use the interface:

- `using Purview.Telemetry;` — always needed (interface + attributes)
- `using System.Diagnostics;` — needed for `Activity?` return types
- Remove: `using Microsoft.Extensions.Logging;` (unless needed elsewhere)
- Remove: `using System.Diagnostics.Metrics;` (unless needed elsewhere)

## Roslyn Code Refactoring Providers

This repository ships four Roslyn refactoring providers (available via IDE / VS Code light-bulb):

| Provider | Action Title |
|---|---|
| `ConvertILoggerToTelemetryRefactoringProvider` | `Convert ILogger to I{ClassName}Logs` |
| `ConvertActivitySourceToTelemetryRefactoringProvider` | `Convert ActivitySource to I{ClassName}Tracing` |
| `ConvertMetricsToTelemetryRefactoringProvider` | `Convert Metrics to I{ClassName}Metrics` |
| `ConvertAllTelemetryToInterfaceRefactoringProvider` | `Convert all telemetry to I{ClassName}Telemetry` |

Place the cursor anywhere on a class declaration and trigger refactorings (Ctrl+. / Cmd+.) to see the available actions.

## Important Notes

- The generated interface is placed **before** the class in the same file. Move it to its own file if needed.
- Method names are derived from message templates or activity/field names. Review and rename if the auto-derived name is unclear.
- If a class has multiple `ILogger` fields (e.g., from dependency injection + local creation), they are all replaced with a single interface parameter.
- Tags/extra arguments on metric calls become `string tagN` parameters by default. Rename them to meaningful names after generation.
- For `[AutoCounter]`, the generated call site has **no arguments** — remove the `1` literal from the original call.
