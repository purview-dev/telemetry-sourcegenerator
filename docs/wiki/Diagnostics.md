# Diagnostics

The package ships a single Roslyn analyzer, `TelemetryDiagnosticAnalyzer`, which validates telemetry interfaces and raises diagnostics with the `TSG` prefix. All diagnostics are grouped by category.

> [!NOTE]
> Each rule is also tracked for release purposes in `AnalyzerReleases.Shipped.md` and
> `AnalyzerReleases.Unshipped.md`, which carry the rule ID, category, and severity and are validated by
> the Roslyn release-tracking analyzers on every build.

- **General** — `TSG1xxx`
- **Logging** — `TSG2xxx`
- **Activities** — `TSG3xxx`
- **Metrics** — `TSG4xxx`

## General diagnostics (TSG1xxx)

| ID | Severity | Description |
| --- | --- | --- |
| `TSG1000`<a id="tsg1000"></a> | Error | Fatal execution error occurred (`Failed to execute the generation stage: {0}`). Raised by the generator itself when an unexpected exception escapes the pipeline. |
| `TSG1001`<a id="tsg1001"></a> | Error | Inferring generation targets is not supported when using multi-target generation. A method on a multi-target interface has no explicit generation attribute. |
| `TSG1002`<a id="tsg1002"></a> | Error | Multiple attributes from the same target family are not supported. Only one Activity, Logging, or Metrics attribute is allowed per method. |
| `TSG1003`<a id="tsg1003"></a> | Error | Duplicate method names are not supported. Two or more methods on the interface share the same name. |
| `TSG1004`<a id="tsg1004"></a> | Error | Generic interfaces are not supported. |
| `TSG1005`<a id="tsg1005"></a> | Error | Generic methods are not supported. |
| `TSG1006`<a id="tsg1006"></a> | Warning | `ExcludeTargets` references a target not present on this method. |
| `TSG1007`<a id="tsg1007"></a> | Warning | `ExcludeTargets` results in an empty or invalid parameter set for a target. |
| `TSG1008`<a id="tsg1008"></a> | Warning | Activity parameter has no Activity target. A parameter of type `Activity` is present on a method with no `[Activity]`/`[Event]`/`[Context]` attribute. |
| `TSG1010`<a id="tsg1010"></a> | Error | Method target not registered on interface. A method carries an attribute for a target family that is not registered on the interface. |
| `TSG1011`<a id="tsg1011"></a> | Error | Unsupported target framework. The compilation targets neither .NET 8+ nor .NET Framework 4.8+; define `PURVIEW_TELEMETRY_NON_NULLABLE` to opt out. |

## Logging diagnostics (TSG2xxx)

| ID | Severity | Description |
| --- | --- | --- |
| `TSG2000`<a id="tsg2000"></a> | Error | Too many exception parameters. A non-scoped log method has more than one `Exception`-typed parameter. |
| `TSG2001`<a id="tsg2001"></a> | Error | More than 6 parameters. A log method in v1 generation mode has more than 6 non-exception parameters. |
| `TSG2002`<a id="tsg2002"></a> | Info | Inferring error log level. In v1 generation, a single `Exception` parameter is present with no explicit level, so `Error` is inferred. |
| `TSG2003`<a id="tsg2003"></a> | Warning | Could not find a reference to `Microsoft.Extensions.Logging.ILogger`, skipping log generation. |
| `TSG2004`<a id="tsg2004"></a> | Error | Cannot mix ordinal and named property placeholders in a message template. |
| `TSG2005`<a id="tsg2005"></a> | Error | Ordinal values exceed parameter count. The maximum ordinal placeholder value exceeds the number of provided parameters. |
| `TSG2006`<a id="tsg2006"></a> | Error | Using `[LogProperties]` and `[ExpandEnumerable]` on the same parameter is not supported. |
| `TSG2007`<a id="tsg2007"></a> | Warning | A scoped log shouldn't have a `LogLevel`; it will be ignored. |
| `TSG2008`<a id="tsg2008"></a> | Warning | Unbounded enumeration possible. `[ExpandEnumerable]` has a `MaximumValueCount` greater than the recommended default of 5. |
| `TSG2021`<a id="tsg2021"></a> | Error | Log method must return void or IDisposable. (Returning `Activity` is allowed when the method is also an Activity method.) |

