# Diagnostics

The package ships a single Roslyn analyzer, `TelemetryDiagnosticAnalyzer`, which validates telemetry interfaces and raises diagnostics with the `TSG` prefix. All diagnostics are grouped by category.

- **General** — `TSG1xxx`
- **Logging** — `TSG2xxx`
- **Activities** — `TSG3xxx`
- **Metrics** — `TSG4xxx`

## General diagnostics (TSG1xxx)

| ID | Severity | Description |
| --- | --- | --- |
| `TSG1000` | Error | Fatal execution error occurred (`Failed to execute the generation stage: {0}`). Raised by the generator itself when an unexpected exception escapes the pipeline. |
| `TSG1001` | Error | Inferring generation targets is not supported when using multi-target generation. A method on a multi-target interface has no explicit generation attribute. |
| `TSG1002` | Error | Multiple attributes from the same target family are not supported. Only one Activity, Logging, or Metrics attribute is allowed per method. |
| `TSG1003` | Error | Duplicate method names are not supported. Two or more methods on the interface share the same name. |
| `TSG1004` | Error | Generic interfaces are not supported. |
| `TSG1005` | Error | Generic methods are not supported. |
| `TSG1006` | Warning | `ExcludeTargets` references a target not present on this method. |
| `TSG1007` | Warning | `ExcludeTargets` results in an empty or invalid parameter set for a target. |
| `TSG1008` | Warning | Activity parameter has no Activity target. A parameter of type `Activity` is present on a method with no `[Activity]`/`[Event]`/`[Context]` attribute. |
| `TSG1010` | Error | Method target not registered on interface. A method carries an attribute for a target family that is not registered on the interface. |
| `TSG1011` | Error | Unsupported target framework. The compilation targets neither .NET 8+ nor .NET Framework 4.8+; define `PURVIEW_TELEMETRY_NON_NULLABLE` to opt out. |

## Logging diagnostics (TSG2xxx)

| ID | Severity | Description |
| --- | --- | --- |
| `TSG2000` | Error | Too many exception parameters. A non-scoped log method has more than one `Exception`-typed parameter. |
| `TSG2001` | Error | More than 6 parameters. A log method in v1 generation mode has more than 6 non-exception parameters. |
| `TSG2002` | Info | Inferring error log level. In v1 generation, a single `Exception` parameter is present with no explicit level, so `Error` is inferred. |
| `TSG2003` | Warning | Could not find a reference to `Microsoft.Extensions.Logging.ILogger`, skipping log generation. |
| `TSG2004` | Error | Cannot mix ordinal and named property placeholders in a message template. |
| `TSG2005` | Error | Ordinal values exceed parameter count. The maximum ordinal placeholder value exceeds the number of provided parameters. |
| `TSG2006` | Error | Using `[LogProperties]` and `[ExpandEnumerable]` on the same parameter is not supported. |
| `TSG2007` | Warning | A scoped log shouldn't have a `LogLevel`; it will be ignored. |
| `TSG2008` | Warning | Unbounded enumeration possible. `[ExpandEnumerable]` has a `MaximumValueCount` greater than the recommended default of 5. |
| `TSG2021` | Error | Log method must return void or IDisposable. (Returning `Activity` is allowed when the method is also an Activity method.) |

## Activities diagnostics (TSG3xxx)

| ID | Severity | Description |
| --- | --- | --- |
| `TSG3000` | Warning | Baggage parameter types only accept strings (`ToString()` will be called). |
| `TSG3001` | Warning | No activity source specified. Generation defaults to `purview` when no name is available anywhere. |
| `TSG3002` | Error | Invalid return type. An Activity/Event method returns something other than `void` or `System.Diagnostics.Activity`. |
| `TSG3003` | Error | Duplicate reserved parameters defined. More than one parameter maps to the same reserved destination. |
| `TSG3004` | Error | Activity parameter is not valid. An `Activity`-destination parameter appears on an Activity method (only valid on `[Event]` methods). |
| `TSG3005` | Error | Timestamp parameter is not valid. A `timestamp` parameter appears on a method that is not an `[Event]` method. |
| `TSG3006` | Error | Start time parameter is not valid on Create activity or Event method. |
| `TSG3007` | Error | Parent context or Parent Id parameter is not valid on event. |
| `TSG3008` | Error | Activity links parameters are not valid on events or context methods. |
| `TSG3009` | Error | Activity tags parameter is not valid on context methods. |
| `TSG3010` | Error | Escaped parameters must be a boolean. |
| `TSG3011` | Error | Escaped parameters are only valid on Events, not Activity or Context methods. |
| `TSG3012` | Info | There are no Activity methods defined, assumed use of `Activity.Current`. |
| `TSG3013` | Warning | Should return the created Activity. An Activity method does not return the created `Activity`. |
| `TSG3014` | Warning | Should accept an Activity to apply the Event/Tags/Baggage to. An Event/Context method has no `Activity` parameter. Opt-in via `ActivitySourceGeneration.GenerateDiagnosticsForMissingActivity` (default `true`). |
| `TSG3015` | Info | Activity should be the first parameter. Opt-in via `GenerateDiagnosticsForMissingActivity`. |
| `TSG3016` | Error | Status description parameter should be a string. |
| `TSG3017` | Error | Status Description parameters are only valid on Events, not Activity or Context methods. |
| `TSG3021` | Info | Exception event does not use OpenTelemetry standard name. An `[Event]` method records an exception but the event name is not the standard `"exception"` (suggest `[Event(Name = "exception")]`). |
| `TSG3022` | Warning | Activity return type should be nullable. An Activity method returns non-nullable `Activity`; use `Activity?` because the Activity can be null when no listeners are active. |

## Metrics diagnostics (TSG4xxx)

| ID | Severity | Description |
| --- | --- | --- |
| `TSG4000` | Error | No instrument defined. A method on a Metrics interface has no instrument attribute and is not excluded. |
| `TSG4001` | Error | Must return void or bool. A metrics-owned method returns something other than `void` or `bool`. |
| `TSG4002` | Error | Auto increment counter and measurement defined. An auto-increment instrument also has a measurement parameter. |
| `TSG4003` | Error | Multiple measurement values defined. |
| `TSG4004` | Error | No measurement value defined. A non-auto-increment instrument method has no measurement parameter. |
| `TSG4005` | Error | Observable instrument requires `Func<T>`. |
| `TSG4006` | Error | Invalid measurement type. Not one of `byte`, `short`, `int`, `long`, `double`, `float`, `decimal`, `Measurement<T>`, or `IEnumerable<Measurement<T>>`. |
| `TSG4007` | Error | Observable metrics cannot return bool. |
| `TSG4008` | Error | AutoCounter must return void. |
| `TSG4009` | Warning | Instrument name matches the instrument type name. Use a name that describes what is measured. |

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

### TSG3022 — non-nullable Activity return

Return `Activity?` so callers can handle the `null` case when no listeners are active.

### TSG4002 — auto-increment counter with a measurement

`[AutoCounter]` (or `[Counter(AutoIncrement = true)]`) must not declare a measurement parameter — the measurement is fixed to 1.

## See also

- [Getting Started](Getting-Started.md)
- [Activities](Activities.md), [Logging](Logging.md), [Metrics](Metrics.md)
- [Multi-Targeting](Multi-Targeting.md)