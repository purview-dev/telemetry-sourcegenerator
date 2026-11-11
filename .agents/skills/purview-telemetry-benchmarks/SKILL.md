---
name: purview-telemetry-benchmarks
description: >-
  Maintainer workflow for this repository's BenchmarkDotNet suite - running the .NET 10, .NET 11 and
  .NET Framework 4.8 jobs, reading a -report-github.md, refreshing docs/wiki/Performance.md and
  README.md from a fresh run, writing the analysis consumers read, and adding a benchmark family.
  Use when asked to run, update, interpret, or extend the benchmark results. This is a developer
  skill for this repository; it is never published to consumers.
metadata:
  product: Purview Telemetry Source Generator
  audience: maintainers
  repository: purview-dev/telemetry-sourcegenerator
---

# Running and documenting the telemetry benchmarks

This skill is for developers **working on** the generator. It covers the whole loop: run the suite,
read the reports, update the two performance documents, and write the analysis a consumer needs to
make a decision. It is not consumer guidance, and it is not published - this repository's
consumer-facing skills live in `skills/` and are installed with `npx skills add`.

Load this skill before you quote, edit, or add a performance number.

## Non-negotiables

1. **Every published number comes from one fresh Release run.** Never copy a stale table, never
   estimate, never interpolate between runtimes. If you cannot run the suite, say so instead of
   editing the docs.
2. **Debug numbers are meaningless.** `just benchmark` and `just benchmark-docs` pin `Release`
   deliberately; do not substitute `just build`-style Debug output.
3. **Never mix rows from different runs, machines or SDKs** in one table. A table is one report.
4. **Never cancel a benchmark run.** The full suite takes tens of minutes. To shorten the loop,
   filter to one family - do not kill a run half-way and keep its numbers.
5. **Observable instruments are not benchmarked by design** (`ObservableCounter`,
   `ObservableGauge`, `ObservableUpDownCounter`): they register a callback once and have no
   per-operation hot path. Do not add them, and do not remove the explanatory paragraph that says so.
6. **`BenchmarkDotNet.Artifacts/` is gitignored.** It is a local scratch folder, not a published
   artifact, so a doc must not link to it on GitHub. The CSV/HTML files stay on the machine that ran
   them; the report numbers are what get committed.

## The suite at a glance

| Benchmark class | What it measures | Feeds |
| --- | --- | --- |
| `ActivityBenchmarks` | Generated vs hand-written Activity telemetry, `HasListener` true/false | README § Activities, wiki § Activities |
| `LoggerBenchmarks` | Generated v1 (`LoggerMessage.Define`) and v2 (`ThreadLocalState`) vs manual, `HasLogging` true/false | README § Logging, wiki § Logging |
| `MultiTargetVsSingleTargetBenchmarks` | Activity-only vs Activity+Logging+Metrics from one call, `HasListener` | README § Multi-Target, wiki § Multi-target |
| `LoggerMultiTargetBenchmarks` | Logging-only vs the full multi-target stack, v1/v2/manual | wiki § Logger multi-target |
| `MetricsBenchmarks` | Counter / UpDownCounter / Histogram, generated vs manual, 0 and 1 tag | README § Metrics, wiki § Metrics |
| `TagListBenchmarks` | Inline `KeyValuePair` (< 4 tags) vs stack-allocated `TagList` (4+ tags) | README § Metrics, wiki § Metrics |
| `GeneratorModelBenchmarks` | The generator's own `closed record` models vs a value-based `[Union]` struct, per method count | wiki § Generator model overhead |

The first six measure code a consumer runs. `GeneratorModelBenchmarks` measures the generator's own
data model and is contributor-facing: it backs the recorded decision in
`docs/wiki/Platform-Support.md` to keep the `closed record` hierarchies. Do not promote it into the
README's consumer performance section.

Exact table shapes, section headings and column headers for each family live in
[`references/performance-doc-shapes.md`](references/performance-doc-shapes.md).

## Supported runtimes and jobs

One invocation measures all three legs, because every class declares one `[SimpleJob]` per runtime:

