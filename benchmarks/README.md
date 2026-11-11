# Purview Telemetry Benchmarks

BenchmarkDotNet-based performance benchmarks comparing interface-based (source-generator-generated) telemetry vs. hand-written equivalents.

## Overview

These benchmarks measure the performance and allocation characteristics of the Purview Telemetry Source Generator across seven key dimensions:

| Benchmark Class | What it measures |
|---|---|
| `ActivityBenchmarks` | Generated vs. manual Activity telemetry, with/without an `ActivityListener` |
| `GeneratorModelBenchmarks` | The generator's own `closed record` models vs. a value-based `[Union]` struct — contributor-facing, not a consumer hot path |
| `LoggerBenchmarks` | Generated logging (v1/v2) vs. manual ILogger and LoggerMessage.Define, with/without logging enabled |
| `LoggerMultiTargetBenchmarks` | Single-target (log only) vs. multi-target (Activity + Logging + Metrics) in v1/v2 modes |
| `MultiTargetVsSingleTargetBenchmarks` | Multi-target (Activity + Log + Metric in one call) vs. single-target (Activity only) |
| `TagListBenchmarks` | Metrics recording: few tags (inline `KeyValuePair` params) vs. many tags (`TagList` struct) |
| `MetricsBenchmarks` | Generated vs. hand-written `Counter`/`UpDownCounter`/`Histogram`, with 0 and 1 tag |

Observable instruments (`ObservableCounter`, `ObservableGauge`, `ObservableUpDownCounter`) are not benchmarked at all:
they are registered once with a callback and observed by the metrics pipeline, so there is no per-operation hot path to
compare.

## Runtimes and jobs

Every benchmark class declares the same three jobs, each naming its runtime explicitly:

| Job | Runtime |
| --- | --- |
| `[SimpleJob(RuntimeMoniker.Net10_0)]` | .NET 10 |
| `[SimpleJob(RuntimeMoniker.Net11_0)]` | .NET 11 |
| `[SimpleJob(RuntimeMoniker.Net48)]` | .NET Framework 4.8 — the `netstandard2.0` asset path, which is the only way to compare it against the modern runtimes. Windows only. |

One invocation therefore measures all three runtimes, and one report carries all three sections. The framework
the suite is launched with only selects the BenchmarkDotNet host.

`RuntimeMoniker.Net11_0` is why this project pins **BenchmarkDotNet `0.16.0-preview.2`**, a prerelease: the newest
stable release, 0.15.8, has `RuntimeMoniker` members only up to `Net10_0`. `Net12_0` is already present in
0.16.0-preview.2 as well, so the next framework needs no further dependency change. `RuntimeMoniker` is declared
in `BenchmarkDotNet.Annotations.dll`, so that is the assembly to inspect when checking which monikers a version has.

`RuntimeMoniker.Net47` is deliberately absent: its generated host project fails to restore on every run, so it only
ever added `NA` rows and "Benchmarks with issues" noise to each report. .NET Framework 4.8 is the supported
.NET Framework target anyway.

## Running the Benchmarks

### Prerequisites

- The .NET 11 SDK pinned in `global.json`, plus the .NET 10 and .NET 11 runtimes (each job measures its own
  runtime, so both must be installed)
