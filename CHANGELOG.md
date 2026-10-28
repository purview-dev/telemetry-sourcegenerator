# Changelog

All notable changes to `Purview.Telemetry.SourceGenerator`.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versions come from the
`version` field in `package.json`, which is the single source of truth. This file starts at
5.0.7; earlier releases are described by their [GitHub releases](https://github.com/purview-dev/telemetry-sourcegenerator/releases)
and, for breaking changes, by [Breaking Changes](docs/wiki/Breaking-Changes.md).

## [5.0.7-prerelease.1] - 2026-10-28

No change to the generated code or to the package's contents: consumers of the package see the same
generator. This release changes how the package is built and what has to pass before it ships.

### Fixed

- `Purview.SourceGeneratorFramework.dll`, a compiler-host-only assembly, was copied into the build
  output of every project that referenced the generator project as an analyzer. Built on `main`, all
  eight sample projects' `bin` folders carried it, and an application built that way would ship it in
  its `deps.json`, publish output and Blazor WebAssembly `_framework` folder, where WebAssembly AOT
  compilation fails on it. The cause was Purview.SourceGeneratorFramework 1.2.1's build targets;
  1.2.2 fixes them, and the generator now builds against 1.2.2. The packed generator was never
  affected: it ships the framework IL-merged under `analyzers/`.

### Changed

- Purview.SourceGeneratorFramework (and its testing packages) 1.2.1 → 1.2.2.
- Purview.BuildSdk 1.1.0 → 1.2.0 and Purview.Skills.Testing 1.1.0 → 1.2.0. The SDK refreshed its
  managed `.editorconfig`; no rule changed for this repository, and no new diagnostic fired.

### Added

- **Packed-consumer gate** (`just smoke-packed-consumers`, `scripts/test-packed-consumers.ps1`).
  Packs the generator to an isolated feed and cache, then builds consumers that reference the
  package only - each also references a class library with its own generated telemetry:
  - a **Native AOT** console, published with `-warnaserror` and run with an in-process
    `ActivityListener`, `MeterListener` and capturing logger provider, asserting the multi-target
    method, activity tags, baggage and events (including the OpenTelemetry exception event), every
    metric measurement, the v1 and v2 log entries, log-level gating and `TelemetryNames`
    aggregation;
  - a **Blazor WebAssembly** app using the telemetry from a page and a service, published trimmed
    and with `RunAOTCompilation=true`; the trimmed assemblies must keep every generated member and
    the AOT compiler must have compiled every generated method;
  - a **Blazor Server** (interactive server) host, rendered on an in-memory `TestServer` and checked
    for the same telemetry.
- **Compiler-host leak scan** (`scripts/assert-no-compiler-host-assemblies.ps1`,
  `just scan-compiler-host-leaks`): fails when build or publish output - by file name, or in a
  `deps.json`, `runtimeconfig.json` or WebAssembly boot manifest - carries
  `Purview.SourceGeneratorFramework*`, `Microsoft.CodeAnalysis*` or `Purview.Telemetry.SourceGenerator*`.
  It runs on every consumer in the gate and on the samples' build output in CI.
- **Pack validation `ForbiddenContent` rules** in `.config/purview-build.json`, keeping compiler-host
  assemblies out of every package folder an application receives, and a rule proof
  (`just verify-pack-rules`, `scripts/test-pack-validation-rules.ps1`) that injects a file per rule
  into scratch copies of the package and shows each one is rejected.
- `ReleaseGateTests` in `Repository.UnitTests`, which keep the gates honest and wired up.
- A `packed-consumers` job in `pr.yml`, and `SampleApp.MultiProject` built and leak-scanned in the
  `samples` job.

[5.0.7-prerelease.1]: https://github.com/purview-dev/telemetry-sourcegenerator/compare/v5.0.6...v5.0.7-prerelease.1
