# Purview Telemetry Benchmarks

BenchmarkDotNet-based performance benchmarks comparing interface-based (source-generator-generated) telemetry vs. hand-written equivalents.

## Overview

These benchmarks measure the performance and allocation characteristics of the Purview Telemetry Source Generator across five key dimensions:

| Benchmark Class | What it measures |
|---|---|
| `ActivityBenchmarks` | Generated vs. manual Activity telemetry, with/without an `ActivityListener` |
| `LoggerBenchmarks` | Generated logging (v1/v2) vs. manual ILogger and LoggerMessage.Define, with/without logging enabled |
| `LoggerMultiTargetBenchmarks` | Single-target (log only) vs. multi-target (Activity + Logging + Metrics) in v1/v2 modes |
| `MultiTargetVsSingleTargetBenchmarks` | Multi-target (Activity + Log + Metric in one call) vs. single-target (Activity only) |
| `TagListBenchmarks` | Metrics recording: few tags (inline `KeyValuePair` params) vs. many tags (`TagList` struct) |

## Running the Benchmarks

### Prerequisites

- .NET 10 SDK
- Release build (BenchmarkDotNet requires `Release` configuration)

### Run all benchmarks

```bash
dotnet run --project benchmarks/Purview.Telemetry.Benchmarks --configuration Release
```

### Run a specific benchmark class

```bash
dotnet run --project benchmarks/Purview.Telemetry.Benchmarks --configuration Release -- --filter "*ActivityBenchmarks*"
dotnet run --project benchmarks/Purview.Telemetry.Benchmarks --configuration Release -- --filter "*LoggerBenchmarks*"
dotnet run --project benchmarks/Purview.Telemetry.Benchmarks --configuration Release -- --filter "*LoggerMultiTarget*"
dotnet run --project benchmarks/Purview.Telemetry.Benchmarks --configuration Release -- --filter "*MultiTarget*"
dotnet run --project benchmarks/Purview.Telemetry.Benchmarks --configuration Release -- --filter "*TagList*"
```

### Interactive selection

```bash
dotnet run --project benchmarks/Purview.Telemetry.Benchmarks --configuration Release -- --list flat
```

## Benchmark Details

### `ActivityBenchmarks`

Compares generated vs. manually-written activity telemetry with a parameterised `HasListener` flag:

- `HasListener = true` — an `ActivityListener` is registered, so activities are actually created and sampled.
- `HasListener = false` — no listener; the `HasListeners()` guard short-circuits the method immediately.

This tests whether the generated code has any overhead compared to equivalent hand-written code, under both the hot path (listener present) and the cold-but-common path (no listener).

### `LoggerBenchmarks`

Compares three logging implementations with a parameterised `HasLogging` flag:

| Implementation | Code path |
|---|---|
| Manual: `LoggerMessage.Define` | Hand-written `static readonly LoggerMessage.Define<T>()` delegates (classic optimised approach) |
| Generated v1 | Source-generator with `DisableMSLoggingTelemetryGeneration = true` — emits `LoggerMessage.Define<T>` (identical pattern to manual above) |
| Generated v2 | Source-generator default — emits state-based `LoggerMessageHelper.ThreadLocalState` approach, matching the built-in `[LoggerMessage]` attribute generator |

- `HasLogging = true` — an always-enabled no-op logger is used; the full code path (including `IsEnabled` check and message formatting) is exercised.
- `HasLogging = false` — `NullLogger` is used; the `IsEnabled` guard short-circuits immediately.

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

## Project Structure

```
benchmarks/
└── Purview.Telemetry.Benchmarks/
    ├── Benchmarks/
    │   ├── ActivityBenchmarks.cs                    # Activity: generated vs. manual, listener vs. no-listener
    │   ├── LoggerBenchmarks.cs                      # Logging: v1/v2 generated vs. manual, logging enabled/disabled
    │   ├── LoggerMultiTargetBenchmarks.cs           # Logger single-target vs. multi-target (v1/v2/manual)
    │   ├── MultiTargetVsSingleTargetBenchmarks.cs   # Multi-target vs. single-target
    │   └── TagListBenchmarks.cs                     # TagList vs. inline KVP metrics
    ├── Manual/
    │   ├── ManualActivityTelemetry.cs               # Hand-written Activity telemetry
    │   ├── ManualLoggerMessageTelemetry.cs          # Hand-written LoggerMessage.Define<T> logging
    │   ├── ManualLoggerTelemetry.cs                 # Hand-written direct ILogger.Log logging
    │   └── ManualMultiTargetTelemetry.cs            # Hand-written Activity + Log + Metric
    ├── Telemetry/
    │   ├── IActivityOnlyTelemetry.cs                # [ActivitySource] interface
    │   ├── ILoggerOnlyTelemetry.cs                  # [Logger] interface (v2 state-based)
    │   ├── ILoggerV1OnlyTelemetry.cs                # [Logger(DisableMSLoggingTelemetryGeneration=true)] (v1 LoggerMessage.Define)
    │   ├── IMetricsFewTagsTelemetry.cs              # [Meter] interface, 0–3 tags per method
    │   ├── IMetricsManyTagsTelemetry.cs             # [Meter] interface, 4–6 tags per method
    │   ├── IMultiTargetTelemetry.cs                 # [ActivitySource][Logger v2][Meter] interface
    │   └── IMultiTargetV1Telemetry.cs               # [ActivitySource][Logger v1][Meter] interface
    ├── BenchmarkHelpers.cs                          # Factory helpers for setup
    └── Program.cs                                   # Entry point
```

## Logging Code-Path Details

The source generator produces two distinct logging implementations depending on whether
`Microsoft.Extensions.Telemetry.Abstractions` is referenced (which exposes `LogPropertiesAttribute`):

| Mode | Condition | Generated pattern |
|---|---|---|
| **v2 (state-based)** | `LogPropertiesAttribute` available AND `DisableMSLoggingTelemetryGeneration = false` | `LoggerMessageHelper.ThreadLocalState` filled with key-value pairs, passed to `_logger.Log()` |
| **v1 (LoggerMessage.Define)** | `LogPropertiesAttribute` absent OR `DisableMSLoggingTelemetryGeneration = true` | `static readonly Action<ILogger, T...> _action = LoggerMessage.Define<T...>(...)` called per-invocation |

