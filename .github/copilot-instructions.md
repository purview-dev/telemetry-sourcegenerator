# Purview Telemetry Source Generator

Purview Telemetry Source Generator is a .NET incremental source generator that generates [`ActivitySource`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysource), [`ILogger`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger), and [`Metrics`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics) based telemetry from methods you define on an interface.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Prerequisites

Install these exact dependencies in order:

- Install .NET 10.0.100 SDK: `curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --version 10.0.100`
- Install .NET 10.0 runtime: `curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --runtime dotnet --version 10.0.0`
- Install Bun: `curl -fsSL https://bun.sh/install | bash`
- Set environment variables: `export PATH=$HOME/.bun/bin:$HOME/.dotnet:$PATH && export DOTNET_ROOT=$HOME/.dotnet`

### Bootstrap, Build, and Test

Bootstrap and build the repository:

- `just build` -- builds the main source generator and integration tests. Takes 26 seconds. NEVER CANCEL. Set timeout to 60+ minutes.
- `just test` -- runs 282 integration tests. Takes 42 seconds. NEVER CANCEL. Set timeout to 60+ minutes.
- `just format` -- formats code according to .editorconfig rules. Takes 21 seconds. NEVER CANCEL. Set timeout to 30+ minutes.

Alternative direct commands (use environment variables above):

- `dotnet build ./src/Purview.Telemetry.SourceGenerator.slnx --configuration Release`
- `dotnet test ./src/Purview.Telemetry.SourceGenerator.slnx --configuration Release`
- `dotnet format ./src/`

### Build and Test Sample Application

The sample application demonstrates the source generator in action:

- `cd samples/SampleApp && dotnet build --configuration Release` -- takes 19 seconds. NEVER CANCEL. Set timeout to 30+ minutes.
- `cd samples/SampleApp && dotnet test --configuration Release` -- runs 8 tests, takes 3 seconds.

### Package Creation

- `just pack` -- creates NuGet package with current version from package.json
- `just version` -- displays current version (currently 3.2.4)

## Validation

### Manual Validation Requirements

Always manually validate changes to the source generator:

- ALWAYS run `just build && just test` after making any changes to the source generator code
- ALWAYS build and test the sample application: `cd samples/SampleApp && dotnet build --configuration Release && dotnet test --configuration Release`
- ALWAYS run `just format` before committing to ensure code formatting compliance
- Test actual source generator functionality by examining generated files in the sample project (EmitCompilerGeneratedFiles is enabled)

### Functional Testing Scenarios

Test these scenarios when modifying the source generator:

- **Interface to Implementation Generation**: Modify an interface in `samples/SampleApp/SampleApp.Host/APIs/` and verify generated telemetry code appears
- **Activity Generation**: Test ActivitySource generation by adding methods with activity attributes
- **Logging Generation**: Test ILogger generation by adding methods with logging attributes
- **Metrics Generation**: Test metrics generation by adding methods with metrics attributes
- **Integration Test Coverage**: Verify new functionality is covered by tests in `src/Purview.Telemetry.SourceGenerator.IntegrationTests/`

### CI Validation

Always run these validation steps before committing:

- `just format` (takes 21 seconds)
- `just build` (takes 26 seconds)
- `just test` (takes 42 seconds)
- Sample app build and test (takes 22 seconds total)

The CI pipeline (`./.github/workflows/ci.yml`) runs the same dotnet restore → build → test workflow.

## Common Tasks

### Project Structure

```
src/
├── Purview.Telemetry.SourceGenerator/          # Main source generator library
├── Purview.Telemetry.SourceGenerator.IntegrationTests/  # 282 integration tests
├── Purview.Telemetry.SourceGenerator.slnx      # Main solution
└── global.json                                 # Pins to .NET 10.0.200

samples/
└── SampleApp/                                  # .NET Aspire demo application
    ├── SampleApp.AppHost/                      # Aspire AppHost
    ├── SampleApp.Host/                         # Main web API
    ├── SampleApp.ServiceDefaults/              # Shared service config
    ├── SampleApp.UnitTests/                    # Sample app tests
    └── SampleApp.slnx                          # Sample solution

benchmarks/
└── Purview.Telemetry.Benchmarks/               # BenchmarkDotNet benchmark project

.build/
└── update-version.ts                           # Version management script

Justfile                                        # Main build automation (just)
package.json                                    # Version 3.2.4, Bun scripts
```

