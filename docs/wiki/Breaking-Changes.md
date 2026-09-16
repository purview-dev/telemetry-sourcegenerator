# Breaking Changes

This page documents breaking changes between major versions to help you migrate your code.

## Table of contents

- [v3 to v4](#v3-to-v4)
  - [Namespace consolidation](#namespace-consolidation)
  - [OpenTelemetry-aligned naming](#opentelemetry-aligned-naming)
- [v4 to v5](#v4-to-v5)
- [v1 and v2 to v3](#v1-and-v2-to-v3)

## v3 to v4

Version 4 introduced two major breaking changes: namespace consolidation and OpenTelemetry-aligned naming conventions.

### Namespace consolidation

**Impact:** High — requires code changes in all projects using v3

v4 consolidates all attributes into a single namespace to simplify imports.

#### Migration required

**Before (v3):**

```csharp
using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;
using Purview.Telemetry.Metrics;
```

**After (v4):**

```csharp
using Purview.Telemetry; // single namespace
```

| v3 namespace | v4 namespace | Affected attributes |
| --- | --- | --- |
| `Purview.Telemetry.Activities` | `Purview.Telemetry` | `[ActivitySource]`, `[Activity]`, `[Event]`, `[Context]`, `[Baggage]` |
| `Purview.Telemetry.Logging` | `Purview.Telemetry` | `[Logger]`, `[Log]`, `[Debug]`, `[Info]`, `[Warning]`, `[Error]`, `[Critical]` |
| `Purview.Telemetry.Metrics` | `Purview.Telemetry` | `[Meter]`, `[Counter]`, `[AutoCounter]`, `[Histogram]`, `[Observable*]`, `[UpDownCounter]` |
| `Purview.Telemetry` | `Purview.Telemetry` | `[Tag]`, `[TelemetryGeneration]`, `[Exclude]` |

#### Migration steps

1. Replace `using Purview.Telemetry.Activities;` / `using Purview.Telemetry.Logging;` / `using Purview.Telemetry.Metrics;` with `using Purview.Telemetry;`.
2. Remove duplicate imports.
3. Rebuild and test.

### OpenTelemetry-aligned naming

**Impact:** Medium to High — changes generated telemetry names (may break dashboards/queries)

**Introduced in:** v4.0.0-alpha.5

**Default behaviour:** Enabled

v4 defaults to **OpenTelemetry semantic conventions** for generated telemetry names. This is a breaking change if you rely on specific telemetry names in dashboards, queries, or monitoring tools.

#### What changed

| Telemetry type | v3 behaviour | v4 default (OpenTelemetry) | Example change |
| --- | --- | --- | --- |
| ActivitySource name | Assembly name lowercased | Assembly name casing preserved | `"myapp"` → `"MyApp"` |
| Activity names | Method name lowercased | Method name casing preserved | `"getentity"` → `"GetEntity"` |
| Tag/baggage keys | Lowercased, smashed compounds | snake_case with underscores | `"entityid"` → `"entity_id"` |
| Metric instrument names | Lowercased, smashed | Hierarchical with meter prefix | `"recordcount"` → `"myapp.products.record.count"` |
| Metric tag keys | Lowercased, smashed | snake_case with underscores | `"requestcount"` → `"request_count"` |

#### Examples

**Before (v3):**

```csharp
[ActivitySource("MyApp")]
interface IOrderTelemetry
{
    [Activity]
    Activity? ProcessingOrder([Baggage]int orderId, [Tag]string customerName);
}

// Generated ActivitySource name: "myapp"
// Generated Activity name: "processingorder"
// Generated tag keys: "orderid", "customername"
```

**After (v4+ OpenTelemetry mode — default):**

```csharp
[ActivitySource("MyApp")]
interface IOrderTelemetry
{
    [Activity]
    Activity? ProcessingOrder([Baggage]int orderId, [Tag]string customerName);
}

// Generated ActivitySource name: "MyApp"
// Generated Activity name: "ProcessingOrder"
// Generated tag keys: "order_id", "customer_name"
```

#### Migration options

**Option 1 — adopt OpenTelemetry naming (recommended):** upgrade and update dashboards/queries to use snake_case keys and hierarchical metric names.

**Option 2 — revert to v3 legacy naming:**

```csharp
using Purview.Telemetry;

// Revert ALL telemetry to v3 naming (assembly-level)
[assembly: TelemetryGeneration(NamingConvention = NamingConvention.Legacy)]

// Or set per-interface
[TelemetryGeneration(NamingConvention = NamingConvention.Legacy)]
interface IMyTelemetry { }
```

```csharp
public enum NamingConvention
{
    Legacy = 0,          // v3 behaviour: lowercase, smashed compounds
    OpenTelemetry = 1    // v4+ default: OTel conventions
}
```

**Option 3 — mixed mode:** apply different conventions per interface with `[TelemetryGeneration]`.

> [!TIP]
> For high-impact scenarios, consider using `NamingConvention.Legacy` initially, then gradually migrating to OpenTelemetry conventions during a planned maintenance window.

## v4 to v5

**Impact:** Low to Medium — affects the default meter name

### Meter default-name resolution

In v4, when `[Meter(Name = ...)]` was not specified, the meter name defaulted to the interface name without the leading `I` (for example, `ICacheServiceTelemetry` → `CacheServiceTelemetry`).

In v5 the default meter name is resolved in this order:

1. `MeterAttribute.Name` (interface)
2. `MeterGenerationAttribute.MeterName` (assembly)
3. The assembly name

If you rely on the old interface-name meter default, set the name explicitly:

```csharp
[Meter("CacheServiceTelemetry")]
interface ICacheServiceTelemetry { }
```

### `MeterGenerationAttribute` additions

v5 adds `MeterName` and `MeterNameGenerationType` to `[MeterGeneration]`, controlling the default meter name and whether instrument names are prefixed with the meter name. See [Metrics](Metrics.md#metergeneration).

### Activity return type should be nullable

The new `TSG3022` warning recommends returning `Activity?` from Activity methods. It is a warning, not an error, but plan to move to nullable Activity return types as the Activity can be `null` when no listeners are active.

## v1 and v2 to v3

### Logging event-name generation

Previously, the default event name generated for a logging method included a trimmed-down version of the class name combined with the method name.

```csharp
[Logger]
interface IServiceTelemetry
{
    void LogAThing(int theThing);
}
```

> [!IMPORTANT]
> In v1 and v2 the default event name for `LogAThing` was `Service.LogAThing`. As of v3, the default is `LogAThing`.

This is supported by changes to `LogPrefixType`:

| Field | Old behaviour | New behaviour |
| --- | --- | --- |
| `Default` | Generated a prefix based on the generated class name. | Generates no suffix. |
| `NoSuffix` | Generated no suffix. | **Field removed.** |
| `TrimmedClassName` | Previously the default behaviour. | New field; generates a suffix based on the generated class name. |

To return to the previous behaviour, set `LoggerGenerationAttribute.DefaultPrefixType` to `LogPrefixType.TrimmedClassName` (assembly level) or `LoggerAttribute.PrefixType` to `LogPrefixType.TrimmedClassName` (interface level).