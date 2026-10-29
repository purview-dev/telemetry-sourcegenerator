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

- The .NET 11 SDK pinned in `global.json` (currently an RC, `rollForward: latestMajor`), plus the .NET 8, 9 and
  10 runtimes for the test legs. Projects target `net10.0` by default; the source generator is a Roslyn component
  targeting `netstandard2.0`. Consumers need Visual Studio 2026 / the .NET 10 SDK or later (Roslyn 5.9.0+).
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
| `just smoke-packed-consumers` | The packed-consumer gate: Native AOT, Blazor WebAssembly and Blazor Server consumers of the `.nupkg`. |
| `just verify-pack-rules` | Proves every `PackValidation` `ForbiddenContent` rule rejects an injected file. |
| `just scan-compiler-host-leaks <dir>...` | Fails when build/publish output carries a generator, SGF or Roslyn assembly. |

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
.agents/skills/                             # Installed skills from other repos, committed (see "Installed skills")
skills-lock.json                            # The skills CLI lockfile for .agents/skills/, committed
skills/                                     # Authored skills shipped by this package (see "Authoring skills")
.config/                                    # dotnet-tools.json, lefthook.yml
.github/workflows/                          # pr.yml, release.yml (reusable purview-dev/build workflows)
scripts/                                    # release-gate scripts (see "Quality gates"), legacy setup-release.*
└── packed-consumers/                       # Native AOT, Blazor WebAssembly and Blazor Server consumers of the .nupkg
docs/release-process.md                     # Release process documentation
docs/wiki/                                  # User documentation (Platform-Support.md: TFM/hosting matrix, .NET 11)
```

## Build and validation workflow

After making changes to the source generator:

1. `just build` — build the main solution.
2. `just test` — run the integration tests.
3. `just build-s && just test-s` — build and test the sample application end to end.
4. `just lint` / `just format` — ensure C# formatting compliance before committing.

The sample projects enable `EmitCompilerGeneratedFiles`, so generated telemetry can be inspected in
`obj/*/generated/` to sanity-check generator output.

## Quality gates

Treat work as incomplete until every gate relevant to it passes. Before a release, all of them:

- `just scrub`, then `just pipeline-local-release --PublishLocalNuGet:LocalFeedPath=p:/_sync-projects/.local-nuget/`
  — restore, build with no new warnings, lint, tests, pack and pack validation (`RequiredContent` and
  `ForbiddenContent`).
- `just smoke-packed-consumers` — packs the generator to an isolated feed and cache and proves the
  `.nupkg` itself works: a Native AOT console (published with `-warnaserror`, then run, asserting the
  emitted activities, metrics and log entries, log-level gating and multi-target methods), a Blazor
  WebAssembly app (trimmed and `RunAOTCompilation`; no generated member trimmed away, every generated
  method AOT-compiled) and a Blazor Server host (page rendered on a `TestServer`, telemetry asserted).
  Every consumer's bin and publish output is leak-scanned. Set `LOCAL_NUGET_FEED_PATH` (or
  `TELEMETRY_PRERELEASE_FEED`) while Purview.SourceGeneratorFramework or Purview.BuildSdk is not on
  nuget.org yet. It needs the Native AOT toolchain and, for the WebAssembly AOT step, the `wasm-tools`
  workload.
- `just verify-pack-rules` after any change to `PackValidation` in `.config/purview-build.json`.
- `just build-s && just test-s` — the samples, which reference the generator as an analyzer project.
- `just lint` — CSharpier reports no change (`just lint-fix` to apply it).

Compiler-host assemblies (`Purview.SourceGeneratorFramework*`, `Microsoft.CodeAnalysis*`, and this
repository's `Purview.Telemetry.SourceGenerator*` generator and refactorings) must never reach an
application's build or publish output. `scripts/assert-no-compiler-host-assemblies.ps1` is the check;
any CI job that builds the samples or publishes an application must run it on what it wrote, and
`src/tests/Repository.UnitTests/ReleaseGateTests.cs` fails the test run when one does not. The same
tests keep the leak scan honest against crafted leaks, keep the `packed-consumers` job, the Justfile
recipes and every `ForbiddenContent` rule wired up, and require the consumers in
`scripts/packed-consumers/` to reference the generator as a package, never as a project. The full
table of gates is in [`docs/wiki/Release-Flow.md`](docs/wiki/Release-Flow.md#release-gates).

### Test project details

- Integration tests target `net8.0;net9.0;net10.0;net11.0`, plus `net48` on Windows only. The `net11.0` leg is the
  only one whose references carry `System.Diagnostics.ActivitySourceFactory`, so the `UseActivitySourceFactory`
  tests (`[RequiresActivitySourceFactory]`) run there and the fallback tests (`[RequiresNoActivitySourceFactory]`,
  `TSG3023`) run on the earlier legs.
- Gate new emitted APIs on the compilation (`Compilation.GetTypeByMetadataName`, surfaced through
  `TelemetryCapabilities`), not on the target framework: the System.Diagnostics.DiagnosticSource package brings .NET 11
  types to `net462`/`netstandard2.0`. The supported matrix and the list of .NET 11 features that are not generated
  yet live in [`docs/wiki/Platform-Support.md`](docs/wiki/Platform-Support.md); update it with any such change.
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
  `NamespacePrefix=Purview.Telemetry` under `src/`. See `.agents/skills/purview-buildsdk-*` for SDK behavior.
- **Analyzer release tracking**: the `TSG` rules are tracked in
  `src/src/SourceGenerator/AnalyzerReleases.{Shipped,Unshipped}.md` (wired as `AdditionalFiles`, so
  the Roslyn RS2000-series checks run under `EnforceExtendedAnalyzerRules=true`). Add new rules to
  `AnalyzerReleases.Unshipped.md`, then move them into a new `## Release x.y.z` section in
  `AnalyzerReleases.Shipped.md` at release time. `RS2003` is suppressed — see the comment in
  `src/src/SourceGenerator/SourceGenerator.csproj`.

## Installed skills (`.agents/skills/`)

Specialist source-generator and SDK guidance — `CodeWriter` emission, incremental pipeline design,
test runners and `CodeQuery`, `Purview.BuildSdk` configuration — is published by the repositories
that own it, and installed here by the skills CLI. The installed skills are **committed**:
`.agents/skills/**` and the CLI's `skills-lock.json` (source, skill path and content hash of every
entry) are both in source, so a clone has them without running anything.

| Skill | Source | Load before |
| --- | --- | --- |
| `purview-sourcegenerator-codewriter` | `purview-dev/sourcegenerator-framework` | writing or reviewing emitter code (`CodeWriter`, scopes, `Emitters/`) |
| `purview-sourcegenerator-pipeline` | `purview-dev/sourcegenerator-framework` | changing the incremental pipeline or its value-equatable models |
| `purview-sourcegenerator-diagnostics` | `purview-dev/sourcegenerator-framework` | adding or changing a `TSG` diagnostic or the analyzer |
| `purview-sourcegenerator-packaging` | `purview-dev/sourcegenerator-framework` | changing how the generator is packed or merged |
| `purview-sourcegenerator-testing` | `purview-dev/sourcegenerator-framework` | writing generator, analyzer or refactoring tests (`CodeQuery`) |
| `purview-sourcegenerator-tunit-tests` | `purview-dev/sourcegenerator-framework` | the TUnit base classes and assertion extensions |
| `purview-buildsdk-*` (8 skills) | `purview-dev/build-sdk` | build/SDK configuration, project layout and detection, testing, skill installation and skill tests |
| `purview-architecture`, `purview-architecture-testing`, `purview-engineering-principles`, `purview-persistence`, `purview-telemetry`, `purview-testing-conventions` | `purview-dev/engineering-playbook` | solution shape, architecture tests, engineering conventions, persistence, telemetry and testing conventions |

This repository's own skills in `skills/` are authored for **consumers** of its packages, not for use while working on this repository. They are never installed into this repository's `.agents/skills/`, which holds only skills installed from other repositories; `InstalledSkillTests` enforces this.

Skills are installed into the universal `.agents/skills/` location **only**. Never install copies
into `.claude/` or any other per-agent folder; `/.claude/` is gitignored for that reason. Pass
`-a universal`, which writes `.agents/skills/` and nothing else:

```bash
# add or update every skill from a source
npx -y skills@1 add purview-dev/sourcegenerator-framework -s '*' -a universal -y
npx -y skills@1 add purview-dev/build-sdk -s '*' -a universal -y
npx -y skills@1 add purview-dev/engineering-playbook -s '*' -a universal -y

# restore the committed set from skills-lock.json
npx -y skills@1 experimental_install
```

Commit the changed `.agents/skills/**` and `skills-lock.json` together, in their own `chore` commit.
Do not edit anything under `.agents/skills/` by hand — the CLI is its only writer, and a hand edit
no longer matches the lockfile's hash. This repository's *own* skills are never installed here; they
live in `skills/` (below). `Purview.BuildSdk`'s `InstallPurviewSkills` target delegates to the same
CLI; `src/Directory.Build.props` sets `PurviewSkillsAgents=universal` so it is restricted the same
way rather than defaulting to every agent (`*`). None of the restored packages ship skills, so
`ListPurviewSkills` has nothing to recommend and the set above is chosen by hand.

`src/tests/Repository.UnitTests/InstalledSkillTests.cs` keeps this honest: every `skills-lock.json`
entry has its `.agents/skills/<name>/SKILL.md`, every installed folder is in the lockfile, and no
`.claude/skills/` copy exists.

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