| Attribute | Runtime | Notes |
| --- | --- | --- |
| `[SimpleJob(RuntimeMoniker.Net10_0)]` | .NET 10 | The line `Directory.Packages.props` targets. |
| `[SimpleJob(RuntimeMoniker.Net11_0)]` | .NET 11 | Needs BenchmarkDotNet **0.16.0-preview.2+**; 0.15.8's `RuntimeMoniker` stops at `Net10_0`. |
| `[SimpleJob(RuntimeMoniker.Net48)]` | .NET Framework 4.8 | Windows only. The `netstandard2.0` asset path - the only way to compare it against the modern runtimes. |

- The suite is *launched* with `--framework net11.0`; that only selects the BenchmarkDotNet host. The
  job attributes decide what is measured, so the report carries a section per runtime.
- `RuntimeMoniker.Net47` is deliberately absent: its generated host project fails to restore on every
  run and only ever produced `NA` rows. Do not add it back.
- `RuntimeMoniker.Net12_0` already exists in the pinned preview package, so a future runtime needs no
  dependency change - but re-check `BenchmarkDotNet.Annotations.dll` before claiming a moniker exists.
- The project pins its own `BenchmarkDotNet` version; it is not in `Directory.Packages.props`.
  `benchmarks/` sits outside `src/`, so it does not import `Purview.BuildSdk`.

Prerequisites: the SDK pinned in `global.json`, plus the .NET 10 and .NET 11 runtimes installed
(each job resolves its own runtime), and Windows for the `net48` leg.

## Run it

```bash
# Full suite, all three runtimes, checked-in Release configuration (tens of minutes)
just benchmark

# Same, then prints the docs-update checklist
just benchmark-docs

# Short-job smoke test, own artifacts folder - never a source for published numbers
just benchmark-quick
```

Direct invocation, when you need to filter or pass extra arguments:

```bash
dotnet run --project ./benchmarks/Purview.Telemetry.Benchmarks/Purview.Telemetry.Benchmarks.csproj \
  --configuration Release --framework net11.0 -- --filter "*ActivityBenchmarks*"
```

- `--filter` matches the fully-qualified class name; `"*MultiTarget*"` also catches
  `LoggerMultiTargetBenchmarks`, so prefer the exact class name when you want one family.
- `--list flat` lists every benchmark without running anything.
- `Program.cs` appends `--filter *` when the arguments select nothing, so `--artifacts <path>` or
  `--job short` alone no longer drops into the interactive prompt. A `--filter` or `--list` you pass
  is forwarded untouched.
- `--artifacts <path>` redirects output; `just benchmark-quick` uses `BenchmarkDotNet.Artifacts/quick`
  so a smoke run cannot overwrite the full suite's report.
- `benchmark-quick` also passes `--runtimes net11.0` while the classes declare their own jobs. Treat
  it as "short job, smoke only" and read the report header to see which runtimes actually ran - never
  assume its scope, and never lift a number from it.

## Read a report

Each benchmark class writes three files to `BenchmarkDotNet.Artifacts/results/`:

```
Purview.Telemetry.Benchmarks.Benchmarks.<Class>-report-github.md   # the table to copy
Purview.Telemetry.Benchmarks.Benchmarks.<Class>-report.csv          # Error/StdDev/Median live here
Purview.Telemetry.Benchmarks.Benchmarks.<Class>-report.html         # full detail, per-method
```

The `-report-github.md` opens with a fenced environment block: BenchmarkDotNet version, OS build, CPU,
RAM, SDK, then one line per job (`[Host]`, `Job-XXXXX`) with its exact runtime version. That block is
the source for both documents' environment headers, and it is the only honest answer to "which
runtimes did this run measure?".

Column semantics:

- **Ratio** is relative to the row marked `Baseline = true` in that class and parameter group, not to
  the manual implementation in general. `0.98` means 2% faster than the baseline.
- **Allocated** `-` means no allocation on that path. Do not claim "zero allocations" for a row that
  shows bytes; `Gen0`/`Gen1` are the GC counts that come with them.
- **Alloc Ratio** appears only where allocations differ between rows.
- `Error`, `StdDev`, `Median`, `RatioSD` are hidden by `[HideColumns]` to keep the GitHub table
  narrow. Read them from the CSV or HTML before calling a difference real.
- The bold row is the baseline. Keep that bolding when you copy a table.
- A run that reports "Benchmarks with issues" or `NA` rows is a broken run. Fix the cause (usually a
  missing runtime or a host-project restore failure) and re-run; do not document the gap.

