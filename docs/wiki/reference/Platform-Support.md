# Platform Support and .NET 11

This page lists the target frameworks and hosting models the generator supports, what the generated code guarantees
under trimming and Native AOT, and what .NET 11 changes. Every "Verified by" entry names a gate that runs on each
release; see [Release Flow](Release-Flow.md#release-gates).

## What's new for .NET 11

- **`ActivitySourceOptions` for factory-created sources.** `[assembly: ActivitySourceGeneration(..., Version = "1.2.3",
  TelemetrySchemaUrl = "https://...")]` passes the source's version and telemetry schema URL to the .NET 11 factory.
  Setting either without `UseActivitySourceFactory` reports `TSG3024`, because a static source has nowhere to put them.
- **`Gauge<T>` and `InstrumentAdvice<T>`.** `[Gauge]` records a value without accumulating it, and
  `HistogramBucketBoundaries = "0.005, 0.01, 0.1"` on `[Histogram]` supplies the histogram's bucket boundaries. Both are
  .NET 9 APIs, gated on the compilation: `TSG4010`/`TSG4011` report a compilation that cannot resolve them, and
  `TSG4012` reports boundaries that do not parse.
- **C# 15 union types as tag, baggage, log and metric-tag values.** A union parameter is recorded as its
  case value, never as the union struct. When the union declares the non-boxing access pattern
  (`bool HasValue` plus a `TryGetValue(out T)` overload per case type) the generated accessor calls those,
  so the union struct is never boxed to read it; a union declared with the `union` keyword falls back to
  its `Value` property, which the language form already holds boxed.
- **Purview.Unions `[GenerateUnion]` types as the same values.** Those members are emitted into *generated* output, so
  this generator cannot see them; it reads the case list from the surfaces that are in source — the attribute's
  `typeof` list, the union's partial static factory signatures, a nested `IUnionMembers` provider, then its
  constructors — and emits the non-boxing accessor against the members the unions generator will produce. No reference
  to Purview.Unions is taken: both attributes are matched by metadata name and namespace. See
  [Tags, Baggage, and Parameter Attributes](Tags-and-Baggage.md#purviewunions-generateunion-types).
- **`ActivitySourceFactory` (opt-in).** `[assembly: ActivitySourceGeneration(..., UseActivitySourceFactory = true)]`
  makes the generated classes create their sources from the injected .NET 11 factory, registered with
  `services.AddTracing()`, instead of a static `ActivitySource`. See
  [Activities](Activities.md#creating-sources-with-activitysourcefactory-net-11). `TSG3023` reports the option when
  the factory type is unavailable.
- **Static interface members are ignored.** C# 15 lets an interface declare non-virtual static members on any runtime.
  The generator and analyzer now skip static members everywhere, so a static helper on a telemetry interface is no
  longer implemented as an instance method or reported as a duplicate name (`TSG1003`).
- **Validated on .NET 11.** The integration tests run on a `net11.0` leg. The packed-consumer gate builds `net11.0`
  Native AOT, Blazor WebAssembly (trimmed and AOT) and Blazor Server consumers of the `.nupkg`.
- **No reference to the logging abstractions is needed on `net11.0`.** .NET 11 ships
  `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Diagnostics.Abstractions` and
  `Microsoft.Extensions.DependencyInjection.Abstractions` in the shared framework, and NuGet reports `NU1510` for an
  explicit reference.
  ([breaking change](https://learn.microsoft.com/en-us/dotnet/core/compatibility/extensions/11/extensions-in-shared-framework))

## Support matrix

### Target frameworks

`.NET 8` and `.NET 9` are no longer supported: `TSG1011` reports a `net8.0`/`net9.0` compilation, and the
test legs and benchmarks no longer build them. The generated code still works on `net48`, so the floor is
`net10.0` on the modern side and `net48` on .NET Framework.

| Consumer target | Activities | Logging v1 | Logging v2 | Metrics | Meters from `IMeterFactory` | `UseActivitySourceFactory` | Verified by |
| --- | --- | --- | --- | --- | --- | --- | --- |
| .NET Framework 4.8 (`net48`+) | Yes | Yes | Yes¹ | Yes | When the compilation resolves `IMeterFactory` (reference Microsoft.Extensions.Diagnostics.Abstractions) | With System.Diagnostics.DiagnosticSource 11+ | Integration tests (`net48` leg, Windows), `SampleApp.Net48` |
| .NET 10 | Yes | Yes | Yes¹ | Yes | Yes | With System.Diagnostics.DiagnosticSource 11+ | Integration tests (`net10.0` leg), `SampleApp`, `SampleApp.MultiProject` |
| .NET 11 | Yes | Yes | Yes¹ | Yes | Yes | Yes | Integration tests (`net11.0` leg), packed-consumer gate |
| `.NET 8`, `.NET 9` | Not supported: reported as [`TSG1011`](Diagnostics.md) (unsupported target framework). Target `net10.0`+ or `net48`+. | | | | | | - |
| `netstandard2.0` and other TFMs | Not supported: reported as [`TSG1011`](Diagnostics.md) (unsupported target framework). Multi-target the library to `net48`/`net10.0`+ instead. | | | | | | - |

The `UseActivitySourceFactory` entries for `net48` and `net10.0` follow from the package's assets and are not run by a gate; only `net11.0` is.

¹ Logging v2 needs `Microsoft.Extensions.Telemetry.Abstractions` (`LoggerMessageHelper`); without it the generator
uses v1. See [Logging Generation v2](Logging-Generation-v2.md).

**Build toolchain:** Visual Studio 2026 (18.x) or the .NET 10 SDK or later (Roslyn 5.9.0+). The generator itself is a
`netstandard2.0` Roslyn component. This repository builds and validates with the .NET 11 SDK pinned in `global.json`.
Declaring a union with the `union` keyword needs C# 15 (the .NET 11 SDK); a hand-written `[Union]` type works on
any compiler that can see its declaration.

### Hosting and render models

| Model | Status | Verified by |
| --- | --- | --- |
| Console / worker / library | Supported | `SampleApp.MultiProject` (`net10.0`), packed-consumer Native AOT console (`net11.0`) |
| ASP.NET Core | Supported | `SampleApp` API service (`net10.0`), packed-consumer Blazor Server host (`net11.0`) |
| .NET Aspire | Supported. Pass `TelemetryNames.ActivitySourceNames`/`MeterNames` to your service defaults. | `SampleApp` AppHost (`net10.0`) |
| Native AOT | Supported, with no trim or AOT warning from the generated code | Packed-consumer gate (a): published with `-warnaserror`, then run, and the emitted activities, metrics and logs asserted |
| Blazor WebAssembly, trimmed | Supported | Packed-consumer gate (b): published trimmed with `-warnaserror`; every generated member must survive ILLink |
| Blazor WebAssembly, AOT (`RunAOTCompilation`) | Supported | Packed-consumer gate (b): every generated method must be AOT-compiled to WebAssembly |
| Blazor Server (interactive server) | Supported | Packed-consumer gate (c): page rendered on a `TestServer`, telemetry asserted |
| .NET Framework 4.8 applications | Supported | `SampleApp.Net48`, `net48` integration tests |

The packed-consumer gate consumes the `.nupkg` exactly as a user does, and each of its applications also references a
class library with its own generated telemetry. On every run it exercises both a factory-created (`UseActivitySourceFactory`)
source and a static one.

## Trim and AOT guarantees

- **No reflection, no dynamic code.** The generated classes call `ActivitySource`, `ILogger` and `Meter` APIs directly,
  with no reflection, `Expression` compilation or runtime code generation. A library built with `IsAotCompatible=true`
  and warnings as errors builds clean, and so does a Native AOT publish with `-warnaserror` (IL2xxx/IL3xxx).
- **Nothing from the compiler host reaches your output.** The generator, its refactorings and the IL-merged
  Purview.SourceGeneratorFramework ship under `analyzers/` only. They never appear in an application's `bin`,
  `deps.json`, publish folder or Blazor `_framework` folder. Pack validation and the compiler-host leak scan enforce this.
- **Dependency injection.** The generated `Add{Interface}()` extension registers the implementation with
  `AddSingleton<TService, TImplementation>()`, which the DI container supports under trimming and Native AOT.
- **Blazor WebAssembly metrics.** On .NET 11 the Blazor WebAssembly SDK turns `MetricsSupport` on by default
  (`BlazorWebAssemblyDiagnosticsEnabled=true`, in `Microsoft.NET.Sdk.BlazorWebAssembly.Current.targets`). If you set
  `BlazorWebAssemblyDiagnosticsEnabled` or `MetricsSupport` to `false`, the trimmer removes metrics support. The
  generated instruments still compile, but record nothing. Earlier SDKs default `MetricsSupport` to `false` for
  WebAssembly, so set it to `true` there.

## .NET 11 features

| Feature | Generator support | Minimum | Source |
| --- | --- | --- | --- |
| C# 15 union types (`union` declarations and hand-written `[Union]` types) as tag, baggage, log and metric-tag values | Implemented: the case value is recorded, read through the non-boxing `TryGetValue`/`HasValue` pattern when the union declares one | `net48`+, any TFM the type resolves on | [Union types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union) |
| Purview.Unions `[GenerateUnion]` types as tag, baggage, log and metric-tag values | Implemented: the case list is read from the source-visible surface (attribute `typeof` list, partial static factories, nested `IUnionMembers` provider, constructors) and the accessor uses the non-boxing pattern the unions generator emits. No package reference is taken; both attributes are matched by metadata name. A target with no visible case list is recorded as an ordinary value and reported by Purview.Unions as `UNI0003`. | `net48`+, any TFM the type resolves on; requires the Purview.Unions generator | [Purview.Unions](https://github.com/purview-dev/unions) |
| `ActivitySourceOptions`, `services.AddTracing()` | Implemented: `UseActivitySourceFactory` with `Version`/`TelemetrySchemaUrl`, diagnostics `TSG3023`/`TSG3024` | `net11.0`, or System.Diagnostics.DiagnosticSource 11 + Microsoft.Extensions.Diagnostics 11 | [What's new in .NET 11 libraries](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#activity-tracing-configuration) |
| `Gauge<T>` (.NET 9) and `InstrumentAdvice<T>` histogram buckets (.NET 9) | Implemented: `[Gauge]`, `[Histogram(HistogramBucketBoundaries = ...)]`, diagnostics `TSG4010`-`TSG4012` | `net48`+ with System.Diagnostics.DiagnosticSource 9 | [Instruments](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation) |
| `ActivitySourceFactory`, `services.AddTracing()` | Implemented: `UseActivitySourceFactory`, diagnostic `TSG3023` | `net11.0`, or System.Diagnostics.DiagnosticSource 11 + Microsoft.Extensions.Diagnostics 11 | [What's new in .NET 11 libraries](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#activity-tracing-configuration) |
| C# 15 non-virtual static interface members on older runtimes | Implemented: static members are skipped | C# 15 (.NET 11 SDK) | [What's new in C# 15](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15) |
| Logging and diagnostics abstractions in the shared framework | Documented: no reference is needed on `net11.0` | `net11.0` | [Breaking change](https://learn.microsoft.com/en-us/dotnet/core/compatibility/extensions/11/extensions-in-shared-framework) |
| `[LoggerMessage]` parameter-shape parity (`SYSLIB1028`/`SYSLIB1029`) | Implemented: `TSG2022` rejects `params`, `TSG2023` rejects `ref`/`out`/`in`/`scoped` and ref struct parameters. Both are carried as invalid and generate a throw stub rather than code that fails to compile. | any TFM | [`[LoggerMessage]` diagnostics](https://github.com/dotnet/runtime/blob/release/11.0/src/libraries/Microsoft.Extensions.Logging.Abstractions/gen/DiagnosticDescriptors.cs) |
| `[LoggerMessage(SkipEnabledCheck = true)]` | Implemented: `[Log(SkipEnabledCheck = true)]` and the same property on every level attribute omit the generated `IsEnabled` guard | any TFM | [`LoggerMessageAttribute.SkipEnabledCheck`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loggermessageattribute.skipenabledcheck) |
| Generic logging methods (the .NET 11 `[LoggerMessage]` generator supports them) | Implemented: a generic method is generated with the interface method's type parameters and constraints mirrored, and always uses the state-based (v2) path because `LoggerMessage.Define` cannot carry an open generic. `TSG1005` now reports only the shapes that cannot be generated. | any TFM | [What's new in .NET 11 libraries](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#generic-source-generated-logging-methods) |
| Generic interfaces | Implemented: the generated class becomes generic over the interface's type parameters with the same constraints, and the DI extension registers the open generic (`typeof(IFoo<>)`). `TSG1004` now reports only a nested generic interface or the `allows ref struct` anti-constraint. | any TFM | [Generic type parameters](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/generic-type-parameters) |
| `[LogProperties(Transitive = true)]` nested property expansion | Implemented: nested properties are flattened to dotted tag names, with `[LogPropertyIgnore]` and `SkipNullProperties` honoured at every level and a 32-type/deepest-chain bound (`Microsoft.Gen.Logging`'s `MaxTypeTraversalDepth`). See [Logging](Logging.md#logpropertiestransitive--true). | any TFM | [`LogPropertiesAttribute.Transitive`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.logpropertiesattribute.transitive) |
| `[TagProvider]` contributed log tags | Implemented for the pair-returning provider shape (`IEnumerable<KeyValuePair<string, object?>>` and the `string` variant), in both the state-based and `LoggerMessage.Define` paths (the latter upgrades the method to the state-based path). Diagnostics `TSG2024`-`TSG2027`. | any TFM | [`TagProviderAttribute`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.tagproviderattribute) |
| `Activity.AddException` | Implemented: when the compilation resolves it, the generated exception helper calls `activity.AddException(exception, tags, timestamp)` and passes `exception.escaped` through the `TagList`; the framework writes `exception.type`/`message`/`stacktrace` itself. Otherwise the hand-built `ActivityEvent` is kept, so `net48` and older DiagnosticSource keep working. | `net48`+ with System.Diagnostics.DiagnosticSource 9+; gated on the symbol, never the TFM | [`Activity.AddException`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity.addexception) |
| `MeterOptions.Version` and `MeterOptions.Tags` | Implemented: `[Meter(Version = "1.2.3", Tags = "env=prod,region=eu")]` or the assembly-level `[MeterGeneration(...)]`, comma-separated `key=value`, interface-level overriding assembly-level per key. Without an `IMeterFactory` the version goes to the `Meter(name, version)` constructor, which is the only metadata that overload carries. `MeterOptions.Scope` stays unreachable: it takes an object instance an attribute cannot express. | Any TFM that resolves `IMeterFactory` (Microsoft.Extensions.Diagnostics.Abstractions), including `net48`; without it the version goes to the `Meter(name, version)` constructor and the tags do not apply | [`MeterOptions`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics.meteroptions) |
| Blazor WebAssembly diagnostics on by default | Documented above | .NET 11 SDK | `Microsoft.NET.Sdk.BlazorWebAssembly.Current.targets` in the .NET 11 SDK |

## .NET 11 in this repository's own build

.NET 11 is GA (SDK 11.0.100, 10 November 2026). The generator targets `netstandard2.0`, so a runtime feature cannot apply
to the generator itself: the entries below are decisions about the projects that *build, test and validate* this package,
and about what the generator emits for a `net11.0` consumer.

`Purview.BuildSdk` 2.0.0 owns the .NET 11 opt-ins, so each is one SDK setting rather than a property copied into projects
(`Sdk/Targets/Modern.targets`, applied only when the target framework's major version is 11 or later).

| Dimension | .NET 11 feature | State here | Why |
| --- | --- | --- | --- |
| async, runtime, debug | Runtime Async V2 (`runtime-async=on`, no `EnablePreviewFeatures` needed on `net11.0`) | On for every `net11.0` project (`PurviewRuntimeAsync=true`, the SDK default) | The runtime libraries themselves ship built with it, so a `net11.0` app built without it still runs against libraries that opted in. Suspension and resumption move out of compiler-generated state machines, which lowers overhead and leaves the real async method on the live call stack, so a profiler, debugger or `new StackTrace()` names the code rather than its infrastructure. `netstandard2.0` (the generator) and `net10.0` are untouched. |
| stability, debug | C# 15 updated memory-safety rules (`updated-memory-safety-rules`) | On for `net11.0` (`MemorySafetyRules=2`, the SDK default); `AllowUnsafeBlocks` deliberately stays off | `unsafe` becomes a caller obligation rather than a property of a member, so an unmanaged-memory access is visible where it happens. Independent of `LangVersion`, so `latest` is enough. The generator emits no pointer code, so nothing here needs it; it is on because the strictest useful combination costs nothing. The preview *syntax* tier (`MemorySafetyRules=1`) is not used: it needs `LangVersion=preview` and this repository has no unsafe code to relax. |
| build | `AnalysisLevel=latest` now resolves to the .NET 11 rule set (the .NET 11 SDK fixed `latest` having meant the .NET 9 rules) | On (SDK default) | New and corrected CA rules apply without a version to bump: CA1873 is quieter and names the reason an argument was flagged, and the CA1515/CA1034/CA1859 false positives with extension members and default interface implementations are gone. |
| build | MSBuild server warm between CLI invocations; Native AOT `dotnet` CLI host | On (SDK default, nothing to configure) | Repeated build and test invocations stop paying the CLI startup path. `Directory.Build.rsp` already pins `-mt` for the parallel build path. |
| testing | Microsoft.Testing.Platform run-level switches (`--timeout`, `--maximum-failed-tests`, `--results-directory-layout per-module`) | On in `just test` and `just test-floor` | A hung test application cannot stall the run, a broken change stops at the first N failures instead of burning the whole suite, and the four legs (`net48`, `net10.0`, `net11.0`, `Repository.UnitTests`) get their own report directories instead of overwriting one another's files. These are MTP switches, not VSTest ones. |
| packaging | NuGet audit over the whole graph (`NuGetAuditMode=all`) | On in `Directory.Packages.props` | The SDK defaults to auditing only directly declared packages. This package ships a compiler-host assembly, so a vulnerable transitive dependency is one it hands to every consumer's build; with `all`, it is reported on every restore instead of being invisible in a graph nobody reads. |
| packaging, stability | Lowest compatible dependency floor (`DependencyFloor`) | Proved by `just test-floor` and the `dependency-floor` PR job | Every declared version is a lower bound, so the floor this package declares becomes the floor every consumer inherits. `Directory.Packages.props` keeps one property per package family (runtime-tracked `10.0.<patch>`, dotnet/extensions `10.<minor>.0`) plus the switch; the ordinary restore takes the latest servicing patch, `-p:DependencyFloor=true` the oldest version the code and tests compile and pass against. |

Everything else in .NET 11 was evaluated and deliberately left alone:

| Evaluated | Not used, because |
| --- | --- |
| Memory-safety preview *syntax* (`MemorySafetyRules=1`) | Needs `LangVersion=preview` and only relaxes pointer contexts. The generator has no unsafe code, and a preview language version in a shipped compiler component is a risk with no payoff. |
| `EnablePreviewFeatures` / preview APIs | No `[RequiresPreviewFeatures]` API is used. Runtime Async is the one .NET 11 feature this repository wants, and on `net11.0` it needs no opt-in flag. |
| JIT and hardware improvements (bounds-check elimination, constant-folded `SequenceEqual`, `Task`/`ValueTask` factory intrinsics, Native AOT interface dispatch, FP16, x86-64-v2 baseline) | Nothing to adopt and nothing to opt into: they are the runtime a `net11.0` consumer gets. The updated minimum hardware is a deployment decision for that consumer, not a generator setting. |
| `System.Text.Json` union serialization and closed-hierarchy polymorphism, `Process.RunAndCaptureTextAsync`, Zstandard, JSON Lines, `Base64` and Rune APIs | Application-facing library APIs. The generator emits `ActivitySource`, `ILogger` and `Meter` calls and serialises nothing. |
| `EnablePackageValidation` / API compatibility baseline | The package ships analyzers only, with no `lib/` surface to compare against a previous release. `PackValidation` in `.config/purview-build.json` is the gate that fits this package's shape. |
| File-based apps (`#:include`, `dotnet run -e`), solution filters (`.slnf`), traversals, affected tests, mobile device selection | Developer-workflow features for applications and for repositories shaped differently from this one. `dotnet watch` Aspire integration is irrelevant: the sample's AppHost is validated by `just build-s` and `just test-s`. |

## Considered and rejected

| Change | Decision and reason |
| --- | --- |
| Replacing the generator's own tagged models (`closed record` `InstrumentAttributeRecord` and `ActivityMethodTarget`) with a value-based `[Union]` struct | **Kept as `closed` records.** The models allocate one object per method per generation pass and dispatch through abstract properties, which a value union would remove — but the measured per-method cost of that mechanism is a few tens of nanoseconds, against a pass that also resolves every symbol, builds the message templates and writes the source text, so the ceiling for any win is a fraction of one percent. The win is not even uniform: on .NET Framework a `readonly struct` union with a tag switch is measurably *slower* than the record hierarchy for the same build-and-dispatch loop, so the change would trade a simpler, exhaustiveness-checked model for a worse .NET Framework build. The exhaustive `switch` over a `closed` hierarchy is also enforced by the compiler, which a tag-plus-payload struct does not give. `benchmarks/Purview.Telemetry.Benchmarks` carries `GeneratorModelBenchmarks`, which compares the two shapes on every runtime leg. The measured comparison is in [Performance § Generator model overhead](Performance.md#generator-model-overhead): the union is 1.8–2.1× faster on .NET 10 and .NET 11 and allocates 25–28% less, but it is 1.5–1.6× *slower* on .NET Framework 4.8 — which is the direction that decided the question. |

## Not yet supported

| Feature | Why it is not supported yet | Source |
| --- | --- | --- |
| `[TagProvider]`'s `ITagCollector` provider shape (`static void M(ITagCollector collector, T value)`) | Reported as `TSG2026`. Implementing `Microsoft.Extensions.Logging.ITagCollector` in generated code would pull `Microsoft.Extensions.Compliance.Classification` into every consumer's build for one optional feature, so only the pair-returning shape (`IEnumerable<KeyValuePair<string, object?>>` / `IEnumerable<KeyValuePair<string, string>>`) is accepted. | [`ITagCollector`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.itagcollector) |
| An end-to-end test with Purview.Unions and this generator in one compilation | The `[GenerateUnion]` support is proven by integration tests that declare a stand-in `Purview.Unions.GenerateUnionAttribute` in the test source, because Purview.Unions is not on a CI-safe package feed yet and the shipped generator must not take a reference to it. Running both generators together is a follow-up once the package is published. The generator reads the case list from source, so nothing in the product depends on the other generator's output. | [Purview.Unions](https://github.com/purview-dev/unions) |
| A `Nullable<T>` (nullable struct) property expanded transitively by `[LogProperties(Transitive = true)]` | Logged as a single tag. Reaching its members needs a `.Value` hop that the generated null-conditional chain cannot express, so the branch stops there rather than emitting code that does not compile. | [`LogPropertiesAttribute.Transitive`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.logpropertiesattribute.transitive) |
| A metric method whose measurement is one of the method's own type parameters | Reported as `TSG1005`, and the method is emitted as a throwing stub. An instrument is a **field**, and a field cannot name a method's type parameter. Declaring that type parameter on the interface makes the generated class generic and the instrument field can then use it; that shape is supported. | [Metrics](Metrics.md) |
| A nested generic interface | Reported as `TSG1004`. The enclosing type's type arguments cannot be repeated on the generated class. | [Generic types](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/) |
| `ActivitySourceOptions` version, tags and `TelemetrySchemaUrl` for factory-created sources | Version and `TelemetrySchemaUrl` are implemented; `Tags` are not, because an attribute cannot express the `IEnumerable<KeyValuePair<string, object?>>` the option takes. Set them in `AddTracing` configuration instead. | [What's new in .NET 11 libraries](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries) |
| `AddTracing` rules from generated registrations | Rules are application configuration, so the generated `Add{Interface}()` does not call `AddTracing()`. Call it yourself. | [Distributed tracing configuration](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-configuration) |
| `ActivityTraceFlags.RandomTraceId` / `Activity.HasRandomizedTraceId` | Nothing to generate: the runtime sets the flag on the activities it creates, and generated code never rewrites trace flags. | [dotnet/runtime#124509](https://github.com/dotnet/runtime/issues/124509) |
| New `System.Diagnostics.Metrics` APIs | `Gauge<T>` and `InstrumentAdvice<T>` are generated (see above). `ObservableGauge<T>` receiving the underlying value instead of `Measurement<T>` is not. | [What's new in .NET 11 libraries](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries) |
| `Activity.AddException` (.NET 9) | Exception events are built with the OpenTelemetry `exception.*` tags directly, which works on every target, `net48` included. | [`Activity.AddException`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity.addexception) |
| A `Microsoft.Extensions.Telemetry.Abstractions` 11 line on `net11.0` | The `11.0.0` line is published, but logging v2 stays on `10.11.0` on every target: this package declares the lowest framework line it supports (`net10.0`), and a package reference raises every consumer's floor to it. A `net11.0` consumer that wants the 11 line references it there itself. | [dotnet/extensions releases](https://github.com/dotnet/extensions/releases) |
