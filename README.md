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
- .NET 8 or higher, including .NET 11

**Hosting models:** console, ASP.NET Core, .NET Aspire, Native AOT, Blazor WebAssembly (trimmed and AOT-compiled)
and Blazor Server. The generated code uses no reflection and adds no trim or AOT warnings, and no compiler-host
assembly reaches your application's output. See
[Platform Support](https://purview.dev/docs/telemetry-sourcegenerator/platform-support/) for the full matrix.

**Build toolchain requirement:**

- Visual Studio 2026 (18.x) or .NET 10 SDK or later (Roslyn 5.9.0+)

## What's new for .NET 11

- **`ActivitySourceFactory`** - opt in with
  `[assembly: ActivitySourceGeneration("MyApp", UseActivitySourceFactory = true)]` and register the factory with
  `services.AddTracing()`. The generated classes then create their sources from the injected .NET 11 factory instead
  of a static `ActivitySource`. When the type isn't available, `TSG3023` reports it and the static source is kept.
- **C# 15 static interface members** are ignored rather than implemented.
- **Validated on .NET 11**: the tests run on `net11.0`, and Native AOT, Blazor WebAssembly and Blazor Server
  consumers of the package are built on every release.

## Installation

```bash
dotnet add package Purview.Telemetry.SourceGenerator
```

`dotnet add package` writes the latest version (shown on the NuGet badge above) into your project. The
generator is a compile-time dependency only, so mark the reference as analyzer-only:

```xml
<PackageReference Include="Purview.Telemetry.SourceGenerator">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>analyzers</IncludeAssets>
</PackageReference>
```

The `Version` attribute that `dotnet add package` adds stays on that element; with
[Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
it lives in `Directory.Packages.props` instead.

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
public sealed record Entity(int Id, float TotalValue, int LastUpdatedByUserId, int SizeInBytes);

public interface IEntityRepository
{
    Task<Entity?> GetAsync(int id, CancellationToken cancellationToken);
}

public class EntityService(IEntityStoreTelemetry telemetry, IEntityRepository repository)
{
    public async Task<Entity?> GetEntityAsync(int id, string serviceUrl, CancellationToken cancellationToken)
    {
        // Single call creates Activity AND logs AND increments counter
        using var activity = telemetry.GettingEntityFromStore(id, serviceUrl);

        var stopwatch = Stopwatch.StartNew();
        var entity = await repository.GetAsync(id, cancellationToken);

        // Adds event to activity AND logs duration
        telemetry.GetDuration(activity, (int)stopwatch.ElapsedMilliseconds);

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
> For single-target interfaces (only Activities, only Logging, or only Metrics), the generator automatically infers the necessary attributes. See the [Multi-Targeting guide](https://purview.dev/docs/telemetry-sourcegenerator/multi-targeting/) for details.

## Documentation

- [Homepage](https://purview.dev/projects/telemetry-sourcegenerator/)
- [Documentation](https://purview.dev/docs/telemetry-sourcegenerator/)
- [Generated Output Examples](https://purview.dev/docs/telemetry-sourcegenerator/generated-output/)
- [Multi-Targeting Guide](https://purview.dev/docs/telemetry-sourcegenerator/multi-targeting/)
- [Logging Configuration](https://purview.dev/docs/telemetry-sourcegenerator/logging/)
- [Platform Support and .NET 11](https://purview.dev/docs/telemetry-sourcegenerator/platform-support/)

## Agent Skills

This repository publishes [Agent Skills](https://agentskills.io/specification) for using the generator - quick
start, interface design, migration, multi-targeting and diagnostics. Install them with:

```bash
npx skills add purview-dev/telemetry-sourcegenerator
```

See [`AGENTS.md`](https://github.com/purview-dev/telemetry-sourcegenerator/blob/main/AGENTS.md) for the list.

## Sample Project

The [.NET Aspire Sample](https://github.com/purview-dev/telemetry-sourcegenerator/tree/main/samples/SampleApp) demonstrates Activities, Logs, and Metrics generation working together with the Aspire Dashboard.

> [!TIP]
> The sample project has [`EmitCompilerGeneratedFiles`](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-generator#enable-the-configuration-source-generator) enabled so you can inspect the generated output.

## Performance

Benchmarked on 13th Gen Intel Core i9-13900KF, .NET SDK 10.0.401. See the [Performance](https://purview.dev/docs/telemetry-sourcegenerator/performance/) page for full cross-runtime results.

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
| **Metric Instrument Names** | Lowercased, smashed: `"product.recordcount"` | Dot-separated namespaces, snake_case words: `"product.record_count"` | Word boundaries become underscores |
| **Metric Tag Keys** | Lowercased, smashed: `"requestcount"` | snake_case: `"request_count"` | Metric tag keys have underscores |

#### Examples

Given this interface:

```csharp
[Meter("MyApp.Products")]
interface IProductTelemetry
{
    [Counter]
    void RecordCount([InstrumentMeasurement] int count);
}
```

**Before (v3/Legacy):**

```csharp
// Generated code:
new ActivitySource("myapp")           // lowercase
activity.SetTag("entityid", ...)      // smashed compound
var meter = meterFactory.Create(new MeterOptions("MyApp.Products") { ... });
meter.CreateCounter<int>("product.recordcount")  // smashed compound
```

**After (v4 OpenTelemetry naming - DEFAULT):**

```csharp
// Generated code:
new ActivitySource("MyApp")           // preserves casing
activity.SetTag("entity_id", ...)     // snake_case
var meter = meterFactory.Create(new MeterOptions("MyApp.Products") { ... });
meter.CreateCounter<int>("product.record_count")  // interface prefix + snake_case instrument
```

An instrument name is built from, in order: the assembly `[MeterGeneration(InstrumentPrefix = ...)]`, the
interface prefix, and the instrument name. The interface prefix is `[Meter(InstrumentPrefix = ...)]` when set,
otherwise it is derived from the interface name: the leading `I` and a trailing `Telemetry` or `Logs` are
removed, so `IProductTelemetry` becomes `product`. The instrument name is the attribute's `Name`, or the method
name in snake_case.

**The meter name is not part of the instrument name by default.** The meter already identifies the instruments
it owns, and `MeterNameGenerationType` defaults to `DotNet`. Opt in to the meter name as the outermost namespace
with `MeterNameGenerationType.OpenTelemetry`, which also lowercases a meter name derived from the assembly name:

```csharp
[assembly: MeterGeneration(MeterNameGenerationType = MeterNameGenerationType.OpenTelemetry)]

// The interface above now generates "my_app.products.product.record_count".
```

See [Metrics](https://purview.dev/docs/telemetry-sourcegenerator/metrics/#meter-naming) for the full rules.

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

Contributions are welcome! See the [Contributing guide](https://purview.dev/docs/telemetry-sourcegenerator/contributing/) for development setup and testing instructions.

See [docs/release-process.md](docs/release-process.md) for the release flow, and [`AGENTS.md`](AGENTS.md)
for the build, validation, and convention rules. Bump the version in `package.json` and run
`just update-version` to sync it into docs/samples before packaging.
