# Logging Generation v1

> [!IMPORTANT]
> All attributes are now in the unified `Purview.Telemetry` namespace. See [Breaking Changes](Breaking-Changes.md#namespace-consolidation) for migration details.

Generation v1 is the previous style of log generation, built on [`LoggerMessage.Define`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loggermessagedefine) from the [high-performance logging](https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging) libraries.

It has these limitations:

- A maximum of **6** non-exception parameters.
- At most **one** `Exception` parameter.
- No support for expanding enumerations (`[ExpandEnumerable]` has no effect).
- No support for `[LogProperties]`.

> [!WARNING]
> The maximum number of parameters allowed by the `LoggerMessage` class is **6**, plus one optional `Exception`. You must reference the `Microsoft.Extensions.Logging` package.

## Selecting v1

v1 is used automatically in `LoggerGenerationMode.Auto` for any method that is within the v1 limits. You can force it for all methods on an interface or assembly:

```csharp
[Logger(GenerationMode = LoggerGenerationMode.V1)]
interface IOrderServiceTelemetry { }
```

## Log generation

To generate a log entry, decorate the method with `[Log]` and return either `void` (non-scoped) or `IDisposable`/`IDisposable?` (scoped). Parameters form part of the structured log generation and are referenced by the `MessageTemplate`.

You can also use the semantic level attributes instead of `[Log].Level`:

- `[Trace]`, `[Debug]`, `[Info]`, `[Warning]`, `[Error]`, `[Critical]`

These support all the same properties as `[Log]`, except `Level`.

## Inferring

When not using multi-targeting you can omit `[Log]` entirely — every method on a `[Logger]` interface becomes a log method. Set the default level with `[Logger].DefaultLevel` (interface) or `[LoggerGeneration].DefaultLevel` (assembly).

If no level is specified on the method and an `Exception` parameter is present, the level is changed to `Error` automatically. This raises the `TSG2002` diagnostic, which can safely be ignored if you are comfortable with the inferred behaviour.

## Attribute reference

### `[Log]`

| Property | Type | Description |
| --- | --- | --- |
| `Level` | [`LogLevel`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loglevel) | The level used when defining the log entry. Available on construction. Defaults to `Information`, unless an `Exception` is detected in the parameters, then `Error`. |
| `MessageTemplate` | `string?` | The template used to populate the log entry. If not specified, one is generated from the prefixes and available parameters. Available on construction. Default `null`. |
| `EventId` | `int?` | Used when generating the [`EventId`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.eventid). Available on construction. Default `null` (one is generated if not supplied). |
| `Name` | `string?` | The name of the log entry. If not defined, the method name is used. Available on construction. Default `null`. |
| `GenerationMode` | `LoggerGenerationMode` | Per-method generation-mode override. `Auto` (default) inherits from the interface/assembly level. |

### `[Logger]`

| Property | Type | Description |
| --- | --- | --- |
| `DefaultLevel` | `LogLevel` | The default level when one is not provided. Available on construction. Default `Information`. |
| `CustomPrefix` | `string?` | Used when generating the log entry name's prefix. When set, `PrefixType` is automatically `Custom`. Available on construction. Default `null`. |
| `PrefixType` | `LogPrefixType` | The prefix type used when generating the log entry name. Default `Default`. |
| `GenerationMode` | `LoggerGenerationMode` | Controls the generation mode for all log methods on the interface. `Auto` (default) selects the best mode per method. |

### `[LoggerGeneration]`

| Property | Type | Description |
| --- | --- | --- |
| `DefaultLevel` | `LogLevel` | The default level when one is not provided. Available on construction. Default `Information`. |
| `GenerationMode` | `LoggerGenerationMode` | Controls the generation mode for all log methods in the assembly. `Auto` (default) selects the best mode per method. |
| `DefaultPrefixType` | `LogPrefixType` | The default prefix type for all log entries in the assembly. Default `Default`. |

### `LogPrefixType`

| Value | Description |
| --- | --- |
| `Default` | No prefix. |
| `Interface` | Uses the name of the interface. |
| `Class` | The name of the class used for generation (`TelemetryGenerationAttribute.ClassName` or auto-generated). |
| `Custom` | Used when `LoggerAttribute.CustomPrefix` is set. |
| `TrimmedClassName` | The interface name without the `I` prefix or `Log`, `Logger`, or `Telemetry` suffixes. |

## Next steps

- [Logging Generation v2](Logging-Generation-v2.md) — the state-based mode with `[ExpandEnumerable]`/`[LogProperties]`
- [Logging](Logging.md) — choosing between v1 and v2
- [Diagnostics](Diagnostics.md) — the `TSG2xxx` Logging rules