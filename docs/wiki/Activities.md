# Activities

All activity-related attributes live in the `Purview.Telemetry` namespace. To signal an interface for Activity generation, decorate it with the `[ActivitySource]` attribute.

> [!IMPORTANT]
> All attributes are now in the unified `Purview.Telemetry` namespace. See [Breaking Changes](Breaking-Changes.md#namespace-consolidation) for migration details.

## ActivitySource naming

In v5 the ActivitySource name defaults to the **assembly name with casing preserved** (OpenTelemetry convention). You can override it at the interface or assembly level:

- Interface: `ActivitySourceAttribute.Name`
- Assembly: `ActivitySourceGenerationAttribute.Name`

> [!NOTE]
> `purview` is only used as a fallback when no assembly name is available, in which case the `TSG3001` diagnostic is generated.

## Activity, Event, or Context

There are three method types:

1. **Activity** methods that generate either a started or un-started [`Activity`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity).
2. **Event** methods that generate an [`ActivityEvent`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activityevent) attached to an Activity.
3. **Context** methods that add either tags or baggage to the current Activity.

> [!TIP]
> Always specify the `Activity` explicitly — otherwise `Activity.Current` is used, which may not be the Activity you expect. When generating an Activity, always return `Activity` or `Activity?`. When using Event and Context methods, always pass in the `Activity` instance returned by an Activity method.

### Activity

Decorate the method with the `[Activity]` attribute to explicitly define an Activity method. Parameters can be passed directly to `ActivitySource.CreateActivity` or `ActivitySource.StartActivity`:

- **tags** — the parameter type must be an [`ActivityTagsCollection`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitytagscollection), `IEnumerable<KeyValuePair<string, object?>>`, or [`TagList`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.taglist).
- **parentContext** — the parameter type must be [`ActivityContext`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitycontext).
- **parentId** — the parameter must be named `parentId` with type `string`.
- **links** — the parameter type must be an `IEnumerable<ActivityLink>`.
- **startTime** — the parameter must be named `startTime` with type `DateTimeOffset`.

Other parameters can be marked with `[Tag]` or `[Baggage]` to specify where they are applied. The return type must be `void`, `Activity`, or `Activity?`; returning the Activity returns the created or started Activity.

### Events

Decorate the method with the `[Event]` attribute to generate a method that creates an `ActivityEvent` and attaches it to the specified `Activity` or `Activity.Current`. To specify an Activity explicitly, the parameter type must be `Activity` or `Activity?`.

The tags collection can be populated with a parameter of type `ActivityTagsCollection`, `IEnumerable<KeyValuePair<string, object?>>`, or `TagList`. To specify the `timestamp`, name the parameter `timestamp` with type `DateTimeOffset`.

The return type must be `void`, `Activity`, or `Activity?`. When returning an Activity, either the one specified as a parameter is used, or `Activity.Current`.

#### Exceptions

When an `Exception` parameter is present, the default behaviour follows the [OpenTelemetry exception rules](https://opentelemetry.io/docs/specs/otel/trace/exceptions/): an event named `exception` is added with the following tags:

- `exception.escaped` — `true` by default; override it by decorating a `bool` parameter with `[Escape]`.
- `exception.message` — the value of `Exception.Message`.
- `exception.stacktrace` — the value of `Exception.StackTrace`.
- `exception.type` — the `Type.FullName` of the exception.

This behaviour can be overridden with the `EventAttribute` options (see below).

### Context

Decorate the method with the `[Context]` attribute to generate a method that populates tags and/or baggage on a specified `Activity` or `Activity.Current`. Parameters are marked with `[Tag]` or `[Baggage]`. The return type must be `void`, `Activity`, or `Activity?`.

## Inferring method type

On a **single-target** Activities interface you can skip the method-level attribute:

- If the method name ends with `Event` (case-sensitive), it is treated as an Event.
- If the first parameter is an `Activity`, it is treated as an Event.
- If the method name ends with `Context` (case-sensitive), it is treated as a Context.
- Anything else defaults to creating an Activity.

> [!NOTE]
> Inference is disabled on multi-target interfaces — see [Multi-Targeting](Multi-Targeting.md).

## Inferring tags or baggage

If you decorate a parameter with `[Tag]` or `[Baggage]`, that stops inference. Any undecorated parameter is treated as a tag or baggage based on the default settings:

- `ActivitySourceAttribute.DefaultToTags` (interface) — `true` means tags, `false` means baggage. Default `true`.
- `ActivitySourceGenerationAttribute.DefaultToTags` (assembly) — same. Default `true`.

See also [Tags, Baggage, and Parameter Attributes](Tags-and-Baggage.md).

## Attribute reference

### `[Activity]`

Defines Activity creation on a method.

| Property | Type | Description |
| --- | --- | --- |
| `Name` | `string?` | The name of the Activity. If not provided, the method name is used. Available on construction. |
| `Kind` | [`ActivityKind`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitykind) | The kind used to create the Activity. Default `Internal`. Available on construction. |
| `CreateOnly` | `bool` | Whether the created Activity is started or not. Default `false`. When `true`, you must return the `Activity`/`Activity?` from the method. |

### `[ActivitySource]`

Defines the creation of the [`ActivitySource`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysource) on an interface.

| Property | Type | Description |
| --- | --- | --- |
| `Name` | `string?` | The ActivitySource name. If not provided, `ActivitySourceGenerationAttribute.Name` is used, then the assembly name. A warning (`TSG3001`) is generated when no custom name is defined anywhere. Available on construction. |
| `DefaultToTags` | `bool` | Whether undecorated parameters are added as tags (`true`) or baggage (`false`). Special-case parameters are matched first. Default `true`. |
| `BaggageAndTagPrefix` | `string?` | Prefix for tag/baggage names. Useful for grouping. Default `null`. |
| `IncludeActivitySourcePrefix` | `bool` | Whether the `ActivitySourceGenerationAttribute.BaggageAndTagSeparator` is used to generate a prefix. Default `true`. |
| `LowercaseBaggageAndTagKeys` | `bool` | Whether tag/baggage names are lower-cased. Default `true`. |

### `[ActivitySourceGeneration]`

Defines ActivitySource defaults at the assembly level.

| Property | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Default ActivitySource name when none is defined on an interface. |
| `DefaultToTags` | `bool` | Whether undecorated parameters are tags (`true`) or baggage (`false`). Default `true`. |
| `BaggageAndTagPrefix` | `string?` | Prefix for tag/baggage names. Default `null`. |
| `BaggageAndTagSeparator` | `string` | Separator used when generating prefixes. Default `.`. |
| `LowercaseBaggageAndTagKeys` | `bool` | Whether tag/baggage names are lower-cased. Default `true`. |
| `GenerateDiagnosticsForMissingActivity` | `bool` | Whether diagnostics (`TSG3014`/`TSG3015`) are raised when Activity parameters or return values are missing. Default `true`. |

### `[Baggage]`

Marks a parameter as baggage on an Activity or Event.

| Property | Type | Description |
| --- | --- | --- |
| `Name` | `string?` | The baggage name. Defaults to `null`, meaning the parameter name is used. |
| `SkipOnNullOrEmpty` | `bool` | Whether the parameter is skipped when it is `null` or default. Default `false`. |

### `[Context]`

Marks a method as adding parameters as tags or baggage to an Activity. There are no properties.

### `[StatusDescription]`

Marks a `string` parameter as the status description for an Activity Event — typically used with events that set an error status code.

```csharp
[ActivitySource("MyApp")]
interface IMyTelemetry
{
    [Event]
    void OperationFailed(
        Activity? activity,
        [StatusDescription]string failureReason
    );
}
```

The parameter must be of type `string`, and this attribute is only valid on Event methods, not Activity or Context methods.

### `[Escape]`

Marks a `bool` parameter as the escape value on an event-based method. See the [OpenTelemetry exception rules](https://opentelemetry.io/docs/specs/otel/trace/exceptions/).

### `[Event]`

Defines a method that creates an `ActivityEvent`.

| Property | Type | Description |
| --- | --- | --- |
| `Name` | `string?` | The name of the event. If not provided, the method name is used. Available on construction. |
| `StatusCode` | [`ActivityStatusCode`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitystatuscode) | Sets the status code on the Activity after the event is added. Default `Unset`. Available on construction. When set to `Error`, the status description is sourced (in order of precedence) from a `[StatusDescription]`-marked parameter, the `StatusDescription` property, the first `Exception` parameter's `Message`, or `null`. |
| `StatusDescription` | `string?` | A static description for the `StatusCode` when set to `Error`. Overridden by a `[StatusDescription]`-marked parameter if present. |
| `UseRecordExceptionRules` | `bool` | Whether the OpenTelemetry exception rules are followed when a parameter is an `Exception`. Default `true`. |
| `RecordExceptionAsEscaped` | `bool` | The value used for `exception.escaped` when `UseRecordExceptionRules` is `true` and an exception is present. Overridable with `[Escape]`. Default `true`. |

## Next steps

- [Tags, Baggage, and Parameter Attributes](Tags-and-Baggage.md) — parameter-level attributes
- [Multi-Targeting](Multi-Targeting.md) — combining Activities with Logging and Metrics
- [Diagnostics](Diagnostics.md) — the `TSG3xxx` Activity rules