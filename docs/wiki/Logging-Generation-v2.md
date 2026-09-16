# Logging Generation v2

> [!IMPORTANT]
> All attributes are now in the unified `Purview.Telemetry` namespace. See [Breaking Changes](Breaking-Changes.md#namespace-consolidation) for migration details.

Generation v2 is the state-based logging mode. Its output closely resembles the built-in [`[LoggerMessage]`](https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator) generator, but with additional features: dynamically generated `MessageTemplate`s and the ability to expand array/`IEnumerable` parameters.

It is used automatically in `LoggerGenerationMode.Auto` for methods that exceed the [v1](Logging-Generation-v1.md) limits (more than 6 non-exception parameters, or `[ExpandEnumerable]`/`[LogProperties]` parameters).

> [!IMPORTANT]
> Generation v2 requires the `Microsoft.Extensions.Telemetry.Abstractions` package (`LoggerMessageHelper` and `LogPropertiesAttribute`). Reference it directly or transitively when any method uses v2 generation.

## Selecting v2

```csharp
// Force v2 for the whole interface
[Logger(GenerationMode = LoggerGenerationMode.V2)]
interface IOrderServiceTelemetry { }

// Or per method
[Logger]
interface IOrderServiceTelemetry
{
    [Info(GenerationMode = LoggerGenerationMode.V2)]
    void OrderPlaced(int orderId, string customerName);
}
```

## Log generation

As with v1, decorate a method with `[Log]` (or a semantic level attribute) and return either `void` (non-scoped) or `IDisposable`/`IDisposable?` (scoped).

The shared `[Log]`, `[Logger]`, `[LoggerGeneration]`, and `LogPrefixType` reference tables are on the [Logging Generation v1](Logging-Generation-v1.md#log) page.

## Custom message templates

`[Log].MessageTemplate` lets you customise the log message. Template placeholders map to method parameters:

```csharp
[Logger]
interface IOrderServiceTelemetry
{
    [Info("Order {OrderId} placed for {CustomerName}")]
    void OrderPlaced(int orderId, string customerName);
}
```

If no template is specified, one is generated from the method name and parameters.

## `[ExpandEnumerable]`

Applied to an array or `IEnumerable` parameter, it logs the individual elements rather than the collection as a whole.

| Property | Type | Description |
| --- | --- | --- |
| `MaximumValueCount` | `int` | The maximum number of elements to output. Default `5`. |

```csharp
[Logger]
interface IOrderServiceTelemetry
{
    [Info]
    void OrdersRetrieved(int orderId, [ExpandEnumerable(maximumValueCount: 100)] string[] orderNumbers);
}
```

> [!NOTE]
> A `MaximumValueCount` greater than the recommended default of 5 generates the `TSG2008` warning. It can be ignored, but test your application's performance thoroughly.

## `[LogProperties]`

The external `Microsoft.Extensions.Logging.LogPropertiesAttribute` (from `Microsoft.Extensions.Telemetry.Abstractions`) expands an object's public properties into individual log properties.

| Property | Type | Description |
| --- | --- | --- |
| `OmitReferenceName` | `bool` | Whether the reference name is omitted from property names. Default `false`. |
| `SkipNullProperties` | `bool` | Whether null properties are skipped. Default `false`. |
| `Transitive` | `bool` | Whether nested objects are expanded transitively. Default `false`. |

The companion `LogPropertyIgnoreAttribute` marks a property to be skipped during expansion.

> [!IMPORTANT]
> `[LogProperties]` and `[ExpandEnumerable]` cannot be applied to the same parameter — this raises `TSG2006`.

## Next steps

- [Logging Generation v1](Logging-Generation-v1.md) — the `LoggerMessage.Define` mode and its limits
- [Logging](Logging.md) — choosing between v1 and v2
- [Diagnostics](Diagnostics.md) — the `TSG2xxx` Logging rules