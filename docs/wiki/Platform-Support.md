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
  its `Value` property, which the language form already holds boxed. See
  [Tags, Baggage, and Parameter Attributes](Tags-and-Baggage.md#union-types).
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
| .NET Framework 4.8 (`net48`+) | Yes | Yes | Yes¹ | Yes | No (`new Meter(...)`) | With System.Diagnostics.DiagnosticSource 11+ | Integration tests (`net48` leg, Windows), `SampleApp.Net48` |
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
| `ActivitySourceOptions`, `services.AddTracing()` | Implemented: `UseActivitySourceFactory` with `Version`/`TelemetrySchemaUrl`, diagnostics `TSG3023`/`TSG3024` | `net11.0`, or System.Diagnostics.DiagnosticSource 11 + Microsoft.Extensions.Diagnostics 11 | [What's new in .NET 11 libraries](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#activity-tracing-configuration) |
| `Gauge<T>` (.NET 9) and `InstrumentAdvice<T>` histogram buckets (.NET 9) | Implemented: `[Gauge]`, `[Histogram(HistogramBucketBoundaries = ...)]`, diagnostics `TSG4010`-`TSG4012` | `net48`+ with System.Diagnostics.DiagnosticSource 9 | [Instruments](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics-instrumentation) |
| `ActivitySourceFactory`, `services.AddTracing()` | Implemented: `UseActivitySourceFactory`, diagnostic `TSG3023` | `net11.0`, or System.Diagnostics.DiagnosticSource 11 + Microsoft.Extensions.Diagnostics 11 | [What's new in .NET 11 libraries](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#activity-tracing-configuration) |
| C# 15 non-virtual static interface members on older runtimes | Implemented: static members are skipped | C# 15 (.NET 11 SDK) | [What's new in C# 15](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15) |
| Logging and diagnostics abstractions in the shared framework | Documented: no reference is needed on `net11.0` | `net11.0` | [Breaking change](https://learn.microsoft.com/en-us/dotnet/core/compatibility/extensions/11/extensions-in-shared-framework) |
| Blazor WebAssembly diagnostics on by default | Documented above | .NET 11 SDK | `Microsoft.NET.Sdk.BlazorWebAssembly.Current.targets` in the .NET 11 SDK |

## Not yet supported

| Feature | Why it is not supported yet | Source |
| --- | --- | --- |
| Generic logging methods (the .NET 11 `[LoggerMessage]` generator supports them) | Generic interface methods still report `TSG1005`. Supporting them needs generic implementations across all three targets, not only logging. | [What's new in .NET 11 libraries](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries#generic-source-generated-logging-methods) |
| Parity with the new `[LoggerMessage]` diagnostics for `params`/`scoped` and `ref struct` parameters (SYSLIB1028, SYSLIB1029) | No equivalent `TSG` rule exists yet. Such parameters are not rejected up front. | [`DiagnosticDescriptors.cs` (release/11.0)](https://github.com/dotnet/runtime/blob/release/11.0/src/libraries/Microsoft.Extensions.Logging.Abstractions/gen/DiagnosticDescriptors.cs) |
| `ActivitySourceOptions` version, tags and `TelemetrySchemaUrl` for factory-created sources | Version and `TelemetrySchemaUrl` are implemented; `Tags` are not, because an attribute cannot express the `IEnumerable<KeyValuePair<string, object?>>` the option takes. Set them in `AddTracing` configuration instead. | [What's new in .NET 11 libraries](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries) |
| `AddTracing` rules from generated registrations | Rules are application configuration, so the generated `Add{Interface}()` does not call `AddTracing()`. Call it yourself. | [Distributed tracing configuration](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-configuration) |
| `ActivityTraceFlags.RandomTraceId` / `Activity.HasRandomizedTraceId` | Nothing to generate: the runtime sets the flag on the activities it creates, and generated code never rewrites trace flags. | [dotnet/runtime#124509](https://github.com/dotnet/runtime/issues/124509) |
| New `System.Diagnostics.Metrics` APIs | `Gauge<T>` and `InstrumentAdvice<T>` are generated (see above). `ObservableGauge<T>` receiving the underlying value instead of `Measurement<T>` is not. | [What's new in .NET 11 libraries](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries) |
| `Activity.AddException` (.NET 9) | Exception events are built with the OpenTelemetry `exception.*` tags directly, which works on every target, `net48` included. | [`Activity.AddException`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity.addexception) |
| A `Microsoft.Extensions.Telemetry.Abstractions` 11 line | None is published yet. Logging v2 uses the 10.x package on every target. | [dotnet/extensions releases](https://github.com/dotnet/extensions/releases) |