## Activities diagnostics (TSG3xxx)

| ID | Severity | Description |
| --- | --- | --- |
| `TSG3000`<a id="tsg3000"></a> | Warning | Baggage parameter types only accept strings (`ToString()` will be called). |
| `TSG3001`<a id="tsg3001"></a> | Warning | No activity source specified. Generation defaults to `purview` when no name is available anywhere. |
| `TSG3002`<a id="tsg3002"></a> | Error | Invalid return type. An Activity/Event method returns something other than `void` or `System.Diagnostics.Activity`. |
| `TSG3003`<a id="tsg3003"></a> | Error | Duplicate reserved parameters defined. More than one parameter maps to the same reserved destination. |
| `TSG3004`<a id="tsg3004"></a> | Error | Activity parameter is not valid. An `Activity`-destination parameter appears on an Activity method (only valid on `[Event]` methods). |
| `TSG3005`<a id="tsg3005"></a> | Error | Timestamp parameter is not valid. A `timestamp` parameter appears on a method that is not an `[Event]` method. |
| `TSG3006`<a id="tsg3006"></a> | Error | Start time parameter is not valid on Create activity or Event method. |
| `TSG3007`<a id="tsg3007"></a> | Error | Parent context or Parent Id parameter is not valid on event. |
| `TSG3008`<a id="tsg3008"></a> | Error | Activity links parameters are not valid on events or context methods. |
| `TSG3009`<a id="tsg3009"></a> | Error | Activity tags parameter is not valid on context methods. |
| `TSG3010`<a id="tsg3010"></a> | Error | Escaped parameters must be a boolean. |
| `TSG3011`<a id="tsg3011"></a> | Error | Escaped parameters are only valid on Events, not Activity or Context methods. |
| `TSG3012`<a id="tsg3012"></a> | Info | There are no Activity methods defined, assumed use of `Activity.Current`. |
| `TSG3013`<a id="tsg3013"></a> | Warning | Should return the created Activity. An Activity method does not return the created `Activity`. |
| `TSG3014`<a id="tsg3014"></a> | Warning | Should accept an Activity to apply the Event/Tags/Baggage to. An Event/Context method has no `Activity` parameter. Opt-in via `ActivitySourceGeneration.GenerateDiagnosticsForMissingActivity` (default `true`). |
| `TSG3015`<a id="tsg3015"></a> | Info | Activity should be the first parameter. Opt-in via `GenerateDiagnosticsForMissingActivity`. |
| `TSG3016`<a id="tsg3016"></a> | Error | Status description parameter should be a string. |
| `TSG3017`<a id="tsg3017"></a> | Error | Status Description parameters are only valid on Events, not Activity or Context methods. |
| `TSG3021`<a id="tsg3021"></a> | Info | Exception event does not use OpenTelemetry standard name. An `[Event]` method records an exception under the OpenTelemetry exception rules but the event name is not the standard `"exception"` (suggest `[Event(Name = "exception")]`). Not raised when `UseRecordExceptionRules` is disabled, for `[Baggage]` exceptions, or when the exception parameter is excluded from the Activities target. |
| `TSG3022`<a id="tsg3022"></a> | Warning | Activity return type should be nullable. An Activity method returns non-nullable `Activity`; use `Activity?` because the Activity can be null when no listeners are active. |

## Metrics diagnostics (TSG4xxx)

