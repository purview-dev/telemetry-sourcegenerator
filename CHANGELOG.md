# Changelog

All notable changes to `Purview.Telemetry.SourceGenerator`.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versions come from the
`version` field in `package.json`, which is the single source of truth. This file starts at
5.0.7; earlier releases are described by their [GitHub releases](https://github.com/purview-dev/telemetry-sourcegenerator/releases)
and, for breaking changes, by [Breaking Changes](docs/wiki/Breaking-Changes.md).

## [Unreleased]

.NET 8 and .NET 9 are dropped, C# 15 union types are supported as telemetry values, and the samples build again.

### Added

- **`Activity.AddException` is used when the compilation can resolve it.** The generated exception helper now calls
  `activity.AddException(exception, tags, timestamp)` so the framework produces the canonical `exception` event and
  writes `exception.type`, `exception.message` and `exception.stacktrace` itself; only `exception.escaped` is supplied,
  through the `TagList` the overload accepts, so nothing is duplicated. The probe is on the symbol
  (`Activity.GetMembers("AddException")`), never the target framework, and the hand-built `ActivityEvent` remains as the
  fallback so `net48` and older DiagnosticSource keep working unchanged.
- **`[LogProperties(Transitive = true)]`.** Nested properties are flattened into dotted tags (`weather.Location.City`),
  mirroring Microsoft's `Microsoft.Gen.Logging` generator: public instance properties with a public getter, base types
  walked, indexers/statics/delegates skipped, `[LogPropertyIgnore]` and `SkipNullProperties` honoured at **every** level,
  non-nullable struct hops written with a plain dot, and a 32-type chain bound equal to the .NET generator's
  `MaxTypeTraversalDepth`. A cycle or an over-deep chain stops that branch instead of overflowing the compiler. Two
  documented divergences: a cycle stops one branch rather than abandoning the whole parameter, and a `Nullable<T>`
  property is not expanded further.
- **`[TagProvider]`.** `[TagProvider(typeof(T), "Method")]` on a logging parameter adds the pairs a static provider
  method returns to the log state, prefixed with the parameter name unless `OmitReferenceName` is set. The generator
  reads Microsoft's `Microsoft.Extensions.Logging.TagProviderAttribute` rather than shipping a duplicate, which would
  make every `[TagProvider]` ambiguous (CS0104). Accepts `IEnumerable<KeyValuePair<string, object?>>`, the `string`
  variant, and any type implementing one of them. New rules `TSG2024`–`TSG2027`. A provider parameter makes the method
  state-based even when `V1` was requested, because `LoggerMessage.Define` has no tag bag.
- **Generic methods across all three targets.** A generic logging, activity or metrics method is generated with the
  interface method's type parameters and constraints mirrored (`struct`, `class`, `class?`, `notnull`, `unmanaged`,
  `new()`, type-parameter and type constraints). A generic logging method always uses the state-based path, since
  `LoggerMessage.Define` cannot carry an open generic. `TSG1005` now reports only what genuinely cannot be generated and
  says which: the `allows ref struct` anti-constraint, or a metric measurement that is one of the method's own type
  parameters (an instrument is a field, and a field cannot name a method's type parameter).
- **Generic interfaces.** The generated class becomes generic over the interface's type parameters with the same
  constraints, and the DI extension registers the open generic (`services.AddSingleton(typeof(IFoo<>), typeof(Foo<>))`).
  `TSG1004` now reports only a nested generic interface or the `allows ref struct` anti-constraint.
- **`GeneratorModelBenchmarks`.** Compares the generator's `closed record` tagged models against a value-based
  `[Union]` struct on every benchmark runtime leg. It backs the recorded decision to keep the `closed` records: the
  measurable win is a fraction of a generation pass, and on .NET Framework the struct union is slower. See
  [Platform Support](docs/wiki/Platform-Support.md#considered-and-rejected).
- **`[Meter]` / `[MeterGeneration]` `Version` and `Tags`.** `MeterOptions.Version` and `MeterOptions.Tags` were
  previously reachable only through a `partial` hook the generator declared and never implemented, and `Version` was
  hard-coded `null`. Both are now attribute settings (comma-separated `key=value` tags, interface-level overriding
  assembly-level per key), and without an `IMeterFactory` the version goes to the `Meter(name, version)` constructor.
  `MeterOptions.Scope` remains unreachable: it takes an object instance an attribute cannot express.
- **`[Log(SkipEnabledCheck = true)]`**, and the same property on every level attribute (`[Info]`, `[Error]`, …). The
  generated `IsEnabled` guard is omitted, for callers that have already checked the level — the same contract as
  `[LoggerMessage(SkipEnabledCheck = true)]`. Saves a virtual dispatch per log entry on a hot path.
- **`TSG2022` and `TSG2023`**, parity with the .NET 11 `[LoggerMessage]` generator's `SYSLIB1028`/`SYSLIB1029`. A
  `params` parameter, or a `ref`/`out`/`in`/`scoped`/ref-struct parameter, is reported instead of being silently
  re-declared without its modifier; the method is carried as invalid and generates a throw stub.

- **`[Gauge]` and `HistogramBucketBoundaries`.** A `[Gauge]` method generates the .NET 9 `Gauge<T>` instrument, which
  records a value instead of accumulating one. `[Histogram(HistogramBucketBoundaries = "0.005, 0.01, 0.1")]` passes the
  bucket boundaries to `InstrumentAdvice<T>`. Both are gated on the compilation: `TSG4010` reports a gauge without
  `Gauge<T>`, `TSG4011` a histogram whose boundaries cannot be applied, and `TSG4012` boundaries that do not parse.
- **`Version` and `TelemetrySchemaUrl` on `[assembly: ActivitySourceGeneration(...)]`.** They are passed to
  `System.Diagnostics.ActivitySourceOptions` for a factory-created source, which previously received only the name.
  Setting either without `UseActivitySourceFactory` reports `TSG3024`.
- **C# 15 union types are recorded as their case value.** A parameter whose type carries
  `[System.Runtime.CompilerServices.Union]` — a `union` declaration, or a hand-written union — is unwrapped
  for activity tags and baggage, metric tags, and logging v1 and v2 values instead of being recorded as the
  union struct, which had no meaningful `ToString`. The generator emits one private, aggressively inlined
  accessor per union type. When the union declares the non-boxing access pattern (`bool HasValue` plus a
  `bool TryGetValue(out T)` overload per case) the accessor calls those, so the union struct is never boxed
  to read it; otherwise it reads `Value`. A union that holds no value records `null`.
  See [Union types](docs/wiki/Tags-and-Baggage.md#union-types).
- **A lowest-compatible dependency floor, proved by a build.** `Directory.Packages.props` now keeps one property per
  package family (`DotnetRuntimeVersion` for the runtime-tracked `10.0.<patch>` line, `MSExtensionVersion` for
  dotnet/extensions' `10.<minor>.0`) plus a `DependencyFloor` switch. `-p:DependencyFloor=true` drops both families to
  `10.0.0`, the oldest version the generator's code and tests compile and pass against, and `just test-floor` plus a
  `dependency-floor` PR job prove it. Every declared version is a lower bound, so the floor declared here becomes the
  floor every consumer inherits; `ReleaseGateTests` fails when the recipe or the job is removed.

### Changed

- **`TSG1004` and `TSG1005` no longer reject generics wholesale.** Generic interfaces and generic methods are now
  generated (see Added); both rules report only the shapes that cannot be, and their messages name the shape. This is a
  wording change to a shipped rule, not a new one.
- **`BenchmarkHelpers` and `SampleApp.Net48` follow the compilation-probed `IMeterFactory`.** Both still branched on the
  target framework (`#if NETFRAMEWORK`, and a container that only called `AddLogging`), which the move to a symbol probe
  had invalidated: the `net48` benchmark build no longer compiled, and the `net48` sample built but threw
  `Unable to resolve service for type 'System.Diagnostics.Metrics.IMeterFactory'` on first resolution. The benchmark now
  passes a factory on every leg, and the sample registers one with `services.AddMetrics()` (plus
  `[assembly: MeterGeneration("sample-weather-app-net48")]` so its documented meter name is the one reported).
  `docs/wiki/Metrics.md` and the [support matrix](docs/wiki/Platform-Support.md#support-matrix) said "not available on
  .NET Framework 4.8"; they now describe the probe instead of a framework.
- **The generator's two tagged unions are C# 15 `closed` hierarchies.** An activity method is exactly one of
  `StartActivityMethod`, `ActivityEventMethod` or `ActivityContextMethod`, and an instrument is exactly one of the eight
  `InstrumentAttributeRecord` cases, each carrying its own payload, creation method and instrument type. A method that
  starts an activity can no longer be read for an event's attribute, and emitting an instrument's factory call no longer
  depends on an enum member's name. Generated code is unchanged.
- **`.NET 8` and `.NET 9` are no longer supported.** `TSG1011` now requires `net10.0`+ or `net48`+ and its
  message says so; a `net8.0`/`net9.0` compilation reports it and generates nothing. The integration tests
  target `net10.0;net11.0` (plus `net48` on Windows), and the benchmarks target `net48;net10.0`.
- **Package versions follow the lowest supported framework line again.** `System.Diagnostics.DiagnosticSource`,
  `Microsoft.Extensions.*` and the ASP.NET Core packages are pinned to `10.0.15`, the latest `net10.0`
  servicing patch, rather than the `11.0.0` line: a package reference raises a consumer's floor, and
  `net10.0` is the lowest target this generator supports. `net11.0` consumers still get the `11.0.x`
  packages when they reference them themselves.
- **`Microsoft.SourceLink.GitHub` moves to `11.0.100`** to match the .NET 11 SDK the repository pins.
- **NuGet audits the whole dependency graph.** `NuGetAuditMode=all` replaces the SDK default of `direct`, so a
  vulnerable transitive dependency is reported rather than only a directly declared one. This package ships a
  compiler-host assembly, so a transitive dependency is one it hands to every consumer's build.
- **`.NET 11` opt-ins are the ones `Purview.BuildSdk` 2.0.0 already applies**, not new project properties: Runtime Async
  V2 (`PurviewRuntimeAsync`) and the C# 15 updated memory-safety rules (`MemorySafetyRules=2`) for every `net11.0`
  project. `docs/wiki/Platform-Support.md` records each .NET 11 feature that was evaluated and why it is or is not used.
- **`EventId` on `[Log]` and the level-specific attributes is `int`, not `int?`.** A nullable property
  cannot be set as a named attribute argument, so `[Info(EventId = 42)]` failed with CS0655 and only
  the constructor overloads worked. Omitting it, or passing a negative value, still uses the id derived
  from the method name.

### Fixed

- **Purview.Unions `[GenerateUnion]` unions used as telemetry parameters are unwrapped to their case value.** Union
  recognition only looked for `[System.Runtime.CompilerServices.Union]`, which the unions generator emits into
  *generated* output — output Roslyn never shows another generator — so a `[GenerateUnion]` parameter was recorded as the
  union struct instead of its case value. `UnionHelpers` now also recognises a type carrying
  `[Purview.Unions.GenerateUnion]`, matched by metadata name and namespace so no package reference is taken, and reads
  the case list from the surfaces that are in source, in the unions generator's own order: the attribute's `typeof`
  list, the union's partial static factory signatures, a nested `IUnionMembers` provider, then its source-visible public
  single-parameter constructors. Cases are normalised exactly as that generator exposes them (`int?` → `int`, nullable
  annotation stripped, constructed generics and arrays preserved). The generated accessor always uses the non-boxing
  `HasValue`/`TryGetValue` pattern for a `[GenerateUnion]` target, because that generator emits it for every surface;
  a hand-written `[Union]` type keeps the visible-member detection and a `union` declaration still uses `Value`. A
  `[GenerateUnion]` target with no source-visible case list is recorded as an ordinary value and produces no diagnostic
  of its own — Purview.Unions reports `UNI0003` for it. The end-to-end test running both generators in one compilation
  is a follow-up: Purview.Unions is not on a CI-safe feed yet, so the tests declare a stand-in attribute in source.

The level-specific logging attributes honour their arguments again. Generated log messages and event
ids change for every method that passed a template, event id, name or generation mode to one of them.

- **The Aspire `SampleApp.AppHost` builds and runs again.** `Purview.BuildSdk` 2.0.0 resolves its curated `Current`
  target set (`net10.0;net11.0`) into `TargetFrameworks` for every project that does not declare a target framework in
  its own file, which made the sample projects cross-targeting. An Aspire AppHost cannot cross-target: the outer
  cross-targeting pass imports `Microsoft.NET.Sdk.BeforeCommonCrossTargeting.targets` instead of
  `BeforeCommon.targets`, so `Microsoft.NET.RuntimeIdentifierInference.targets` — the only declaration of the
  `Microsoft.NET.Build.Tasks.PickBestRid` task the AppHost SDK runs during restore — is never imported, and restore
  failed with `MSB4036`. The samples now pin `net10.0` (and clear `TargetFrameworks`) as documented.
- **The generated `[Histogram]` attribute's `HistogramBucketBoundaries` XML comment is well-formed.** It named
  `InstrumentAdvice<T>` unescaped, which `CS1570` rejected in a consumer that generates documentation files.
- **`[Trace]`, `[Debug]`, `[Info]`, `[Warning]`, `[Error]` and `[Critical]` ignored every argument
  but the level** (since 5.0.0). `[Info("Order {OrderId} placed")]` emitted the default
  `"Method: OrderId = {OrderId}"` template, every such method logged with `EventId` 0 rather than the
  id derived from its name, and `Name` and `GenerationMode` were dropped. `[Log]` was unaffected. The
  cause was Purview.SourceGeneratorFramework's nested-model parsing, fixed in 1.3.0, which this release
  builds on. Output now matches 4.x.
- **V2 logging named the `EventId` with `nameof(...)`**, so a custom `Name` failed to compile (CS0103)
  and a prefixed name such as `ITestLogger.Log` lost its prefix. The name is now emitted as a string,
  as V1 does.
- `[Error]` now treats a whitespace-only `MessageTemplate` or `Name` as unset, like the other
  level-specific attributes.

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
