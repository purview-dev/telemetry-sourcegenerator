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
- The `Purview.BuildSdk` MSBuild SDK (pinned in `global.json` under `msbuild-sdks`)
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
| `just build-mp` | Builds `samples/SampleApp.MultiProject/SampleApp.MultiProject.slnx`. |
| `just run-mp` | Runs the multi-project sample, printing the aggregated telemetry names. |
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

The version lives in `package.json`. **Current Version:** `*` — read from `package.json` at build
time and applied to `Version` / `PackageVersion` via the SDK's package.json version detection. Run
`just version` to print the current value; it is deliberately not duplicated here.

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
│   │   ├── AnalyzerReleases.Shipped.md     # Roslyn analyzer release tracking (shipped TSG rules)
│   │   ├── AnalyzerReleases.Unshipped.md   # Roslyn analyzer release tracking (upcoming TSG rules)
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
├── SampleApp.MultiProject/                 # .NET 10 console demo of TelemetryNames aggregation across projects
│   └── SampleApp.MultiProject.slnx         #   (Observability, Notifications, Payments, Ordering, ConsoleApp)
└── SampleApp.Net48/                        # .NET Framework 4.8 console demo
    └── SampleApp.Net48.slnx

benchmarks/
└── Purview.Telemetry.Benchmarks/           # BenchmarkDotNet project (net48/net8.0/net9.0/net10.0)

.build/                                     # TS scripts (build-pack.ts, update-version.ts)
.agents/                                    # Package-delivered skills/prompts/agents (gitignored mirror)
skills/                                     # Authored skills shipped by this package (see "Authoring skills")
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
- **SDK**: projects import `Purview.BuildSdk` via `Directory.Build.props`/`.targets`. The repo
  sets `ExcludePurviewTelemetry=true` (it does not consume the telemetry package it generates) and
  `NamespacePrefix=Purview.Telemetry` under `src/`. See `.agents/skills/sdk-*` for SDK behavior.
- **Analyzer release tracking**: the `TSG` rules are tracked in
  `src/src/SourceGenerator/AnalyzerReleases.{Shipped,Unshipped}.md` (wired as `AdditionalFiles`, so
  the Roslyn RS2000-series checks run under `EnforceExtendedAnalyzerRules=true`). Add new rules to
  `AnalyzerReleases.Unshipped.md`, then move them into a new `## Release x.y.z` section in
  `AnalyzerReleases.Shipped.md` at release time. `RS2003` is suppressed — see the comment in
  `src/src/SourceGenerator/SourceGenerator.csproj`.

## Skills from other repositories

Specialist source-generator and SDK guidance — `CodeWriter` emission, incremental pipeline design,
test runners and `CodeQuery`, `Purview.BuildSdk` configuration — is published by the repositories
that own it. Install what you need through the skills CLI; it lands in `.agents/skills/`, which is
gitignored and recorded in `skills-lock.json`:

```bash
npx skills add purview-dev/sourcegenerator-framework
npx skills add purview-dev/build-sdk
```

Do not edit anything under `.agents/skills/` — the CLI is its only writer.

## Authoring skills

Authored skills live in the repository-root `skills/` folder. **Nothing packs them into a NuGet
package.** `Purview.BuildSdk` 1.0.6 removed skill packaging outright — the skills CLI clones the
*git* repository and only discovers a root `skills/` folder, so that is the one delivery route.
`PurviewSkillsToPack`, the `IncludeRepositorySkillsInPackage` shim in `src/Directory.Build.targets`,
and the `.agents/**/*` entry in `.config/purview-build.json` went with it.

```bash
npx skills add purview-dev/telemetry-sourcegenerator
```

| Skill | Answers |
| --- | --- |
| `skills/purview-telemetry-quickstart/` | adding the package, a first telemetry interface, registering it in DI |
| `skills/purview-telemetry-design/` | one interface or several, naming, structuring multi-target methods |
| `skills/purview-telemetry-migration/` | converting existing `ILogger`, `ActivitySource` or instrument code |
| `skills/purview-telemetry-multi-targeting/` | emitting an Activity, a log and a metric from one method |
| `skills/purview-telemetry-diagnostics/` | what a `TSG` diagnostic means and how to fix it |

Every skill carries the `purview-telemetry` prefix. Skills install into one flat directory per agent,
shared with every skill a developer has installed from anywhere, so an unprefixed name collides and
the loser is whichever installed second.

To add a skill: create `skills/<name>/SKILL.md` with spec-legal frontmatter, and nothing else. There
is no per-project registration any more.

`src/tests/Repository.UnitTests/SkillTests.cs` validates every skill on every test run, through
`Purview.Skills.Testing`'s rule set (the Agent Skills specification: frontmatter fields, name and
description constraints, bundled links, body size). The `[InheritsTests]` attribute on that class is
load-bearing — TUnit does not walk base types during discovery, so without it the run is green and
nothing is checked. That project is single-targeted at `net10.0`, because it asserts something about
the repository rather than about the runtime's reach.

A relative link from one `SKILL.md` into another skill's folder is a rule violation: only a skill's
own folder is installed with it, so such a link works here and nowhere else. Name the other skill in
prose instead, and keep deep detail in the skill's own `references/`.

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