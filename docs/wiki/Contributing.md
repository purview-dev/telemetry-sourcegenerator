# Contributing

Contributions are welcome! This page covers development setup, build commands, and the conventions enforced in this repository.

## Prerequisites

- .NET 10 SDK (projects target `net10.0`; the source generator targets `netstandard2.0`)
- [Bun](https://bun.sh) for the `package.json`/`.build/*.ts` scripts
- The `Purview.DotNetProjectSdk` MSBuild SDK (pinned in `global.json` under `msbuild-sdks`)
- `csharpier` dotnet tool (pinned in `.config/dotnet-tools.json`) for linting/formatting

## Clone and set up

```bash
git clone https://github.com/purview-dev/telemetry-sourcegenerator
cd telemetry-sourcegenerator
```

All `just` recipes read the [`Justfile`](https://github.com/purview-dev/telemetry-sourcegenerator/blob/main/Justfile); configuration defaults to **Debug**.

## Common commands

| Command | Purpose |
| --- | --- |
| `just build` | Builds `src/Telemetry.SourceGenerator.slnx` (Debug). |
| `just test` | Runs the integration tests (Debug) with a tree-node filter. |
| `just build-s` | Builds `samples/SampleApp/SampleApp.slnx`. |
| `just test-s` | Runs the sample test solution. |
| `just lint` | Runs `dotnet csharpier check .` (no writes). |
| `just lint-fix` | Runs `dotnet csharpier format .` (writes). |
| `just clean` | Cleans the main solution. |
| `just restore` | Restores packages for the main solution. |
| `just scrub` | Removes `bin`/`obj` folders, cleans, restores, and shuts down build servers. |

## Build and validation workflow

After making changes to the source generator:

1. `just build` — build the main solution.
2. `just test` — run the integration tests.
3. `just build-s && just test-s` — build and test the sample application end to end.
4. `just lint` / `just format` — ensure C# formatting compliance before committing.

> [!NOTE]
> Build and test commands can take a long time (tens of minutes) in slow environments. Give them a generous timeout and never cancel them part-way.

## Testing

Integration tests live in `src/tests/SourceGenerator.IntegrationTests` and target `net8.0;net9.0;net10.0`, plus `net48` on Windows only. They use `Purview.SourceGeneratorFramework.Testing.TUnit` (TUnit + the framework's `CodeQuery` syntax-lookup API and assertion extensions). There is no Verify/snapshot library; refactoring tests use the framework's `CodeRefactoringTestBase` snapshot approach.

When generator behaviour changes, add or update tests in `src/tests/SourceGenerator.IntegrationTests/`.

## Conventions

- **Conventional commits** — enforced by commitlint (`.config/lefthook.yml`). Types: `build`, `chore`, `ci`, `docs`, `feat`, `fix`, `perf`, `refactor`, `revert`, `style`, `test`.
- **Formatting** — C# is formatted with csharpier; check with `just lint`.
- **Solutions** — `.slnx` solution files are used.
- **Source generator** — targets `netstandard2.0` and uses `Purview.SourceGeneratorFramework` (CodeWriter-based emission, incremental pipeline, value-equatable models).

## Pipelines

The repo mirrors its GitHub CI/CD pipelines locally with the reusable `purview-build` tool:

| Command | Purpose |
| --- | --- |
| `just pipeline-pr` | Restore, build, lint, and tests (the PR gate). |
| `just pipeline-build` | Restore, build, lint (no tests). |
| `just pipeline-tests` | Build with tests enabled. |
| `just pipeline-release` | Full release: build, test, pack, publish, GitHub release. |
| `just pipeline-local-release` | Pack + publish to a local NuGet feed (see the Justfile note on argument quoting). |

## Source-generator skills

Before doing specialist work, load the relevant skill from `.agents/skills/` — see `AGENTS.md` ("Source-generator and testing skills") for the full list and when to load each one.

## Release process

See [`docs/release-process.md`](../release-process.md) for the full flow. In short: push a feature branch and open a PR to `main` (`pr.yml` runs build + lint + tests); merging to `main` triggers the reusable release pipeline (build, test, pack, publish, GitHub release). The version lives in `package.json`; after bumping it, run `just update-version` to sync docs/samples, then `just pipeline-local-release` to validate locally.

## Next steps

- [Release Flow](Release-Flow.md) — how releases are produced
- [Testing](Testing.md) — mocking generated telemetry interfaces