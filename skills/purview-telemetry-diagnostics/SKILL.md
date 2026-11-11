---
name: purview-telemetry-diagnostics
description: >-
  Use when a Purview.Telemetry.SourceGenerator build reports a TSG diagnostic - TSG1xxx general,
  TSG2xxx logging, TSG3xxx activities, TSG4xxx metrics - to find what triggered it and how to fix it,
  or when deciding whether a rule can be suppressed.
license: MIT
compatibility: C# projects using Purview.Telemetry.SourceGenerator.
metadata:
  author: Purview-Dev
  product: Purview.Telemetry.SourceGenerator
  repository: purview-dev/telemetry-sourcegenerator
---

# Purview.Telemetry.SourceGenerator diagnostics

One Roslyn analyzer, `TelemetryDiagnosticAnalyzer`, validates telemetry interfaces and raises `TSG`
diagnostics in four families:

| Family | Covers |
| --- | --- |
| `TSG1xxx` | general — interface and method shape, targets, framework support |
| `TSG2xxx` | logging |
| `TSG3xxx` | activities |
| `TSG4xxx` | metrics |

Every rule is tracked in `AnalyzerReleases.Shipped.md` / `AnalyzerReleases.Unshipped.md` with its ID,
category and severity, validated by the Roslyn release-tracking analyzers on every build. A new
rule, or a changed category or severity, must be recorded there in the same change. The files do not
carry the message, so a reworded message is recorded in the CHANGELOG instead.

## General — `TSG1xxx`

| ID | Severity | Reported when |
| --- | --- | --- |
| `TSG1000` | Error | An unexpected exception escaped the generation pipeline. |
| `TSG1001` | Error | A method on a multi-target interface has no explicit generation attribute — inference is not supported there. |
| `TSG1002` | Error | Two attributes from the same target family on one method. Only one Activity, Logging or Metrics attribute per method. |
| `TSG1003` | Error | Two methods on the interface share a name. |
| `TSG1004` | Error | A generic interface cannot be generated: only a top-level generic interface whose type parameters the generated class can mirror is supported. A nested generic interface, or a type parameter carrying the `allows ref struct` anti-constraint, is not. |
| `TSG1005` | Error | A generic method cannot be generated: a type parameter carrying `allows ref struct`, or a **metric** measurement that is one of the method's own type parameters (the instrument is a field, and a field cannot name a method's type parameter). The method is emitted as a throwing stub. |
| `TSG1006` | Warning | `ExcludeTargets` names a target not present on the method. |
| `TSG1007` | Warning | `ExcludeTargets` leaves a target with an empty or invalid parameter set. |
| `TSG1008` | Warning | An `Activity`-typed parameter on a method with no `[Activity]`/`[Event]`/`[Context]`. |
| `TSG1010` | Error | A method carries an attribute for a target family the interface does not register. |
| `TSG1011` | Error | The compilation targets neither .NET 10+ nor .NET Framework 4.8+, so nothing is generated. There is no opt-out: target `net10.0`+ or `net48`+, and reference the package only from those targets of a multi-targeted library. |

## Logging — `TSG2xxx`

| ID | Severity | Reported when |
| --- | --- | --- |
| `TSG2000` | Error | A non-scoped log method has more than one `Exception` parameter. |
| `TSG2001` | Error | A v1-mode log method has more than 6 non-exception parameters. |
| `TSG2002` | Info | v1 generation inferred `Error` from a lone `Exception` parameter with no explicit level. |
| `TSG2003` | Warning | No reference to `Microsoft.Extensions.Logging.ILogger`; log generation is skipped. |
| `TSG2004` | Error | Ordinal and named placeholders mixed in one message template. |
| `TSG2005` | Error | The maximum ordinal placeholder exceeds the parameter count. |
| `TSG2006` | Error | `[LogProperties]` and `[ExpandEnumerable]` on the same parameter. |
| `TSG2007` | Warning | A scoped log declares a `LogLevel`; it is ignored. |
| `TSG2008` | Warning | `[ExpandEnumerable]` has a `MaximumValueCount` above the recommended default of 5. |
| `TSG2021` | Error | A log method returns something other than `void` or `IDisposable`. (`Activity` is allowed when the method is also an Activity method.) |
| `TSG2022` | Error | A logging parameter is declared `params`. Take an array and mark it `[ExpandEnumerable]` instead. Parity with `SYSLIB1028`. |
| `TSG2023` | Error | A logging parameter is `ref`/`out`/`in`/`scoped`, or a ref struct such as `Span<T>`; the generated state struct cannot capture it. Parity with `SYSLIB1029`. |
| `TSG2024` | Error | A `[TagProvider]` names a type that cannot be resolved. The parameter is still logged, without the provider's extra tags. |
| `TSG2025` | Error | A `[TagProvider]` names a method that does not exist on the provider type. The parameter is still logged, without the extra tags. |
| `TSG2026` | Error | A `[TagProvider]` method has an unusable signature: it must be a visible static method taking one parameter the annotated parameter can be passed to, and returning `IEnumerable<KeyValuePair<string, object?>>` (or the `string` variant, or a type implementing one). The .NET 11 `ITagCollector` collector shape is deliberately not accepted. |
| `TSG2027` | Warning | `[TagProvider]` is on a parameter of a method that produces no log entry, so the provider would never run. |