### Key Commands Output

#### just --list

```
build          Builds the project.
test           Runs the tests for the project.
format         Formats the code according to the rules of the src/.editorconfig file.
release-final  Creates a new release, e.g. v3.0.1.
release-pre    Creates a new pre-release, e.g. v3.0.1-prerelease.1.
pack           Packs the project into a nuget package using PACK_VERSION argument.
vs             Opens the project in Visual Studio.
code           Opens the project in Visual Studio Code.
vs-s           Opens the sample project in Visual Studio.
version        Displays the current version of the project.
update-version Update related samples and docs to new version.
benchmark      Runs the benchmark project.
benchmark-docs Runs benchmarks and prints a reminder to update performance docs.
```

#### Repository Root Files

```
.build/          .config/         .cspell.json     .git/
.gitattributes   .github/         .gitignore       .gitmodules
.husky/          .vscode/         .wiki/           CHANGELOG.md
LICENSE.md       Justfile         README.md        PERFORMANCE.md
bun.lock         global.json      package.json     samples/         src/
```

### Source Generator Architecture

The source generator processes interface definitions and generates three types of telemetry code:

- **Activities**: Distributed tracing using ActivitySource
- **Logging**: Structured logging using ILogger
- **Metrics**: Performance metrics using .NET metrics APIs

Generated code includes:

- Implementation classes with telemetry instrumentation
- Dependency injection registration helpers
- Configuration and initialization code

### Generated Code Location

When `EmitCompilerGeneratedFiles` is true (as in the sample app), generated files appear in:

- `obj/Debug|Release/generated/` directories

### Integration Test Snapshots

