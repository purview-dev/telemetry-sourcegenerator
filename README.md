# Purview Telemetry Source Generator

[![NuGet version](https://img.shields.io/nuget/v/Purview.Telemetry.SourceGenerator.svg)](https://www.nuget.org/packages/Purview.Telemetry.SourceGenerator)
[![Release](https://github.com/purview-dev/telemetry-sourcegenerator/actions/workflows/release.yml/badge.svg)](https://github.com/purview-dev/telemetry-sourcegenerator/actions/workflows/release.yml)

Generates [`ActivitySource`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysource), [`ILogger`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger), and [`Metrics`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics) based telemetry from methods you define on an interface.

## Features

- **Zero boilerplate** - define methods on an interface, get full telemetry implementation generated
- **Multi-target generation** - generate Activities, Logging, and Metrics from a single interface
- **Testable** - easy mocking/substitution for unit testing
- **DI-ready** - automatic dependency injection registration helpers

## Supported Frameworks

**Consumer runtime targets:**

- .NET Framework 4.8
- .NET 8 or higher

**Build toolchain requirement:**

- Visual Studio 2026 (18.x) or .NET 10 SDK (Roslyn 5.9.0+)

## Installation

Add to your `Directory.Build.props` or `.csproj` file:

```xml
<PackageReference Include="Purview.Telemetry.SourceGenerator" Version="5.0.0-prerelease.11">
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
        }

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
| ----------- | ---------------- | ------------- |
| `[ActivitySource]` | Class-level | Marks interface for Activity generation |
| `[Activity]` | Method | Creates and starts a new Activity |
| `[Event]` | Method | Adds an ActivityEvent to an Activity |
| `[Context]` | Method | Adds Baggage to an Activity |
| `[Logger]` | Class-level | Marks interface for ILogger generation |
| `[Log]` | Method | Generates structured log message |
| `[Trace]`, `[Debug]`, `[Info]`, `[Warning]`, `[Error]`, `[Critical]` | Method | Log with specific level |
| `[Meter]` | Class-level | Marks interface for Metrics generation |
| `[Counter]`, `[AutoCounter]` | Method | Counter instrument |
| `[UpDownCounter]` | Method | Up-down counter instrument |
| `[Histogram]` | Method | Histogram instrument |
| `[ObservableCounter]`, `[ObservableGauge]`, `[ObservableUpDownCounter]` | Method | Observable instruments |

> [!TIP]
> For single-target interfaces (only Activities, only Logging, or only Metrics), the generator automatically infers the necessary attributes. See the [wiki](https://github.com/purview-dev/telemetry-sourcegenerator/wiki/Multi-Targeting) for details.

## Documentation

- [Homepage](https://purview.dev/projects/telemetry-sourcegenerator/)
- [Documentation](https://purview.dev/docs/telemetry-sourcegenerator/)
- [Full Wiki](https://github.com/purview-dev/telemetry-sourcegenerator/wiki)
- [Generated Output Examples](https://github.com/purview-dev/telemetry-sourcegenerator/wiki/Generated-Output)
- [Multi-Targeting Guide](https://github.com/purview-dev/telemetry-sourcegenerator/wiki/Multi-Targeting)
- [Logging Configuration](https://github.com/purview-dev/telemetry-sourcegenerator/wiki/Logging)

## Agent Skills

This repository ships with [Agent Skills](https://agentskills.io/specification) under `.agents/skills/` covering the source generator, its test framework, and the `Purview.BuildSdk` build SDK. See [`AGENTS.md`](AGENTS.md) — "Source-generator and testing skills" for the full list and when to load each one.

## Sample Project

The [.NET Aspire Sample](https://github.com/purview-dev/telemetry-sourcegenerator/tree/main/samples/SampleApp) demonstrates Activities, Logs, and Metrics generation working together with the Aspire Dashboard.

> [!TIP]
> The sample project has [`EmitCompilerGeneratedFiles`](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-generator#enable-the-configuration-source-generator) enabled so you can inspect the generated output.

## Performance

Benchmarked on 13th Gen Intel Core i9-13900KF, .NET SDK 10.0.401. See the [Performance](https://github.com/purview-dev/telemetry-sourcegenerator/wiki/Performance) wiki page for full cross-runtime results.

### Activities (.NET 10.0)

| Scenario | HasListener | Manual | Generated | Ratio |
| --- | --- | --- | --- | --- |
| start + complete | False | 0.53 ns | 0.58 ns | 1.11x |
| start + complete | True | 220 ns / 1008 B | 217 ns / 1008 B | 0.98x |
| start + fail | True | 208 ns / 920 B | 203 ns / 920 B | 0.92x |

Generated activities match hand-written code with identical allocations.

### Logging (.NET 10.0)

| Scenario | HasLogging | LoggerMessage.Define | Generated v1 | Generated v2 |
| --- | --- | --- | --- | --- |
| single Info call | True | 6.12 ns | 4.43 ns (0.72x) | 3.80 ns (0.62x) |
| full lifecycle (4 calls) | True | 18.88 ns | 19.58 ns (1.04x) | 15.01 ns (0.80x) |

Both v1 and v2 allocate **0 bytes** per call on all runtimes. Generated methods are emitted with `[MethodImpl(AggressiveInlining)]`; generated v1 and v2 both beat the manual `LoggerMessage.Define` baseline for single calls on .NET 10.0.

### Multi-Target (.NET 10.0, Activity + Logging + Metrics)

| Scenario | HasListener | Single-target | Multi-target generated | Multi-target manual |
|---|---|---|---|---|
| start + complete | True | 216 ns / 1008 B | 230 ns / 1032 B (1.07x) | 260 ns / 1032 B (1.20x) |

Adding full Activity+Logging+Metrics multi-target generation costs ~7% over Activity-only on .NET 10.0, and the generated multi-target code is faster than the hand-written equivalent.

### Metrics (.NET 10.0)

| Scenario | Generated | Notes |
| --- | --- | --- |
| auto-counter (0 tags) | 0.40 ns | - |
| auto-counter (1 tag) | 0.35 ns | - |
| up-down counter | 0.37 ns | - |
| histogram (0 tags) | 0.42 ns | - |
| histogram (1 tag) | 0.34 ns | - |
| 4+ tags (TagList) | 5-8 ns | Stack-allocated `TagList` |

All instruments are **0 allocations** on all runtimes.

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
| ---------------- | ------------- | ------------ | -------- |
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

## Contributing

Contributions are welcome! See the [Contributing guide](https://github.com/purview-dev/telemetry-sourcegenerator/wiki/Contributing) for development setup and testing instructions.

See [docs/release-process.md](docs/release-process.md) for the release flow, and [`AGENTS.md`](AGENTS.md)
for the build, validation, and convention rules. Bump the version in `package.json` and run
`just update-version` to sync it into docs/samples before packaging.