## Update the documents

Do these in order, from the same run. Nothing below is optional because "it looked the same".

1. **`docs/wiki/Performance.md`** - the full cross-runtime page.
   - `## Environment`: replace the fenced block with the fresh report header (BenchmarkDotNet, OS, CPU,
     SDK, and one line per measured runtime).
   - One section per family, in the existing order, each with a row per runtime and per parameter
     value. Keep the `**Source:**` line naming the class.
   - `GeneratorModelBenchmarks` has no section yet: add `## Generator model overhead` on the first
     refresh that carries its report, and keep it wiki-only.
   - Rewrite each `**Interpretation:**` paragraph from the new numbers (see "Write the analysis").
   - Delete stale caveats the new run makes untrue - the "`.NET 11.0` is not measured yet",
     "`.NET 8`/`.NET 9` rows were removed", and ".NET Framework produced no results" notes - and add a
     caveat only for a limitation that still exists.
   - Leave the `## Observable instruments` paragraph alone unless the generator's design changes.
2. **`README.md`** - `## Performance`, the consumer-facing digest.
   - One environment line (machine + SDK) matching the fresh run; it currently reads
     "Benchmarked on ... .NET SDK ...".
   - One table per family, deliberately narrower than the wiki: the headline runtime only. Keep the
     runtime in the heading and in the environment line consistent - if the headline runtime changes,
     change the heading, the sentence and the environment line together.
   - Keep the prose that tells a consumer what the number means ("generated activities match
     hand-written code with identical allocations"), refreshed against the new ratios.
3. **`benchmarks/README.md`** - only when the suite itself changed: the family table, the "runtimes and
   jobs" table, the per-family detail sections and the project-structure tree all list the classes and
   the monikers.
4. **`docs/wiki/Platform-Support.md`** - when `GeneratorModelBenchmarks` numbers back (or contradict)
   the recorded decision to keep the `closed record` models, update that row's reasoning. It currently
   says the tables predate the benchmark; once they do not, say so.
5. **`AGENTS.md`** - the "Benchmarking and performance documentation" section carries the commands, the
   three runtimes and the moniker constraints. It changes only when the suite changes.
6. **`CHANGELOG.md`** - only when a result changes a documented decision or adds a family, not for a
   routine refresh of the numbers.

Never edit `BenchmarkDotNet.Artifacts/**` by hand, and never commit it.

## Write the analysis

The tables are data; the paragraph after them is the product. A consumer reads one sentence to decide
whether generated telemetry is safe to adopt.

Cover these four things, in this order:

1. **What is compared** - the baseline and the alternatives, and whether the parameter (listener
   present, logging enabled) represents the production path.
2. **The delta** - time ratio and allocation side by side, for the runtime you are quoting. "Within 2%"
   and "identical allocation" are different claims; make both or neither.
3. **Why** - name the mechanism when you know it: framework work dominating Activity creation, the
   `IsEnabled` short-circuit, `[MethodImpl(AggressiveInlining)]` folding the generated call into the
   caller, the inline `KeyValuePair` vs `TagList` threshold at 4 tags.
4. **The caveat** - machine, SDK, and the fact that numbers on another host will differ.

Rules of thumb:

- Sub-10 ns means are noise-dominated. Check `Error`/`StdDev` in the CSV before attributing a 0.02 ns
  difference to generated code; usually the honest sentence is "indistinguishable".
- Do not compare a runtime's row to another runtime's row as a claim about generated code - the runtime
  dominates. Compare generated vs manual *within* a runtime.
- Do not compare this run's numbers to the previous run's as a regression unless the machine and SDK are
  the same; state the comparison basis if you do.
- Every ratio you quote must exist in the report. If you want to say "~7%", the rows must say
  `1.07`.
- Keep the consumer's frame: absolute nanoseconds for the hot path, allocations per call, and whether
  the generated code is on par with "what you would write by hand".

Anti-patterns to avoid: "blazing fast", "no overhead" for a measured 1.1x, quoting a Debug run, quoting
the `HasListener = false` row as the production cost, and describing a family the run did not measure.

## Add or change a benchmark family

1. `Telemetry/` - the interface(s) the generator consumes (`[ActivitySource]`, `[Logger]`, `[Meter]`,
   or the multi-target combination). Give the v1 logging variant its own interface
   (`GenerationMode = LoggerGenerationMode.V1`) if the family compares generations.
2. `Manual/` - the hand-written baseline, written the way a careful developer would: `LoggerMessage.Define`
   for logging, `Create*` for instruments, `ActivitySource.StartActivity` for activities. A weak baseline
   invalidates the comparison.
3. `BenchmarkHelpers.cs` - a factory that builds every implementation for a fair comparison: one logger and
   one `SimpleMeterFactory` shared across the manual and generated instances, an always-sampling
   `ActivityListener` for the listener-present parameter.
4. `Benchmarks/<Name>Benchmarks.cs` - required shape:
   - `[MemoryDiagnoser]`, `[HideColumns("Error", "StdDev", "Median", "RatioSD")]`, and the three
     `[SimpleJob]` attributes above, in that order.
   - `[Params(true, false)]` for `HasListener`/`HasLogging`; do not silently measure only one side.
   - Exactly one `Baseline = true`, on the row a consumer would compare against (usually manual).
   - `Description` strings that read as a sentence in the table - they are what the docs copy.
   - A `<summary>` that says what question the benchmark answers and why the pair is fair. This is where
     the mechanism the analysis will cite is recorded.
   - `[GlobalSetup]` / `[GlobalCleanup]` dispose meters, loggers and listeners.
5. Register it: `benchmarks/README.md` (family table + project tree), `docs/wiki/Performance.md` (new
   section with one row per runtime), and `README.md` only if it is a consumer hot path.
6. Validate as described below, then run the full suite - a new family cannot be documented from a
   filtered run that never executed it on all three legs.

`[SimpleJob]` on the class, not `[SimpleJob]` on a host runtime: a host-runtime job carries no `Runtime`
characteristic and fails BenchmarkDotNet's `GetSupportedBenchmarks` validation instead of measuring.

## `GeneratorModelBenchmarks` specifics

- It replicates the generator's internal models inside the benchmark project, because the generator's
  records are internal to the analyzer assembly. Keep the replica faithful to
  `src/src/SourceGenerator/Records/` when either shape changes, and say so in the class remarks.
- It exists to answer one question: is the per-method allocation and virtual dispatch of the record
  hierarchy measurable against a pass that also parses symbols, builds templates and writes source? A
  delta of tens of nanoseconds per method against a generation pass is not a throughput story.
- Report it in `docs/wiki/Performance.md` only, with a row per runtime and per `MethodCount`. A
  `.NET Framework` result that is *slower* for the struct union is a real outcome, not a mistake - it is
  part of why the records stayed.

## Validate before committing

- `just lint` - CSharpier, no writes. `just lint-fix` to apply. Any C# you touched in `benchmarks/` is
  formatted by the same check.
- `dotnet build ./benchmarks/Purview.Telemetry.Benchmarks/Purview.Telemetry.Benchmarks.csproj --configuration Release`
  - a compile check of a new or edited family that costs seconds instead of a benchmark run. Note that
  `just build` does **not** cover this project: it is outside `src/` and not in the main solution, so a
  broken benchmark class passes the normal build.
- Re-read both documents against the report you were given: every row traces to the report, every
  environment line matches the report header, no stale caveat survives, and no table mixes runs.
- Re-read the interpretation paragraphs for claims the report does not support.
- A docs-only change needs no test run; a change to the suite needs `just benchmark` (full) before its
  numbers are documented.
- Commit as `docs:` for a numbers refresh, `perf:` or `test:` for a suite change, per the conventional
  commit rules in `AGENTS.md`.

## Traps that have bitten before

- `just build` is green while `benchmarks/` does not compile - the project is not in the solution.
- `--filter "*MultiTarget*"` matches two classes; the report then covers only part of what the docs claim.
- `net48` silently disappears on a non-Windows machine, and with it the `netstandard2.0` comparison.
- A missing .NET 10 or .NET 11 runtime turns a leg into `NA` rows rather than a failure.
- `BenchmarkDotNet.Artifacts/` is gitignored, so a "raw results" link to it on GitHub is a dead link.
- `benchmark-quick` output looks like a result but is a short job; publishing it overstates precision.
- The README headings pin a runtime (".NET 10.0"). Refreshing the table without refreshing the heading
  is how a doc ends up claiming one runtime while showing another.