Snapshot files in `src/Purview.Telemetry.SourceGenerator.IntegrationTests/Snapshots/` are **entirely machine-generated** by the [Verify](https://github.com/VerifyTests/Verify) library. **Never manually edit snapshot files.** They are only updated by:

1. Running `just test` (or `dotnet test`)
2. Reviewing the `*.received.*` diff output
3. Accepting changes — copy `*.received.*` → `*.verified.*`, or use the Verify CLI/IDE extension

Any manual edits to `.verified.*` files will be silently overwritten the next time tests run and snapshots are accepted.

### Version Management

- Version is managed in `package.json` (currently 3.2.4)

- `bun .build/update-version.ts` synchronizes version across all files
- `make release-final` and `make release-pre` create new releases using commit-and-tag-version

## Benchmarking and Performance Documentation

> **MANDATORY RULE:** Always run the full benchmark suite before updating `README.md` or `PERFORMANCE.md` with performance numbers. Never copy stale results or estimate values — every performance table must come from a fresh benchmark run. There are no exceptions.

### Running Benchmarks

Run all benchmarks with:

```
just benchmark
```

Or directly (must specify `--framework net10.0` because the project targets multiple frameworks):

```
dotnet run --project ./benchmarks/Purview.Telemetry.Benchmarks/Purview.Telemetry.Benchmarks.csproj --configuration Release --framework net10.0
```

- **NEVER CANCEL** benchmark runs — they take 30–60+ minutes across all runtimes (net47, net48, net8.0, net9.0, net10.0)
- BenchmarkDotNet spawns child processes for each target runtime automatically — no manual selection needed
- Results land in `BenchmarkDotNet.Artifacts/results/` as `*-report-github.md`, `*.csv`, and `*.html`

### Benchmark Classes

| Class | What it measures |
|---|---|
| `ActivityBenchmarks` | Activity (generated vs. manual), with/without listener |
| `LoggerBenchmarks` | Logging v1 (LoggerMessage.Define) vs. v2 (ThreadLocalState) vs. manual ILogger.Log |
| `LoggerMultiTargetBenchmarks` | Multi-target Activity+Logging+Metrics combined vs. single-target logging |
| `MultiTargetVsSingleTargetBenchmarks` | Single-target Activity-only vs. multi-target overhead |
| `TagListBenchmarks` | Metrics with 0–3 tags (direct KVP) vs. 4–6 tags (TagList struct) |
| `MetricsBenchmarks` | Counter, UpDownCounter, Histogram (generated vs. manual, 0 and 1 tag) |

**Observable instruments are excluded**: `ObservableCounter`, `ObservableGauge`, and `ObservableUpDownCounter` are registered once via a callback — there is no per-operation hot path to compare.

### Updating README.md Performance Section

After running benchmarks, update the `## Performance` section in `README.md`:

1. Open `BenchmarkDotNet.Artifacts/results/` and locate the `*-report-github.md` files
2. For each telemetry type, extract the `.NET 10.0` rows where `HasListener = True` (activities) or `HasLogging = True` (logging)
3. **Activities table** — source: `*ActivityBenchmarks*-report-github.md`
   - Rows: `HasListener=False` (fast path), `HasListener=True, start+complete`, `HasListener=True, start+fail`
   - Compare Manual (baseline) vs. Generated columns
4. **Logging table** — source: `*LoggerBenchmarks*-report-github.md`
   - Rows: `HasLogging=False, single call`, `HasLogging=True, single call`, `HasLogging=True, full lifecycle`
   - Compare `ILogger.Log` (manual) vs. `LoggerMessage.Define` (manual) vs. Generated v1
5. **Multi-target table** — source: `*MultiTargetVsSingleTargetBenchmarks*-report-github.md` or `*LoggerMultiTargetBenchmarks*`
   - Rows: no-listener, listener-active start+complete, listener-active full lifecycle
   - Compare Manual vs. Generated v1
6. **Metrics table** — source: `*MetricsBenchmarks*-report-github.md`
   - Rows: auto-counter 0 tags, auto-counter 1 tag, UpDownCounter, Histogram 0 tags, Histogram 1 tag
   - Compare Manual (baseline) vs. Generated
7. Update the environment line (machine, SDK version, .NET runtime version) from the benchmark header

Keep tables concise — `.NET 10.0` data only in README.md. Cross-runtime data belongs in `PERFORMANCE.md`.

### Regenerating PERFORMANCE.md

`PERFORMANCE.md` contains the full cross-runtime results for all six benchmark classes.

To regenerate it:

1. Run the full benchmark suite: `just benchmark` (run once per runtime or use multi-runtime run)
2. For each of the six `*-report-github.md` files in `BenchmarkDotNet.Artifacts/results/`:
   - Copy the BenchmarkDotNet environment header (machine info, SDK, runtimes)
   - Copy the full markdown table including all runtimes (net47/48/8/9/10)
3. Structure `PERFORMANCE.md` with:
   - A top-level environment section (copy from any report header)
   - One `##` section per benchmark class with the full table
   - A brief interpretation note per section (e.g. "generated is within X% of manual")
   - A `## Observable Instruments` section explaining why they are not benchmarked
   - A link to `BenchmarkDotNet.Artifacts/results/` for raw CSV/HTML data

### just benchmark-docs

The `just benchmark-docs` recipe runs benchmarks and then prints a reminder checklist of which docs to update. Use it as your end-to-end workflow:

```
just benchmark-docs
```

## CRITICAL Timing and Cancellation Warnings

- **NEVER CANCEL** any build or test command - builds may take up to 60 minutes in some environments
- **Build timeout**: Set 60+ minutes timeout for `make build` and related commands
- **Test timeout**: Set 60+ minutes timeout for `make test` and related commands
- **Format timeout**: Set 30+ minutes timeout for `make format`
- **Expected times**: Build ~26s, Test ~42s, Format ~21s, Sample build ~19s on typical hardware

## Important Development Notes

- Always use conventional commits
- The project uses .slnx solution files (Visual Studio 2022 format)
- Source generator targets netstandard2.0 for broad compatibility
- Integration tests target net9.0
- Sample application is a .NET Aspire application demonstrating telemetry integration
- Always test changes against the sample application to ensure end-to-end functionality
- The integration tests use Verify for snapshot testing of generated code — snapshots are machine-generated only, never hand-edited
