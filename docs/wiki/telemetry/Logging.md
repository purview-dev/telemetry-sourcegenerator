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
| `[TagProvider]` | No effect — the method is generated with v2 instead | Supported |
| A generic method | No effect — the method is generated with v2 instead | Supported |
| Required reference | `Microsoft.Extensions.Logging` | `Microsoft.Extensions.Telemetry.Abstractions` |

### How `Auto` mode works

The default `LoggerGenerationMode.Auto` selects the mode **per method**:

- **v1** is used when the method is within the v1 limits (≤ 6 non-exception parameters, at most one `Exception`, and no `[ExpandEnumerable]`, `[LogProperties]` or `[TagProvider]` parameters, and the method is not generic).
- **v2** is used when the method needs it (more than 6 parameters, `[ExpandEnumerable]`, `[LogProperties]`, `[TagProvider]`, or a generic method), which requires `Microsoft.Extensions.Telemetry.Abstractions`.

A `[TagProvider]` parameter is the one case that overrides an explicit `V1`: `LoggerMessage.Define` carries a fixed
parameter list and no tag bag, so the method is generated with the state-based path rather than silently dropping the
provider's tags. A generic method always uses the state-based path for the same reason — `LoggerMessage.Define` cannot
carry an open generic.

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

## `[LogProperties(Transitive = true)]`

`[LogProperties]` flattens the parameter's readable properties into individual tags. With `OmitReferenceName` off, each
tag is named `parameter.Property`; with it on, just `Property`.

`Transitive = true` also flattens the properties of any property whose type is itself a complex object, producing
dotted names:

```csharp
public class WeatherForecast
{
    public Location Location { get; set; }
}

public class Location
{
    public string City { get; set; }
}

[Logger]
interface IWeatherTelemetry
{
    void LogWeather([LogProperties(Transitive = true)] WeatherForecast weather);
    // tags: weather.Location.City, ...
}
```

The walk mirrors Microsoft's `Microsoft.Gen.Logging` generator:

- public instance properties with a public getter, walking base types, with a property hidden by a derived type logged
  once;
- indexers, static properties and delegate-typed properties are skipped;
- `[LogPropertyIgnore]` is honoured at **every** level;
- `SkipNullProperties` is applied at **every** level;
- a non-nullable struct property is reached with a plain dot (`weather?.Location.City`); a nullable reference-typed
  property adds a conditional access (`weather?.Location?.City`);
- primitives, strings, enums, collections and the well-known value-like types (`DateTime`, `DateTimeOffset`, `DateOnly`,
  `TimeOnly`, `TimeSpan`, `Guid`, `Uri`, `Version`) stay a single tag rendered with `ToString`.

**Depth and cycles.** A property chain may reach 32 types, the same `MaxTypeTraversalDepth` the .NET 11 generator uses.
A type already on the chain (a self-reference, or two types that reference each other) and a chain that would exceed
the limit **stop flattening that branch**: the tags found so far are kept and the repeated type is not walked again.
The compiler cannot be made to overflow, and the parameter is still logged.

Two deliberate divergences from the .NET 11 generator, both to keep the emitted code compiling:

- Microsoft's generator abandons the *whole* `[LogProperties]` expansion when it sees a cycle; this generator stops only
  the branch, so the parent's other properties are still logged.
- A `Nullable<T>` property (a nullable *struct*) is flattened no further. Reaching its members needs a `.Value` hop that
  the generated null-conditional chain cannot express, so it is logged as one tag.

## `[TagProvider]` contributed tags

`[TagProvider(typeof(T), "Method")]` on a logging parameter names a static method that contributes extra tags at run
time. The attribute is Microsoft's `Microsoft.Extensions.Logging.TagProviderAttribute`, from
`Microsoft.Extensions.Telemetry.Abstractions` — the generator reads that type rather than shipping a duplicate, because a
second `Purview.Telemetry.TagProviderAttribute` would make every `[TagProvider]` ambiguous (CS0104) in a project that
imports both namespaces for `ILogger` and `[Logger]`.

```csharp
public static class WeatherTagProvider
{
    public static IEnumerable<KeyValuePair<string, object?>> GetTags(WeatherForecast weather) =>
        new[] { new KeyValuePair<string, object?>("Summary", weather.Summary) };
}

[Logger]
interface IWeatherTelemetry
{
    void LogWeather([TagProvider(typeof(WeatherTagProvider), nameof(WeatherTagProvider.GetTags))] WeatherForecast weather);
    // tags: weather.Summary (the parameter name prefixes each contributed key)
}
```

- The provider method must be a visible static, non-generic method taking one parameter the annotated parameter can be
  passed to, and returning `IEnumerable<KeyValuePair<string, object?>>` — the `string`-valued variant, and any type
  implementing one of them (an array or `List<...>`), are accepted too. Anything else is `TSG2026`.
- `OmitReferenceName = true` leaves the contributed keys unprefixed.
- A null parameter contributes nothing (the call is guarded).
- **Divergence:** the .NET 11 collector shape (`static void GetTags(ITagCollector collector, T value)`) is not accepted
  and is reported as `TSG2026`. Implementing `ITagCollector` in generated code would pull
  `Microsoft.Extensions.Compliance.Classification` into every consumer's build, so only the pair-returning shape is
  supported. This is recorded in [Platform Support](Platform-Support.md).
- A `[TagProvider]` parameter makes the method state-based (v2), even if `V1` was requested, because
  `LoggerMessage.Define` has no tag bag to add the pairs to.

## Next pages

- [Generation v1](Logging-Generation-v1.md) — `LoggerMessage.Define` mode and its limits
- [Generation v2](Logging-Generation-v2.md) — state-based mode, `[ExpandEnumerable]`, `[LogProperties]`
- [Breaking Changes](Breaking-Changes.md#namespace-consolidation) — namespace migration
- [Diagnostics](Diagnostics.md) — the `TSG2xxx` Logging rules