# Purview Telemetry Source Generator

Generates [`ActivitySource`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysource), [`ILogger`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger), and [`Metrics`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics) based telemetry from methods you define on an interface.

[![CI](https://github.com/kjldev/purview-telemetry-sourcegenerator/actions/workflows/ci.yml/badge.svg)](https://github.com/kjldev/purview-telemetry-sourcegenerator/actions/workflows/ci.yml)

## Features

- **Zero boilerplate** - define methods on an interface, get full telemetry implementation generated
- **Multi-target generation** - generate Activities, Logging, and Metrics from a single interface
- **Testable** - easy mocking/substitution for unit testing
- **DI-ready** - automatic dependency injection registration helpers

## Performance

The source generator produces code that matches — or outperforms — handwritten best-practice telemetry. Benchmarks were run with [BenchmarkDotNet](https://benchmarkdotnet.org/) on .NET 10.0.4 (X64 RyuJIT AVX2, Intel Core Ultra 7 155H).

> [!NOTE]
> All "no listener / no logging" rows represent the fast-path where telemetry is disabled at runtime. The important comparison is the **"active"** rows, which reflect real production behaviour.

### Activities (generated vs. manual)

| Scenario | Manual | Generated | Overhead |
|---|---|---|---|
| Start + complete (no listener) | 0.73 ns | 1.00 ns | +0.27 ns |
| Start + complete (listener active) | 266 ns / 1008 B | 274 ns / 1008 B | **+3%, same allocations** |
| Start + fail (listener active) | 243 ns / 920 B | 242 ns / 920 B | **identical** |

Generated activities are **within 3% of hand-written code** and allocate identically.

### Logging (generated v1 vs. manual approaches)

| Scenario | `ILogger.Log` (manual) | `LoggerMessage.Define` (manual) | Generated (v1) |
|---|---|---|---|
| Single call (logging disabled) | 0.19 ns / 0 B | 0.019 ns / 0 B | 0.061 ns / 0 B |
| Single call (logging enabled) | 28.8 ns / 152 B | 6.13 ns / 0 B | **6.17 ns / 0 B** |
| Full lifecycle — 4 calls (logging enabled) | 105.7 ns / 568 B | 24.3 ns / 0 B | **23.9 ns / 0 B** |

Generated logging (v1) is **on par with `LoggerMessage.Define`** — the gold-standard approach — and is **~4.7× faster** and **allocates ~6× less** than a naïve `ILogger.Log` call.

### Multi-target (Activity + Logging + Metrics combined)

| Scenario | Manual | Generated (v1) |
|---|---|---|
| Start + complete (no listener) | 18 ns / 24 B | 17 ns / 24 B |
| Start + complete (listener active) | 316 ns / 1032 B | **304 ns / 1032 B** |
| Full lifecycle (listener active) | 272 ns / 1032 B | **271 ns / 1032 B** |

Multi-target generation — emitting an Activity, a log entry, and a metric increment from a single method call — is **on par with or faster than equivalent manual code**.

### Metrics (generated vs. manual)

The baseline here is calling the .NET metrics API directly (e.g. `counter.Add(1, tag)`).

| Scenario | Manual (raw API) | Generated | Overhead |
|---|---|---|---|
| Auto-counter (0 tags) | 0.19 ns / 0 B | **~0 ns / 0 B** | **none — JIT-eliminated** |
| Auto-counter (1 tag) | 0.26 ns / 0 B | ~1–3 ns / 0 B | +0.7–2 ns (null-check guard) |
| UpDownCounter | 0.20 ns / 0 B | **0.18 ns / 0 B** | **identical** |
| Histogram (0 tags) | 0.23 ns / 0 B | 0.61 ns / 0 B | +0.4 ns |
| Histogram (1 tag) | ~0.2 ns / 0 B | ~1 ns / 0 B | +0.8 ns |

Generated metrics are **allocation-free** (same as hand-written code). Zero-tag instruments are JIT-eliminated to near-zero, identical to the manual baseline. Instruments with tags add ≤ 3 ns of overhead from a null-check guard on every call — negligible in any realistic workload.

Full benchmark results are in [`BenchmarkDotNet.Artifacts/results/`](BenchmarkDotNet.Artifacts/results/) and [`PERFORMANCE.md`](PERFORMANCE.md).

## Supported Frameworks

- .NET Framework 4.8
- .NET 8 or higher

## Installation

Add to your `Directory.Build.props` or `.csproj` file:

```xml
<PackageReference Include="Purview.Telemetry.SourceGenerator" Version="4.0.0-prerelease.4">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>analyzers</IncludeAssets>
</PackageReference>
```

## Quick Start

Define an interface with telemetry methods and the generator creates the implementation:

```csharp
using Purview.Telemetry;

// Multi-target interface: generates Activities, Logging, AND Metrics from combined methods
[ActivitySource]
[Logger]
[Meter]
interface IEntityStoreTelemetry
{
    // MULTI-TARGET: Creates Activity + Logs Info + Increments Counter - all from one method!
    [Activity]
    [Info]
    [AutoCounter]
    Activity? GettingEntityFromStore(int entityId, [Baggage]string serviceUrl);

    // MULTI-TARGET: Adds ActivityEvent + Logs the duration as Trace.
    [Event]
    [Trace]
    void GetDuration(Activity? activity, int durationInMS);

    // Single-target examples (when you only need one telemetry type):
    
    // Activity-only: Adds Baggage to the Activity
    [Context]
    void RetrievedEntity(Activity? activity, float totalValue, int lastUpdatedByUserId);

    // Log-only: Structured log message
    [Warning]
    void EntityNotFound(int entityId);

    // Metric-only: Histogram for tracking values
    [Histogram]
    void RecordEntitySize(int sizeInBytes);
}
```

Register with dependency injection:

```csharp
// Generated extension method
services.AddEntityStoreTelemetry();
```

Then inject and use - a single method call emits an Activity, Log, and Metric simultaneously:

```csharp
public class EntityService(IEntityStoreTelemetry telemetry)
{
    public async Task<Entity> GetEntityAsync(int id, string serviceUrl, CancellationToken cancellationToken)
    {
        // Single call creates Activity AND logs AND increments counter
        using var activity = telemetry.GettingEntityFromStore(id, serviceUrl);
        
        var entity = await _repository.GetAsync(id, cancellationToken);
                        
        // Adds event to activity AND logs duration
        telemetry.GetDuration(activity, stopwatch.ElapsedMilliseconds);

        if (entity == null)
        {
            // Logs warning if entity not found
            telemetry.EntityNotFound(id);
            return null;
        })

        // Activity context addition
        telemetry.RetrievedEntity(activity, entity.TotalValue, entity.LastUpdatedByUserId);
        
        // Histogram only records size
        telemetry.RecordEntitySize(entity.SizeInBytes);

        return entity;
    }
}
```

## Telemetry Types

| Attribute | Generation Type | Description |
|-----------|----------------|-------------|
| `[ActivitySource]` | Class-level | Marks interface for Activity generation |
| `[Activity]` | Method | Creates and starts a new Activity |
| `[Event]` | Method | Adds an ActivityEvent to an Activity |
| `[Context]` | Method | Adds Baggage to an Activity |
| `[Logger]` | Class-level | Marks interface for ILogger generation |
| `[Log]` | Method | Generates structured log message |
| `[Debug]`, `[Info]`, `[Warning]`, `[Error]`, `[Critical]` | Method | Log with specific level |
| `[Meter]` | Class-level | Marks interface for Metrics generation |
| `[Counter]`, `[AutoCounter]` | Method | Counter instrument |
| `[Histogram]` | Method | Histogram instrument |
| `[ObservableCounter]`, `[ObservableGauge]`, `[ObservableUpDownCounter]` | Method | Observable instruments |

> [!TIP]
> For single-target interfaces (only Activities, only Logging, or only Metrics), the generator automatically infers the necessary attributes. See the [wiki](https://github.com/kjldev/purview-telemetry-sourcegenerator/wiki/Multi-Targeting) for details.

## Documentation

- [Full Wiki](https://github.com/kjldev/purview-telemetry-sourcegenerator/wiki)
- [Generated Output Examples](https://github.com/kjldev/purview-telemetry-sourcegenerator/wiki/Generated-Output)
- [Multi-Targeting Guide](https://github.com/kjldev/purview-telemetry-sourcegenerator/wiki/Multi-Targeting)
- [Logging Configuration](https://github.com/kjldev/purview-telemetry-sourcegenerator/wiki/Logging)

## Sample Project

The [.NET Aspire Sample](https://github.com/kjldev/purview-telemetry-sourcegenerator/tree/main/samples/SampleApp) demonstrates Activities, Logs, and Metrics generation working together with the Aspire Dashboard.

> [!TIP]
> The sample project has [`EmitCompilerGeneratedFiles`](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-generator#enable-the-configuration-source-generator) enabled so you can inspect the generated output.

## v4 Breaking Changes

### Namespace Consolidation

v4 consolidates all attributes into a single namespace. Update your using statements:

**Before (v3):**

```csharp
using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;
using Purview.Telemetry.Metrics;
```

**After (v4):**

```csharp
using Purview.Telemetry;
```

All attributes (`[ActivitySource]`, `[Logger]`, `[Meter]`, `[Activity]`, `[Event]`, `[Log]`, `[Counter]`, etc.) are now in the unified `Purview.Telemetry` namespace.

### OpenTelemetry-Aligned Naming (NEW in v4.0.0-alpha.5+)

v4 defaults to **OpenTelemetry semantic conventions** for generated telemetry names, improving observability and cross-platform compatibility. This is a **breaking change** if you rely on specific telemetry names.

#### What Changed

| Telemetry Type | v3 Behavior | v4 Default | Impact |
|----------------|-------------|------------|--------|
| **ActivitySource Name** | Assembly name lowercased: `"myapp"` | Assembly name preserved: `"MyApp"` | ActivitySource names change casing |
| **Tag/Baggage Keys** | Lowercased, smashed: `"entityid"` | snake_case: `"entity_id"` | Tag keys have underscores for word boundaries |
| **Metric Instrument Names** | Lowercased, smashed: `"recordhistogram"` | Hierarchical dot.separated: `"myapp.products.record.histogram"` | Includes meter name prefix + word boundaries |
| **Metric Tag Keys** | Lowercased, smashed: `"requestcount"` | snake_case: `"request_count"` | Metric tag keys have underscores |

#### Examples

**Before (v3/Legacy):**

```csharp
// Generated code:
new ActivitySource("myapp")           // lowercase
activity.SetTag("entityid", ...)      // smashed compound
var meter = meterFactory.Create("MyApp.Products");
meter.CreateCounter<int>("recordcount")  // smashed compound, no meter prefix
```

**After (v4 OpenTelemetry mode - DEFAULT):**

```csharp
// Generated code:
new ActivitySource("MyApp")           // preserves casing
activity.SetTag("entity_id", ...)     // snake_case
var meter = meterFactory.Create("MyApp.Products");
meter.CreateCounter<int>("myapp.products.record.count")  // hierarchical: meter + instrument
```

**Note**: In OpenTelemetry mode, instrument names automatically include the meter name prefix (converted to lowercase dot.separated), following OpenTelemetry best practices for hierarchical metric naming.

#### Reverting to v3 Naming (Legacy Mode)

If you need to maintain v3-style naming for backward compatibility, set `NamingConvention = Legacy` on the `[TelemetryGeneration]` attribute:

```csharp
using Purview.Telemetry;

// Revert ALL telemetry to v3 naming (assembly-level)
[assembly: TelemetryGeneration(NamingConvention = NamingConvention.Legacy)]
```

Or set per-interface:

```csharp
// Legacy naming for this interface only
[TelemetryGeneration(NamingConvention = NamingConvention.Legacy)]
interface IMyTelemetry { }
```

#### Available Naming Conventions

```csharp
public enum NamingConvention
{
    Legacy = 0,          // v3 behaviour: lowercase, smashed compounds
    OpenTelemetry = 1    // v4 default: OTel conventions (dot.separated, snake_case)
}
```

**Recommendation:** Use `NamingConvention.OpenTelemetry` (default) for new projects. Only use `Legacy` if you need exact v3 compatibility.
