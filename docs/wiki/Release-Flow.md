# Release Flow

Releases are driven by GitHub Actions using the reusable [`purview-dev/build`](https://github.com/purview-dev/build) pipelines. No release steps are performed manually.

```text
Feature branch
  → PR to main      (pr.yml runs purview-build.yml: restore + build + lint + tests)
  → merge to main   (release.yml runs purview-release.yml: build, test, pack, publish)
  → GitHub Release  (NuGet package + changelog, created by the pipeline)
```

## Workflow components

| Component | File | Purpose |
| --- | --- | --- |
| PR gate | `.github/workflows/pr.yml` | Runs the reusable `purview-dev/build` `purview-build.yml` pipeline (restore, build, lint, tests), builds/tests the samples and leak-scans their output, and runs the packed-consumer gate (see [Release gates](#release-gates)). |
| CD pipeline | `.github/workflows/release.yml` | Runs the reusable `purview-dev/build` `purview-release.yml` pipeline on push to `main`. |
| Local pipeline | `Justfile` `pipeline-*` recipes | Mirror the CI/CD pipelines locally via the `Purview.Build` local tool (`dotnet purview-build`). |

## Developer workflow

1. Create a feature branch and make your changes.
2. Validate locally with `just pipeline-pr` (restore, build, lint, tests — the same gate CI enforces).
3. Push and open a PR to `main`. `pr.yml` must pass (build, lint, tests, plus the sample build/test job).
4. Merge the PR into `main`.
5. The release is published automatically — `release.yml` restores and builds the main and sample solutions, runs the integration tests, packs the NuGet package, publishes it, and creates a GitHub Release.

## Versioning

- The version lives in `package.json`. **Current Version:** `*` (run `just version` to print it)
- It is applied to `Version`/`PackageVersion` by `Purview.BuildSdk` via package.json version detection.
- `just version` prints the current version.
- After bumping `package.json`, run `just update-version` to sync the version into docs/samples.

## Building the package locally

| Command | Purpose |
| --- | --- |
| `just pack` | Updates the version, then packs the NuGet package into `artifacts/`. |
| `just pipeline-local-release` | Packs and publishes to a local NuGet feed (see the Justfile note on argument quoting for the feed path). |
| `just pipeline-release` | Full release pipeline (build, test, pack, publish, GitHub release). |

## Release gates

Each gate proves one thing the others cannot. The tests and the samples reference the generator project, which is
fast and debuggable but is not what ships; the packed gates consume the `.nupkg` itself. A gate that has nothing to
check fails rather than passing.

| Gate | What it proves | Local | CI |
| --- | --- | --- | --- |
| Build, lint and tests | The solution builds warning-free, CSharpier reports no change, and every test project passes. | `just build`, `just lint`, `just test` | `pr.yml` `build` |
| Pack and pack validation | The package packs; `purview-build.json` `PackValidation` finds exactly the declared content (`RequiredContent`, `RequireExplicitContent`) and none of the `ForbiddenContent`. | `just pipeline-pack-validate` (which also runs the rule proof) | `pr.yml` `build` |
| Forbidden-content rule proof | Every `ForbiddenContent` rule rejects a package: a matching file is injected into scratch copies of the package and the real `purview-build` validation must fail, per package and per rule. | `just verify-pack-rules` (and the end of `just pipeline-pack-validate`) | `pr.yml` `pack-validation`, in parallel with the consumer gate |
| Packed consumers | The package, packed to an isolated feed and cache, works for consumers that reference it as a package - each with a class library of its own generated telemetry. (a) A **Native AOT** console publishes with `-warnaserror` and no IL2xxx/IL3xxx warning, then runs with an in-process `ActivityListener`, `MeterListener` and capturing logger provider and checks the multi-target method (Activity, Information log and counter from one call), tags, baggage, events and the OpenTelemetry exception event, every metric measurement, v1 and v2 logging, log-level gating and the `TelemetryNames` aggregation. (b) A **Blazor WebAssembly** app, using the telemetry from a page and a service, publishes trimmed with `-warnaserror` and with `RunAOTCompilation=true`: the trimmed assemblies keep every generated member, and the WebAssembly AOT compiler compiles every generated method. (c) A **Blazor Server** host renders its page on an in-memory `TestServer`, and the telemetry the page emitted is checked as in (a). (d) Every bin and publish folder is leak-scanned. | `just smoke-packed-consumers` | `pr.yml` `packed-consumers` (linux-x64, `-WasmAot Require`) |
| Compiler-host leak scan | No generator, refactorings, Purview.SourceGeneratorFramework or Roslyn assembly reaches build or publish output, by file name or in a `deps.json`, `runtimeconfig.json` or boot manifest (`blazor.boot.json`, `dotnet.boot.js`, `dotnet.js`). Purview.SourceGeneratorFramework 1.2.1 put its DLL in every sample's `bin` while every build passed. | `just scan-compiler-host-leaks <dir>...` | the `samples` job (every sample's `bin`) and every consumer in `packed-consumers`; `ReleaseGateTests` fails a job that builds the samples or publishes without it |

`ReleaseGateTests` (in `src/tests/Repository.UnitTests`, so `just test` runs it) guards the gates themselves: the leak
scan must catch each kind of leak it claims to, every workflow job that builds the samples or publishes must scan its
output, the `packed-consumers` job must run the gate and the rule proof must have a cheap job of its own, the Justfile
recipes must run their scripts, every gate script must state its exit code rather than inherit a non-zero
`$LASTEXITCODE` (GitHub's `pwsh` shell exits with it, which reported a green proof as a failed step), the consumers
must reference the generator as a package and never the repository's projects, and `ForbiddenContent` must keep every
rule.

### Prerelease build dependencies

The generator is built against the Purview.SourceGeneratorFramework version `Directory.Packages.props` pins, with the
Purview.BuildSdk version `global.json` pins. Until those versions are on nuget.org, nothing can be packed, so the
`packed-consumers` job fails at its first step with what to do, rather than skipping. Publish them to nuget.org, or
rely on the organisation's GitHub Packages source, which the shared `build` workflow registers when required. Locally,
`just smoke-packed-consumers` reads `LOCAL_NUGET_FEED_PATH` (or takes `-PrereleaseFeed`) and uses that feed for those
packages only.

The consumers target `net11.0` and take their ASP.NET Core package versions from the `Packed consumers` group in
`Directory.Packages.props`, so bump those with the .NET 11 SDK in `global.json`.

## Pre-release validation

Run these on a clean tree before bumping and merging a release. Each must pass.

```powershell
$env:LOCAL_NUGET_FEED_PATH = 'P:\_sync-projects\.local-nuget'   # wherever the prerelease build dependencies are

just scrub                                    # delete bin/obj, clean, force-restore, stop build servers
just pipeline-local-release --PublishLocalNuGet:LocalFeedPath=p:/_sync-projects/.local-nuget/
                                              # restore, build, lint, tests, pack, pack validation, local publish
just smoke-packed-consumers                   # Native AOT, Blazor WebAssembly (trimmed and AOT) and Blazor Server
just build-s && just test-s                   # sample solution build and tests
just lint                                     # CSharpier reports no change
```

`just verify-pack-rules` re-proves the forbidden-content rules against the package in `artifacts/` (after
`just pipeline-local-release`, which moves it to the local feed, pass `-PackagesFolder <feed>`); run it after changing
`PackValidation`.

## Releasing

1. Bump the version in `package.json`.
2. Run `just update-version` to sync docs/samples.
3. Push a PR with the version bump; merge to `main`.
4. `release.yml` publishes the release automatically.

> The legacy `scripts/setup-release.*` files describe a changeset-based (`@changesets/cli`) flow that is **not** used by this repository. They are retained for reference only; the actual release automation lives in `.github/workflows/pr.yml` and `.github/workflows/release.yml`.

## See also

- [Contributing](Contributing.md) — development setup and conventions