## Activities — `TSG3xxx`

| ID | Severity | Reported when |
| --- | --- | --- |
| `TSG3000` | Warning | A baggage parameter is not a string; `ToString()` will be called. |
| `TSG3001` | Warning | No activity source name available anywhere; generation defaults to `purview`. |
| `TSG3002` | Error | An Activity/Event method returns something other than `void` or `Activity`. |
| `TSG3003` | Error | More than one parameter maps to the same reserved destination. |
| `TSG3004` | Error | An `Activity`-destination parameter on an Activity method — only valid on `[Event]`. |
| `TSG3005` | Error | A `timestamp` parameter on a method that is not `[Event]`. |
| `TSG3006` | Error | A start-time parameter on a Create-activity or Event method. |
| `TSG3007` | Error | A parent-context or parent-id parameter on an event. |
| `TSG3008` | Error | Activity-links parameters on an event or context method. |
| `TSG3009` | Error | An activity-tags parameter on a context method. |
| `TSG3010` | Error | An escaped parameter is not a boolean. |
| `TSG3011` | Error | Escaped parameters on an Activity or Context method — Events only. |
| `TSG3012` | Info | No Activity methods defined; `Activity.Current` is assumed. |
| `TSG3013` | Warning | An Activity method does not return the created `Activity`. |
| `TSG3014` | Warning | An Event/Context method has no `Activity` parameter. Controlled by `ActivitySourceGeneration.GenerateDiagnosticsForMissingActivity` (default `true`). |
| `TSG3015` | Info | `Activity` should be the first parameter. Same opt-in as `TSG3014`. |
| `TSG3016` | Error | A status-description parameter is not a string. |
| `TSG3017` | Error | Status-description parameters on an Activity or Context method — Events only. |
| `TSG3021` | Info | An `[Event]` records an exception under the OpenTelemetry rules but is not named `"exception"`. |
| `TSG3022` | Warning | An Activity method returns non-nullable `Activity`. |
| `TSG3023` | Warning | `UseActivitySourceFactory` is set but `System.Diagnostics.ActivitySourceFactory` (.NET 11, or DiagnosticSource 11+) cannot be resolved; a static `ActivitySource` is generated instead. Target `net11.0` or remove the option. |
| `TSG3024` | Warning | `Version` or `TelemetrySchemaUrl` is set on `[assembly: ActivitySourceGeneration(...)]` without `UseActivitySourceFactory`. A static `ActivitySource` has nowhere to put them, so they are ignored; add the factory or remove them. |

## Metrics — `TSG4xxx`

| ID | Severity | Reported when |
| --- | --- | --- |
| `TSG4000` | Error | A method on a Metrics interface has no instrument attribute and is not excluded. |
| `TSG4001` | Error | A metrics-owned method returns something other than `void` or `bool`. |
| `TSG4002` | Error | An auto-increment instrument also declares a measurement parameter. |
| `TSG4003` | Error | More than one measurement value. |
| `TSG4004` | Error | A non-auto-increment instrument method has no measurement parameter. |
| `TSG4005` | Error | An observable instrument is not declared as `Func<T>`. |
| `TSG4006` | Error | The measurement type is not `byte`, `short`, `int`, `long`, `double`, `float`, `decimal`, `Measurement<T>` or `IEnumerable<Measurement<T>>`. |
| `TSG4007` | Error | An observable metric returns `bool`. |
| `TSG4008` | Error | An `AutoCounter` does not return `void`. |
| `TSG4009` | Warning | The instrument name matches the instrument type name. Name what is measured, not the instrument. |
| `TSG4010` | Warning | `[Gauge]` needs `System.Diagnostics.Metrics.Gauge<T>` (.NET 9, or System.Diagnostics.DiagnosticSource 9+). The compilation cannot resolve it, so no instrument is generated and the method throws `NotSupportedException`. |
| `TSG4011` | Warning | `HistogramBucketBoundaries` needs `System.Diagnostics.Metrics.InstrumentAdvice<T>` (.NET 9, or System.Diagnostics.DiagnosticSource 9+). The histogram is generated without the boundaries. |
| `TSG4012` | Warning | `HistogramBucketBoundaries` does not parse as a comma-separated list of ascending, positive numbers (for example `"0.005, 0.01, 0.1, 1"`). No boundaries are generated. |

## The ones people actually hit

### `TSG1001` — inference is off on a multi-target interface

A method on an interface carrying `[ActivitySource]` + `[Logger]` + `[Meter]` has no explicit
attribute. Add one per target the method should emit, or mark it `[Exclude]`:

```csharp
[ActivitySource("MyApp")]
[Logger]
[Meter]
interface IMyTelemetry
{
    [Info]      // explicit target
    void ProcessItem(int id);
}
```

### `TSG3013` / `TSG3014` — the Activity went missing