| ID | Severity | Description |
| --- | --- | --- |
| `TSG4000`<a id="tsg4000"></a> | Error | No instrument defined. A method on a Metrics interface has no instrument attribute and is not excluded. |
| `TSG4001`<a id="tsg4001"></a> | Error | Must return void or bool. A metrics-owned method returns something other than `void` or `bool`. |
| `TSG4002`<a id="tsg4002"></a> | Error | Auto increment counter and measurement defined. An auto-increment instrument also has a measurement parameter. |
| `TSG4003`<a id="tsg4003"></a> | Error | Multiple measurement values defined. |
| `TSG4004`<a id="tsg4004"></a> | Error | No measurement value defined. A non-auto-increment instrument method has no measurement parameter. |
| `TSG4005`<a id="tsg4005"></a> | Error | Observable instrument requires `Func<T>`. |
| `TSG4006`<a id="tsg4006"></a> | Error | Invalid measurement type. Not one of `byte`, `short`, `int`, `long`, `double`, `float`, `decimal`, `Measurement<T>`, or `IEnumerable<Measurement<T>>`. |
| `TSG4007`<a id="tsg4007"></a> | Error | Observable metrics cannot return bool. |
| `TSG4008`<a id="tsg4008"></a> | Error | AutoCounter must return void. |
| `TSG4009`<a id="tsg4009"></a> | Warning | Instrument name matches the instrument type name. Use a name that describes what is measured. |

## Common resolutions

### TSG1001 — inference disabled on multi-target interfaces

A method on a multi-target interface (`[ActivitySource]` + `[Logger]` + `[Meter]`) has no explicit attribute. Add the attributes for each target the method should emit, or mark it `[Exclude]`.

```csharp
[ActivitySource("MyApp")]
[Logger]
[Meter]
interface IMyTelemetry
{
    [Info]      // ✅ explicit target
    void ProcessItem(int id);
}
```

### TSG1003 — duplicate method names

Two or more methods share the same name, which is used to generate members on the implementation class. Rename the methods.

### TSG2008 — unbounded enumeration

`[ExpandEnumerable]` is configured with a `MaximumValueCount` greater than 5. Reduce it to the recommended default (5) unless you have tested the performance impact.

### TSG3013 / TSG3014 — missing Activity

An Activity method does not return the created `Activity`, or an Event/Context method has no `Activity` parameter. Return the `Activity`/`Activity?` and pass it to Event/Context methods. These best-practice diagnostics are controlled by `ActivitySourceGeneration.GenerateDiagnosticsForMissingActivity`.

### TSG3021 — exception event name

Rename the event with `[Event(Name = "exception")]` so the OpenTelemetry exception tags (`exception.type`, `exception.message`, `exception.stacktrace`, `exception.escaped`) are attached to that event instead of a separate event named `exception`.

```csharp
[Event(Name = "exception")]                       // ✅ the event carries the exception tags
void FailedToRetrieve(Activity? activity, Exception exception);
```

A `Name` set on a *logging* attribute does not name the event — it renames the log entry:

```csharp
[Event]                                           // ❌ event name is the method name
[Error(Name = "exception")]                       // the log entry is named 'exception'
void FailedToRetrieve(Activity? activity, Exception exception);
```

The diagnostic is not raised when the exception is not recorded using the OpenTelemetry exception rules: `[Event(UseRecordExceptionRules = false)]`, a `[Baggage]` exception parameter, or an exception parameter excluded from the Activities target (`[ExcludeTargets(Targets.Activities)]`).

### TSG3022 — non-nullable Activity return

Return `Activity?` so callers can handle the `null` case when no listeners are active.

### TSG4002 — auto-increment counter with a measurement

`[AutoCounter]` (or `[Counter(AutoIncrement = true)]`) must not declare a measurement parameter — the measurement is fixed to 1.

## See also

- [Getting Started](Getting-Started.md)
- [Activities](Activities.md), [Logging](Logging.md), [Metrics](Metrics.md)
- [Multi-Targeting](Multi-Targeting.md)