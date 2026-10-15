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
category and severity, validated by the Roslyn release-tracking analyzers on every build. A new or
changed diagnostic must be recorded there in the same change.

## General — `TSG1xxx`

| ID | Severity | Reported when |
| --- | --- | --- |
| `TSG1000` | Error | An unexpected exception escaped the generation pipeline. |
| `TSG1001` | Error | A method on a multi-target interface has no explicit generation attribute — inference is not supported there. |
| `TSG1002` | Error | Two attributes from the same target family on one method. Only one Activity, Logging or Metrics attribute per method. |
| `TSG1003` | Error | Two methods on the interface share a name. |
| `TSG1004` | Error | Generic interfaces are not supported. |
| `TSG1005` | Error | Generic methods are not supported. |
| `TSG1006` | Warning | `ExcludeTargets` names a target not present on the method. |
| `TSG1007` | Warning | `ExcludeTargets` leaves a target with an empty or invalid parameter set. |
| `TSG1008` | Warning | An `Activity`-typed parameter on a method with no `[Activity]`/`[Event]`/`[Context]`. |
| `TSG1010` | Error | A method carries an attribute for a target family the interface does not register. |
| `TSG1011` | Error | The compilation targets neither .NET 8+ nor .NET Framework 4.8+. Define `PURVIEW_TELEMETRY_NON_NULLABLE` to opt out. |

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

### `TSG1003` — duplicate method names

The method name becomes a member on the generated implementation class, so two methods cannot share
one. Rename.

## Related skills

`purview-telemetry-quickstart` to get an interface wired up, `purview-telemetry-design` for
structuring interfaces, and `purview-telemetry-migration` for converting existing `ILogger`,
`ActivitySource` or instrument code.
