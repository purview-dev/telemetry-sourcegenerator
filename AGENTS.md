# AGENTS.md

Primary instruction set for working in this repository. Read this file first and treat it as the
source of truth for build commands, structure, conventions, and validation. Fall back to searching
the repository only when something here does not match what you observe.

## Project overview

Purview Telemetry Source Generator is a .NET incremental source generator that turns interface
method definitions into telemetry implementations:

- [`ActivitySource`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysource)-based
  distributed tracing (Activities, Events, Baggage/Context)
- [`ILogger`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger)-based
  structured logging
- [`Metrics`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics)-based instruments
  (Counter, AutoCounter, UpDownCounter, Histogram, Observable*)

The repository also ships Visual Studio code refactorings (convert `ILogger`/`ActivitySource`/metrics
code to generated telemetry interfaces) in a companion assembly.

## Prerequisites

- .NET 10 SDK (projects target `net10.0`; the source generator is a Roslyn component targeting
  `netstandard2.0`)
- [Bun](https://bun.sh) for the `package.json`/`.build/*.ts` scripts
- The `Purview.DotNetProjectSdk` MSBuild SDK (pinned in `global.json` under `msbuild-sdks`)
- `csharpier` dotnet tool (pinned in `.config/dotnet-tools.json`) for linting/formatting

## Commands

All `just` recipes read the repository's [`Justfile`](Justfile). Configuration defaults to **Debug**.

### Build and test

| Command | Purpose |
| --- | --- |
| `just build` | Builds `src/Telemetry.SourceGenerator.slnx` (Debug). |
| `just test` | Runs the test solution (Debug) with a tree-node filter. |
| `just build-s` | Builds `samples/SampleApp/SampleApp.slnx`. |
| `just test-s` | Runs the sample test solution. |
| `just clean` | Cleans the main solution. |
| `just restore` | Restores packages for the main solution. |
| `just scrub` | Removes `bin`/`obj` folders, cleans, restores, and shuts down build servers. |

### Formatting and linting

| Command | Purpose |
| --- | --- |
| `just format` | Runs `dotnet format` over `./src/`. |
| `just lint` | Runs `dotnet csharpier check .` (no writes). |
| `just lint-fix` | Runs `dotnet csharpier format .` (writes). |

`csharpier check` also runs in the pre-commit hook (`.config/lefthook.yml`) alongside a commitlint
conventional-commits check.

### Versioning and packaging

| Command | Purpose |
| --- | --- |
| `just version` | Prints the current version from `package.json`. |
| `just update-version` | Runs `.build/update-version.ts` to sync the version into docs/samples. |
| `just pack` | Updates the version then packs the NuGet package into `artifacts/`. |

The version lives in `package.json`. **Current Version:** 5.0.0-prerelease.8 — applied to `Version` /
`PackageVersion` via the SDK's package.json version detection.

### Pipelines (reusable `purview-build` tool)

These mirror the GitHub CI/CD workflows and are the supported way to run a full pipeline locally:

| Command | Purpose |
| --- | --- |
| `just pipeline-pr` | Restore, build, lint, and tests (the PR gate). |
| `just pipeline-build` | Restore, build, lint (no tests). |
| `just pipeline-tests` | Build with tests enabled. |
| `just pipeline-release` | Full release: build, test, pack, publish, GitHub release. |
| `just pipeline-local-release` | Pack + publish to a local NuGet feed (see the Justfile note on argument quoting). |

### Benchmarks

| Command | Purpose |
| --- | --- |
| `just benchmark` | Runs the full benchmark project on `net10.0` (Debug config). |
| `just benchmark-quick` | Single-runtime `net10.0` run with a short BenchmarkDotNet job. |
| `just benchmark-docs` | Runs benchmarks, then prints a checklist of docs to update. |

> BenchmarkDotNet numbers are only meaningful from **Release** builds. When measuring performance,
> run the benchmark project directly with `--configuration Release` (see `benchmarks/README.md`).

### Timeouts and cancellations

- **NEVER CANCEL** build or test commands. They can take a long time (tens of minutes) in slow
  environments; give them a generous timeout (30–60+ minutes) instead.

## Project structure

```
src/
├── src/
│   ├── SourceGenerator/                    # Main incremental source generator (netstandard2.0, Roslyn)
│   │   ├── Analyzers/                      # Diagnostic analyzers for telemetry interfaces
│   │   ├── Emitters/                       # CodeWriter-based code emission
│   │   ├── Generators/                     # Incremental generator pipeline
│   │   └── Sdk/                            # SDK-style package content (props/targets)
│   ├── SourceGenerator.Refactorings/       # VS code refactorings (shipped inside the generator package)
│   └── Shared/                             # Shared source (TelemetryAttributeNames.cs)
├── tests/
│   └── SourceGenerator.IntegrationTests/   # TUnit integration tests (see below for target frameworks)
└── Telemetry.SourceGenerator.slnx          # Main solution

samples/
├── SampleApp/                              # .NET Aspire demo (AppHost, APIService, Web, Shared, ServiceDefaults, UnitTests)
│   └── SampleApp.slnx
└── SampleApp.Net48/                        # .NET Framework 4.8 console demo
    └── SampleApp.Net48.slnx

benchmarks/
└── Purview.Telemetry.Benchmarks/           # BenchmarkDotNet project (net48/net8.0/net9.0/net10.0)

.build/                                     # TS scripts (build-pack.ts, update-version.ts)
.agents/                                    # Agent skills/prompts/agents (see "Agent instructions")
.config/                                    # dotnet-tools.json, lefthook.yml
.github/workflows/                          # pr.yml, release.yml (reusable purview-dev/build workflows)
scripts/                                    # setup-release.* (legacy changesets DSC scripts)
docs/release-process.md                     # Release process documentation
```

## Build and validation workflow

After making changes to the source generator:

1. `just build` — build the main solution.
2. `just test` — run the integration tests.
3. `just build-s && just test-s` — build and test the sample application end to end.
4. `just lint` / `just format` — ensure C# formatting compliance before committing.

The sample projects enable `EmitCompilerGeneratedFiles`, so generated telemetry can be inspected in
`obj/*/generated/` to sanity-check generator output.

### Test project details

- Integration tests target `net8.0;net9.0;net10.0`, plus `net48` on Windows only.
- Tests use `Purview.SourceGeneratorFramework.Testing.TUnit` (TUnit + the framework's `CodeQuery`
  syntax-lookup API and assertion extensions). There is no Verify/snapshot library; refactoring tests
  use the framework's `CodeRefactoringTestBase` snapshot approach.
- When generator behavior changes, add or update tests in `src/tests/SourceGenerator.IntegrationTests/`.

## Conventions

- **Conventional commits**: enforced by commitlint (`.config/lefthook.yml`). Types: `build`, `chore`,
  `ci`, `docs`, `feat`, `fix`, `perf`, `refactor`, `revert`, `style`, `test`.
- **Formatting**: C# is formatted with csharpier; check with `just lint`.
- **Solutions**: `.slnx` solution files are used.
- **Source generator**: targets `netstandard2.0` for broad compiler-host compatibility and uses
  `Purview.SourceGeneratorFramework` (CodeWriter-based emission, incremental pipeline, value-equatable
  models).
- **SDK**: projects import `Purview.DotNetProjectSdk` via `Directory.Build.props`/`.targets`. The repo
  sets `ExcludePurviewTelemetry=true` (it does not consume the telemetry package it generates) and
  `NamespacePrefix=Purview.Telemetry` under `src/`. See `.agents/skills/sdk-*` for SDK behavior.

## Source-generator and testing skills

Load the relevant skill before doing specialist work:

- `.agents/skills/source-generator-codewriter-modernization/SKILL.md` — CodeWriter/XmlCommentWriter
  emission, incremental pipeline design, value equality, Roslyn best practices.
- `.agents/skills/source-generator-testing/SKILL.md` — picking the right runner/base, configuring
  options, `CodeQuery`, asserting incremental caching.
- `.agents/skills/tunit-test-authoring/SKILL.md` — TUnit base classes, assertion extensions,
  modernising existing tests.
- `.agents/skills/sdk-configuration-reference/SKILL.md`,
  `.agents/skills/sdk-project-behavior-and-detection/SKILL.md`,
  `.agents/skills/project-placement-defaults/SKILL.md` — `Purview.DotNetProjectSdk` configuration,
  project-type detection, and placement rules.
- `.agents/agents/sdk-consumer-setup.md` — generic agent spec for helping consuming repos adopt the SDK.

## Benchmarking and performance documentation

> **MANDATORY RULE:** Always run a fresh benchmark suite before updating performance numbers in
> `README.md`. Never copy stale results or estimate values — every performance table must come from a
> current run.

- Run with `just benchmark` or directly:
  `dotnet run --project ./benchmarks/Purview.Telemetry.Benchmarks/Purview.Telemetry.Benchmarks.csproj --configuration Release --framework net10.0`
- Results land in `BenchmarkDotNet.Artifacts/results/` as `*-report-github.md`, `*.csv`, `*.html`.
- `just benchmark-docs` runs the suite and prints the docs-update checklist.
- Observable instruments (`ObservableCounter`/`ObservableGauge`/`ObservableUpDownCounter`) are not
  benchmarked: they are registered once via a callback with no per-operation hot path.

## Release process

See [`docs/release-process.md`](docs/release-process.md) for the full flow. In short:

1. Push a feature branch and open a PR to `main`. `pr.yml` runs the reusable
   `purview-dev/build/.github/workflows/purview-build.yml` pipeline (build + lint + tests).
2. Merging to `main` triggers `release.yml`, which runs the reusable
   `purview-dev/build/.github/workflows/purview-release.yml` pipeline (build, test, pack, publish,
   GitHub release).
3. The version is read from `package.json`. Bump it and run `just update-version` to sync docs/samples,
   then use `just pipeline-local-release` to validate locally or `just pipeline-release` to publish.