- Windows, for the .NET Framework 4.8 job
- Release build (BenchmarkDotNet requires `Release`; a Debug build's numbers are not meaningful)

### Run the full suite

```bash
dotnet run --project benchmarks/Purview.Telemetry.Benchmarks --configuration Release --framework net11.0
```

`just benchmark` runs the same thing. One invocation measures .NET 10, .NET 11 and .NET Framework 4.8, and writes one
report per benchmark class to `BenchmarkDotNet.Artifacts/results/`.

### Run a specific benchmark class

```bash
dotnet run --project benchmarks/Purview.Telemetry.Benchmarks --configuration Release --framework net11.0 -- --filter "*ActivityBenchmarks*"
dotnet run --project benchmarks/Purview.Telemetry.Benchmarks --configuration Release --framework net11.0 -- --filter "*GeneratorModelBenchmarks*"
dotnet run --project benchmarks/Purview.Telemetry.Benchmarks --configuration Release --framework net11.0 -- --filter "*LoggerBenchmarks*"
dotnet run --project benchmarks/Purview.Telemetry.Benchmarks --configuration Release --framework net11.0 -- --filter "*LoggerMultiTarget*"
dotnet run --project benchmarks/Purview.Telemetry.Benchmarks --configuration Release --framework net11.0 -- --filter "*MultiTarget*"
dotnet run --project benchmarks/Purview.Telemetry.Benchmarks --configuration Release --framework net11.0 -- --filter "*TagList*"
dotnet run --project benchmarks/Purview.Telemetry.Benchmarks --configuration Release --framework net11.0 -- --filter "*MetricsBenchmarks*"
```

### Interactive selection

```bash
dotnet run --project benchmarks/Purview.Telemetry.Benchmarks --configuration Release --framework net11.0 -- --list flat
```

## Benchmark Details

### `ActivityBenchmarks`

Compares generated vs. manually-written activity telemetry with a parameterised `HasListener` flag:

- `HasListener = true` — an `ActivityListener` is registered, so activities are actually created and sampled.
- `HasListener = false` — no listener; the `HasListeners()` guard short-circuits the method immediately.

This tests whether the generated code has any overhead compared to equivalent hand-written code, under both the hot path (listener present) and the cold-but-common path (no listener).

### `GeneratorModelBenchmarks`

Compares the two modelling strategies the generator's pipeline can use for its tagged hierarchies
(`closed record InstrumentAttributeRecord` / `ActivityMethodTarget`): an abstract record with one sealed case per
instrument and virtual properties, versus a `readonly struct` union carrying a tag and the case payloads with a
non-boxing `TryGetValue` accessor. Both build the same number of models and read the same four facts off each one,
which is what a generation pass does per method; `MethodCount` is 8 and 64.

The generator's models are internal to the analyzer assembly, so the two shapes are replicated in the benchmark
project rather than referenced — keep the replica faithful to `src/src/SourceGenerator/Records/` when either shape
changes.

This family is **contributor-facing**: it answers whether the record hierarchy's per-method allocation and virtual
dispatch can move generator throughput, and it backs the recorded decision in
[Platform Support](../docs/wiki/Platform-Support.md#considered-and-rejected) to keep the `closed` records. It is not
a consumer hot path and is not part of the README's consumer performance tables.

### `LoggerBenchmarks`

Compares three logging implementations with a parameterised `HasLogging` flag:

| Implementation | Code path |
|---|---|
| Manual: `LoggerMessage.Define` | Hand-written `static readonly LoggerMessage.Define<T>()` delegates (classic optimised approach) |
| Generated v1 | Source-generator with `GenerationMode = LoggerGenerationMode.V1` — emits `LoggerMessage.Define<T>` (identical pattern to manual above) |
| Generated v2 | Source-generator default — emits state-based `LoggerMessageHelper.ThreadLocalState` approach, matching the built-in `[LoggerMessage]` attribute generator |

- `HasLogging = true` — an always-enabled no-op logger is used; the full code path (including `IsEnabled` check and message formatting) is exercised.
- `HasLogging = false` — `NullLogger` is used; the `IsEnabled` guard short-circuits immediately.

All three are held as an interface in the benchmark — `ManualLoggerMessageTelemetry` implements `ILoggerOnlyTelemetry`
for that reason — so one side is not inlined into the measurement loop while the other is. Consuming the generated
class through its interface is also what a DI-resolved consumer gets.

### `LoggerMultiTargetBenchmarks`

Compares the overhead of including logging in multi-target telemetry with a parameterised `HasListener` flag (logging is always enabled):

- **Single-target v2**: Generated logger-only (state-based).
- **Single-target v1**: Generated logger-only (LoggerMessage.Define).
- **Multi-target v2**: Generated Activity + Logging (state-based) + Metrics from a single interface call.
- **Multi-target v1**: Generated Activity + Logging (LoggerMessage.Define) + Metrics from a single interface call.
- **Multi-target manual**: Equivalent hand-written code combining Activity, ILogger, and Metrics.

### `MultiTargetVsSingleTargetBenchmarks`

Compares:

- **Single-target generated**: one method call → Activity only.
- **Multi-target generated**: one method call → Activity + ILogger + Metrics counter all at once.
- **Multi-target manual**: equivalent hand-written code combining the three telemetry types.

The multi-target pattern is the key value proposition of the source generator, and this benchmark quantifies its cost relative to the single-target case.

### `TagListBenchmarks`

Demonstrates the two code paths the generator produces for metric tag recording:

| Tag count | Generated code path |
|---|---|
| 0 – 3 tags | Tags passed as inline `KeyValuePair<string, object?>` parameters directly to `Add`/`Record`. |
| 4+ tags | A `System.Diagnostics.TagList` (stack-allocated struct) is populated and passed to `Add`/`Record`. |

The threshold is 4 tags. This benchmark shows the performance difference between the two approaches.

### `MetricsBenchmarks`

Compares the generated synchronous instruments against hand-written best-practice code, one instrument call per
benchmark with the manual implementation as the baseline:

| Pair | Generated call | Manual call |
|---|---|---|
| Auto-counter, 0 tags | `IncrementRequestCount()` | `Counter<int>.Add(1)` |
| Auto-counter, 1 tag | `CountRequestByType("read")` | `Counter<int>.Add(1, KeyValuePair("request_type", "read"))` |
| Up-down counter | `AdjustActiveConnections(1)` | `UpDownCounter<int>.Add(1)` |
| Histogram, 0 tags | `RecordDuration(42.0)` | `Histogram<double>.Record(42.0)` |
| Histogram, 1 tag | `RecordDurationByEndpoint(42.0, "/api/data")` | `Histogram<double>.Record(42.0, KeyValuePair("endpoint", "/api/data"))` |

Both implementations are held as `IMetricsBenchmarkTelemetry` — `ManualMetricsTelemetry` implements the interface for
that reason — so neither side is inlined into the measurement loop and a difference is generated code rather than call
shape.

Observable instruments are absent for the reason given above: a callback registered once has no per-operation hot path.

## Project Structure

```
benchmarks/
└── Purview.Telemetry.Benchmarks/
    ├── Benchmarks/
    │   ├── ActivityBenchmarks.cs                    # Activity: generated vs. manual, listener vs. no-listener
    │   ├── GeneratorModelBenchmarks.cs              # Generator models: closed records vs. value-based union
    │   ├── LoggerBenchmarks.cs                      # Logging: v1/v2 generated vs. manual, logging enabled/disabled
    │   ├── LoggerMultiTargetBenchmarks.cs           # Logger single-target vs. multi-target (v1/v2/manual)
    │   ├── MetricsBenchmarks.cs                     # Counter/UpDownCounter/Histogram: generated vs. manual
    │   ├── MultiTargetVsSingleTargetBenchmarks.cs   # Multi-target vs. single-target
    │   └── TagListBenchmarks.cs                     # TagList vs. inline KVP metrics
    ├── Manual/
    │   ├── ManualActivityTelemetry.cs               # Hand-written Activity telemetry
    │   ├── ManualLoggerMessageTelemetry.cs          # Hand-written LoggerMessage.Define<T> logging
    │   ├── ManualLoggerTelemetry.cs                 # Hand-written direct ILogger.Log logging
    │   ├── ManualMetricsTelemetry.cs                # Hand-written Counter/UpDownCounter/Histogram
    │   └── ManualMultiTargetTelemetry.cs            # Hand-written Activity + Log + Metric
    ├── Telemetry/
    │   ├── IActivityOnlyTelemetry.cs                # [ActivitySource] interface
    │   ├── ILoggerOnlyTelemetry.cs                  # [Logger] interface (v2 state-based)
    │   ├── ILoggerV1OnlyTelemetry.cs                # [Logger(GenerationMode = V1)] (v1 LoggerMessage.Define)
    │   ├── IMetricsBenchmarkTelemetry.cs            # [Meter] interface, generated-vs-manual instrument pairs
    │   ├── IMetricsFewTagsTelemetry.cs              # [Meter] interface, 0–3 tags per method
    │   ├── IMetricsManyTagsTelemetry.cs             # [Meter] interface, 4–6 tags per method
    │   ├── IMultiTargetTelemetry.cs                 # [ActivitySource][Logger v2][Meter] interface
    │   └── IMultiTargetV1Telemetry.cs               # [ActivitySource][Logger v1][Meter] interface
    ├── BenchmarkHelpers.cs                          # Factory helpers for setup
    ├── Net48Polyfills.cs                            # IsExternalInit shim for the .NET Framework 4.8 leg
    └── Program.cs                                   # Entry point
```

## Logging Code-Path Details

The source generator produces two distinct logging implementations depending on whether
`Microsoft.Extensions.Telemetry.Abstractions` is referenced (which exposes `LogPropertiesAttribute`):

| Mode | Condition | Generated pattern |
|---|---|---|
| **v2 (state-based)** | `LogPropertiesAttribute` available AND `GenerationMode != V1` | `LoggerMessageHelper.ThreadLocalState` filled with key-value pairs, passed to `_logger.Log()` |
| **v1 (LoggerMessage.Define)** | `LogPropertiesAttribute` absent OR `GenerationMode = V1` | `static readonly Action<ILogger, T...> _action = LoggerMessage.Define<T...>(...)` called per-invocation |

