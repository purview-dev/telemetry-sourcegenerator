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
- .NET 10 or higher, including .NET 11 (.NET 8 and .NET 9 are no longer supported)

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
- **C# 15 union types** are unwrapped to their case value for tags, baggage, log values and metric tags.
  A union that declares the non-boxing `TryGetValue`/`HasValue` pattern is read without boxing the union
  struct. **Purview.Unions `[GenerateUnion]` types** work too: the case list is read from the surface that is
  in source — the attribute's `typeof` list, partial static factories, a nested `IUnionMembers` provider, then
  the constructors — so no reference to that package is taken and no other generator's output is needed. See
  [Union types](https://purview.dev/docs/telemetry-sourcegenerator/tags-and-baggage/#union-types).
- **Validated on .NET 11**: the tests run on `net11.0`, and Native AOT, Blazor WebAssembly and Blazor Server
  consumers of the package are built on every release.
- **.NET 8 and .NET 9 are no longer supported**: target `net10.0`+ or `net48`+. `TSG1011` reports a
  compilation that targets either dropped framework.

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

Benchmarked on 13th Gen Intel Core i9-13900KF, .NET SDK 11.0.100. See the [Performance](https://purview.dev/docs/telemetry-sourcegenerator/performance/) page for the full cross-runtime results (.NET 10, .NET 11 and .NET Framework 4.8). Ratios below compare generated against the hand-written implementation of the same scenario.

### Activities (.NET 10.0)

| Scenario | HasListener | Manual | Generated | Ratio |
| --- | --- | --- | --- | --- |
| start + complete | False | 1.50 ns | 1.51 ns | 1.01x |
| start + complete | True | 218 ns / 1008 B | 213 ns / 1008 B | 0.98x |
| start + fail | True | 192 ns / 920 B | 191 ns / 920 B | 0.99x |

Generated activities match hand-written code with identical allocations. With a listener active, ~99% of the measured time is the framework creating the `Activity`, not generated code.

### Logging (.NET 10.0)

| Scenario | HasLogging | LoggerMessage.Define | Generated v1 | Generated v2 |
| --- | --- | --- | --- | --- |
| single Info call | True | 5.36 ns | 5.36 ns (1.00x) | 4.39 ns (0.82x) |
| full lifecycle (4 calls) | True | 20.21 ns | 19.60 ns (0.97x) | 17.96 ns (0.89x) |

Both v1 and v2 allocate **0 bytes** per call on all runtimes. Generated methods are emitted with `[MethodImpl(AggressiveInlining)]`; v2 (state-based) is the fastest implementation on .NET 10.0, .NET 11.0 and .NET Framework 4.8, and v1 — which reproduces the hand-written `LoggerMessage.Define` delegate — is at parity with the hand-written baseline (0.97-1.00x here, 0.99-1.03x across all runtimes). The one place v2 is measurably more expensive is the disabled level on .NET Framework 4.8 (8.19 ns against 2.82 ns), before any log state is built.

### Multi-Target (.NET 10.0, Activity + Logging + Metrics)

| Scenario | HasListener | Single-target | Multi-target generated | Multi-target manual |
|---|---|---|---|---|
| start + complete | True | 204 ns / 1008 B | 213 ns / 1032 B (1.04x) | 237 ns / 1032 B (1.16x) |

Adding full Activity+Logging+Metrics multi-target generation costs ~4% over Activity-only on .NET 10.0, and the generated multi-target code is faster than the hand-written equivalent. The 24 B difference is the log and metric state, which the hand-written version allocates identically.

### Metrics (.NET 10.0)

| Scenario | Generated | Notes |
| --- | --- | --- |
| auto-counter (0 tags) | 1.31 ns | - |
| auto-counter (1 tag) | 1.30 ns | - |
| up-down counter | 1.30 ns | - |
| histogram (0 tags) | 1.29 ns | - |
| histogram (1 tag) | 1.29 ns | - |
| 4+ tags (TagList) | 4.9-7.1 ns | Stack-allocated `TagList` |

All instruments are **0 allocations** on all runtimes, generated and hand-written alike. On .NET 10.0 the generated rows sit within 2% of the hand-written equivalents (1.29-1.31 ns against 1.28-1.34 ns), with both consumed through the same interface, so the ~1.3 ns is the instrument update itself rather than generator overhead.

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
