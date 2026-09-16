# Logging

There are two distinct generated types for [`ILogger`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger)-based generation.

To signal an interface for logging generation, decorate it with the `[Logger]` attribute. To signal a method, use `[Log]` (or one of the semantic level attributes). All logging attributes live in the `Purview.Telemetry` namespace.

## Generation v1 vs v2

| | Generation v1 | Generation v2 |
| --- | --- | --- |
| Implementation | [`LoggerMessage`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loggermessage)/[`LoggerMessage.Define`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loggermessagedefine) high-performance logging | State-based output resembling the built-in [`[LoggerMessage]`](https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator) generator |
| Parameter limit | Maximum 6 non-exception parameters plus one optional `Exception` | No fixed parameter limit |
| `[ExpandEnumerable]` | No effect | Supported |
| `[LogProperties]` | No effect | Supported |
| Required reference | `Microsoft.Extensions.Logging` | `Microsoft.Extensions.Telemetry.Abstractions` |

### How `Auto` mode works

The default `LoggerGenerationMode.Auto` selects the mode **per method**:

- **v1** is used when the method is within the v1 limits (≤ 6 non-exception parameters, at most one `Exception`, and no `[ExpandEnumerable]` or `[LogProperties]` parameters).
- **v2** is used when the method needs it (more than 6 parameters, `[ExpandEnumerable]`, or `[LogProperties]`), which requires `Microsoft.Extensions.Telemetry.Abstractions`.

You can force a mode with the `GenerationMode` property:

- `[Log].GenerationMode` — per method
- `[Logger].GenerationMode` — per interface
- `[LoggerGeneration].GenerationMode` — per assembly

```csharp
// Force v2 for one method
[Logger]
interface IOrderServiceTelemetry
{
    [Info(GenerationMode = LoggerGenerationMode.V2)]
    void OrderPlaced(int orderId, string customerName);
}
```

> [!IMPORTANT]
> If you force `V2` (or use a method that requires v2) without referencing `Microsoft.Extensions.Telemetry.Abstractions`, the generated code will not compile because it uses `LoggerMessageHelper` and `LogPropertiesAttribute` from that package.

## Disabling logging generation

Define the `EXCLUDE_PURVIEW_TELEMETRY_LOGGING` constant to ignore the logging attributes entirely:

```xml
<!-- In a .csproj or Directory.Build.props -->
<PropertyGroup>
  <DefineConstants>EXCLUDE_PURVIEW_TELEMETRY_LOGGING</DefineConstants>
</PropertyGroup>
```

This is primarily used when the [`ILogger`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger) type is unavailable — without it your project will fail to compile because the generated attributes reference related types such as [`LogLevel`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loglevel).

## Scoped loggers

Return `IDisposable`/`IDisposable?` from a log method to generate a scoped log entry:

```csharp
[Logger]
interface IOrderServiceTelemetry
{
    [Info]
    IDisposable? ProcessingOrder(Guid orderId);
}
```

When a method returns `IDisposable?`, the entry is emitted when the scope is created and the duration is logged when it is disposed. A scoped method should not specify an explicit level (`TSG2007` warns if it does, since the level is ignored).

## Next pages

- [Generation v1](Logging-Generation-v1.md) — `LoggerMessage.Define` mode and its limits
- [Generation v2](Logging-Generation-v2.md) — state-based mode, `[ExpandEnumerable]`, `[LogProperties]`
- [Breaking Changes](Breaking-Changes.md#namespace-consolidation) — namespace migration
- [Diagnostics](Diagnostics.md) — the `TSG2xxx` Logging rules