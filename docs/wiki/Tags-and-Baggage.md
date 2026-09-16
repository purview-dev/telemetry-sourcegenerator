# Tags, Baggage, and Parameter Attributes

> [!IMPORTANT]
> All attributes are now in the unified `Purview.Telemetry` namespace.

Parameters on telemetry methods can be decorated to control how they are emitted. This page covers the parameter-level attributes.

## `[Tag]`

Used within [Activity](Activities.md) and [Metrics](Metrics.md) generation to add a parameter as a tag. Tag names follow the configured `NamingConvention`:

- **OpenTelemetry** (default): `snake_case` for compound words (e.g. `"entity_id"`)
- **Legacy**: lowercased, smashed (e.g. `"entityid"`)

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Name` | `string?` | `null` | Explicitly sets the tag name. When `null`, the parameter name is used (transformed according to the naming convention). |
| `SkipOnNullOrEmpty` | `bool` | `false` | When `true`, the tag is not added if the parameter value is `null` or default. |

### Examples

```csharp
using Purview.Telemetry;

[ActivitySource("OrderService")]
interface IOrderTelemetry
{
    [Activity]
    Activity? ProcessingOrder(
        // Auto-named tag (becomes "order_id" in OpenTelemetry mode)
        [Tag]int orderId,

        // Explicitly named tag (explicit names are not transformed)
        [Tag(Name = "customer.name")]string customerName,

        // Skip if null
        [Tag(SkipOnNullOrEmpty = true)]string? notes
    );
}
```

**OpenTelemetry convention (default):**

```csharp
[Tag]int orderId       // Generated: "order_id"
[Tag]string userName   // Generated: "user_name"
[Tag(Name = "my.custom.tag")]int value  // Generated: "my.custom.tag" (not transformed)
```

**Legacy convention:**

```csharp
[Tag]int orderId       // Generated: "orderid"
[Tag]string userName   // Generated: "username"
```

To change the convention, see [Naming conventions](Generation.md#naming-conventions).

### Best practices

1. **Use explicit names for cross-service tags** to prevent breakage if parameter names change:

   ```csharp
   [Tag(Name = "trace.id")]string traceId
   ```

2. **Use `SkipOnNullOrEmpty` for optional tags** to avoid cluttering telemetry with null values:

   ```csharp
   [Tag(SkipOnNullOrEmpty = true)]string? optionalContext
   ```

3. **Follow OpenTelemetry semantic conventions** for standard names such as `http.method`, `http.status_code`, `service.name`.

## `[Baggage]`

Marks a parameter as baggage on an Activity or ActivityEvent. Baggage propagates across service boundaries, unlike tags.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Name` | `string?` | `null` | Explicitly sets the baggage name. When `null`, the parameter name is used. |
| `SkipOnNullOrEmpty` | `bool` | `false` | When `true`, the parameter is skipped when `null` or default. |

> [!NOTE]
> `[Baggage]` parameters should be `string`; `TSG3000` warns if a non-string is used (`ToString()` is called).

## `[ExcludeTargets]`

Excludes a parameter from specific telemetry targets. See [Multi-Targeting](Multi-Targeting.md) for the `Targets` enum values (`None`, `Activities`, `Logging`, `Metrics`, `All`).

| Property | Type | Description |
| --- | --- | --- |
| `ExcludedTargets` | `Targets` | The targets to exclude the parameter from. Available on construction. |

```csharp
[ExcludeTargets(Targets.Metrics)]
string verboseMessage;  // excluded from metrics only
```

## `[ExpandEnumerable]`

Applied to an array or `IEnumerable` parameter on a log method, it logs the individual elements. See [Logging Generation v2](Logging-Generation-v2.md#expandenumerable).

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `MaximumValueCount` | `int` | `5` | The maximum number of elements to output from the enumeration/array. |

## `[InstrumentMeasurement]`

Marks the parameter used as the instrument measurement value on a metrics method. See [Metrics](Metrics.md#the-measurement-value).

## `[Escape]`

Marks a `bool` parameter as the escape value for an exception event on an Event method. See [Activities](Activities.md#escape).

## `[StatusDescription]`

Marks a `string` parameter as the status description for an Event that sets an error status code. See [Activities](Activities.md#statusdescription).

## See also

- [Activities](Activities.md) — tags/baggage in Activity generation
- [Metrics](Metrics.md) — tags in metrics generation
- [Multi-Targeting](Multi-Targeting.md) — excluding parameters per target
- [Generation](Generation.md) — naming conventions
- [Breaking Changes](Breaking-Changes.md#opentelemetry-aligned-naming) — v3 to v4 naming changes