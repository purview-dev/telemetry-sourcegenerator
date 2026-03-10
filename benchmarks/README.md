# Purview Telemetry Benchmarks

BenchmarkDotNet-based performance benchmarks comparing interface-based (source-generator-generated) telemetry vs. hand-written equivalents.

## Overview

These benchmarks measure the performance and allocation characteristics of the Purview Telemetry Source Generator across three key dimensions:

| Benchmark Class | What it measures |
|---|---|
| `ActivityBenchmarks` | Generated vs. manual Activity telemetry, with/without an `ActivityListener` |
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
    │   ├── MultiTargetVsSingleTargetBenchmarks.cs   # Multi-target vs. single-target
    │   └── TagListBenchmarks.cs                     # TagList vs. inline KVP metrics
    ├── Manual/
    │   ├── ManualActivityTelemetry.cs               # Hand-written Activity telemetry
    │   └── ManualMultiTargetTelemetry.cs            # Hand-written Activity + Log + Metric
    ├── Telemetry/
    │   ├── IActivityOnlyTelemetry.cs                # [ActivitySource] interface
    │   ├── IMultiTargetTelemetry.cs                 # [ActivitySource][Logger][Meter] interface
    │   ├── IMetricsFewTagsTelemetry.cs              # [Meter] interface, 0–3 tags per method
    │   └── IMetricsManyTagsTelemetry.cs             # [Meter] interface, 4–6 tags per method
    ├── BenchmarkHelpers.cs                          # Factory helpers for setup
    └── Program.cs                                   # Entry point
```
