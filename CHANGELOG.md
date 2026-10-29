# Changelog

All notable changes to `Purview.Telemetry.SourceGenerator`.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versions come from the
`version` field in `package.json`, which is the single source of truth. This file starts at
5.0.7; earlier releases are described by their [GitHub releases](https://github.com/purview-dev/telemetry-sourcegenerator/releases)
and, for breaking changes, by [Breaking Changes](docs/wiki/Breaking-Changes.md).

## [5.0.7-prerelease.3] - 2026-10-28

The `TSG1011` message no longer offers an opt-out that never existed, and the define it named is
gone. Generated code is unchanged for every project that did not define it.

### Changed

- **`TSG1011` (unsupported target framework) has a new message.** It said that defining
  `PURVIEW_TELEMETRY_NON_NULLABLE` suppresses the error. Nothing ever implemented that: the check reads
  only `NET8_0_OR_GREATER` and `NET48_OR_GREATER`, so the diagnostic was raised regardless. The message
  now says no telemetry is generated for the target, and that a project must target `net8.0`+ or
  `net48`+, with a multi-targeted library referencing the package only from its supported targets.
  The rule's ID, title, category and severity are unchanged, so the analyzer release files are too.

### Removed

- **`PURVIEW_TELEMETRY_NON_NULLABLE`.** Beyond the false `TSG1011` claim, its one effect was on the
  generated marker attributes: it dropped `#nullable enable` and declared their optional members as
  `string` rather than `string?`. The attributes now switch on `NET48_OR_GREATER` alone. A `net8.0`+
  project that defined it gets the `string?` declarations every other modern project gets, which
  accept every value the plain `string` ones did. `net48` projects are unaffected.

## [5.0.7-prerelease.2] - 2026-10-28

.NET 11 support. Generated code is unchanged unless you opt in to the new option. The one
exception is the opt-in `MeterNameGenerationType.OpenTelemetry` naming, which was fixed (see below).

### Added

- **`ActivitySourceFactory` (.NET 11).**
  `[assembly: ActivitySourceGeneration(..., UseActivitySourceFactory = true)]` makes the generated
  classes take a `System.Diagnostics.ActivitySourceFactory` constructor parameter and create their
  source with `Create(new ActivitySourceOptions(name))`, instead of a static `ActivitySource`.
  Register the factory with `services.AddTracing()`. The option is gated on the compilation resolving
  the type (.NET 11, or System.Diagnostics.DiagnosticSource 11+). When the type is unavailable,
  the static source is kept and the new warning **`TSG3023`** reports it.
- [Platform Support and .NET 11](docs/wiki/Platform-Support.md): the target-framework and hosting
  matrix, the trim/AOT guarantees, the .NET 11 features and what is not supported yet.
- The integration tests run on a `net11.0` leg.
- The packed-consumer gate exercises `UseActivitySourceFactory` under Native AOT, Blazor
  WebAssembly (trimmed and AOT) and Blazor Server, alongside a static source in the library.

### Fixed

- `MeterNameGenerationType.OpenTelemetry` placed the meter name between the interface prefix and
  the instrument name (`product.my_app.products.record_count`). It is now the outermost namespace
  (`my_app.products.product.record_count`), as documented. The default (`DotNet`) is unchanged.
  **If you opted in, your instrument names change.**
- The README and wiki said OpenTelemetry naming prefixes instrument names with the meter name by
  default. It never has; the docs now describe the interface-derived prefix
  (`IProductTelemetry.RecordCount` becomes `product.record_count`), and tests pin the examples.
- Static interface members are ignored. They were implemented as instance methods and reported as
  duplicates (`TSG1003`). This matters now because C# 15 allows them on every runtime.
- The packaged README no longer pins a package version. 5.0.6 shipped a README telling consumers to
  install 5.0.5. `PackagedDocumentationTests` now fails on any version in a packaged document.
- `MessageTemplateHole` is internal. As a public type it exposed a Purview.SourceGeneratorFramework
  type on the merged analyzer's surface (`PSGFR41`). `PurviewMergePublicSurfaceSeverity` is now
  `error`, so such a leak fails the build.
- The benchmarks build again. They loaded a `Purview.SourceGeneratorFramework.Shared.dll` that no
  framework version ships, and now use the merged generator. Their `net48` target compiles too.
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

[5.0.7-prerelease.3]: https://github.com/purview-dev/telemetry-sourcegenerator/compare/v5.0.7-prerelease.2...v5.0.7-prerelease.3
[5.0.7-prerelease.2]: https://github.com/purview-dev/telemetry-sourcegenerator/compare/v5.0.7-prerelease.1...v5.0.7-prerelease.2
[5.0.7-prerelease.1]: https://github.com/purview-dev/telemetry-sourcegenerator/compare/v5.0.6...v5.0.7-prerelease.1