Return the created `Activity` (as `Activity?`, per `TSG3022` — it is null when no listeners are
active) and pass it into the Event and Context methods that should attach to it. Both are
best-practice rules controlled by `ActivitySourceGeneration.GenerateDiagnosticsForMissingActivity`.

### `TSG3021` — exception event name

Name the event `exception` so the OpenTelemetry exception tags (`exception.type`,
`exception.message`, `exception.stacktrace`, `exception.escaped`) attach to *that* event rather than
a separate one:

```csharp
[Event(Name = "exception")]   // the event carries the exception tags
void FailedToRetrieve(Activity? activity, Exception exception);
```

A `Name` on a **logging** attribute does not name the event — it renames the log entry:

```csharp
[Event]                       // event name is still the method name
[Error(Name = "exception")]   // this renames the log entry
void FailedToRetrieve(Activity? activity, Exception exception);
```

The rule is not raised when the exception is not recorded under the OpenTelemetry rules:
`[Event(UseRecordExceptionRules = false)]`, a `[Baggage]` exception parameter, or an exception
parameter excluded from the Activities target (`[ExcludeTargets(Targets.Activities)]`).

### `TSG2008` — unbounded enumeration

`[ExpandEnumerable]` with `MaximumValueCount` above 5. Bring it back to the default unless you have
measured the cost.

### `TSG4002` — auto-increment counter with a measurement

`[AutoCounter]` — equivalently `[Counter(AutoIncrement = true)]` — must declare no measurement
parameter. The measurement is fixed at 1.

### `TSG1004` — generic interface cannot be generated

A plain generic interface works: the generated class is generic over the same type parameters, with the same
constraints, and the DI extension registers the open generic (`typeof(IFoo<>)`). What does not work is a **nested**
generic interface (the enclosing type's type arguments cannot be repeated on the class) or a type parameter with the
`allows ref struct` anti-constraint (the class cannot repeat it). Move the interface out of its enclosing type, or
drop the anti-constraint.

### `TSG1005` — generic method cannot be generated

Generic methods work, including constraint mirroring (`struct`, `class`, `class?`, `notnull`, `unmanaged`, `new()`,
type-parameter and type constraints) and multiple type parameters. Two shapes are still refused, and the method is
emitted as a throwing stub so the interface stays implemented:

- a type parameter with the `allows ref struct` anti-constraint, which cannot be repeated on the generated
  declaration;
- a **metric** method whose measurement is one of its own type parameters. An instrument is a field, and a field cannot
  name a method's type parameter — declare that type parameter on the interface instead, which makes the class generic
  and lets the instrument field use it.

### `TSG2024` / `TSG2025` / `TSG2026` — `[TagProvider]` cannot be used

`[TagProvider(typeof(T), "M")]` needs a visible static method `M` on `T` that takes one parameter the annotated
parameter can be passed to, and returns `IEnumerable<KeyValuePair<string, object?>>` (the `string`-valued variant, an
array, or a `List<...>` of either also work). `2024` is the type, `2025` the method, `2026` the signature. In every
case the parameter is still logged — only the contributed tags are missing. The .NET 11 collector shape
(`static void M(ITagCollector, T)`) is reported as `TSG2026`: implementing `ITagCollector` in generated code would drag
`Microsoft.Extensions.Compliance.Classification` into the consumer's build.

### `TSG2027` — `[TagProvider]` where nothing logs

The attribute sits on a parameter of a method that produces no log entry — commonly an `[Activity]`-only method on a
multi-target interface. Move it to the logging method.

### `TSG1011` — unsupported target framework

Raised for a compilation that targets neither `net10.0`+ nor `net48`+ (typically `netstandard2.0`, or the dropped `net8.0`/`net9.0`).
It cannot be suppressed by a define; no telemetry is generated for that target. A library that has
to keep such a target references the generator only from its supported ones:

```xml
<PackageReference Include="Purview.Telemetry.SourceGenerator"
                  Condition="'$(TargetFramework)' != 'netstandard2.0'"
                  PrivateAssets="all" IncludeAssets="analyzers" />
```

The telemetry interfaces then need the same condition (or an `#if`), because the attributes are
generated only where the generator runs.

### `TSG1003` — duplicate method names

The method name becomes a member on the generated implementation class, so two methods cannot share
one. Rename.

## No diagnostic, but the log message is wrong

The log reads `Method: Param = {Param}` despite `[Info("…")]`, and the event id is 0. Versions 5.0.0
to 5.1.0 drop every argument of `[Trace]`, `[Debug]`, `[Info]`, `[Warning]`, `[Error]` and
`[Critical]` except the level: template, event id, `Name` and `GenerationMode`. Upgrade, or on those
versions use `[Log(LogLevel.Information, "…")]`, which is unaffected. Do not fall back to
`[LoggerMessage]` or `ILogger.Log*`. To confirm, set `EmitCompilerGeneratedFiles` and read the template
in the generated `*.Logging.g.cs` under `obj/*/generated/`.

## Related skills

`purview-telemetry-quickstart` to get an interface wired up, `purview-telemetry-design` for
structuring interfaces, and `purview-telemetry-migration` for converting existing `ILogger`,
`ActivitySource` or instrument code.
