# Migration from Activities

This guide covers converting hand-written `ActivitySource`/`Activity` code to generated telemetry interfaces. For the fastest path, use the [IDE refactorings](../reference/Refactorings.md) — this page documents the manual fallback.

## Before

```csharp
using System.Diagnostics;

public class OrderService
{
    static readonly ActivitySource _source = new("OrderService");

    public void PlaceOrder(int orderId)
    {
        using var activity = _source.StartActivity("PlaceOrder", ActivityKind.Internal);
        activity?.AddEvent(new ActivityEvent("Validated"));
    }
}
```

## After

```csharp
using System.Diagnostics;
using Purview.Telemetry;

[ActivitySource]
public interface IOrderServiceTracing
{
    [Activity]
    Activity? PlaceOrder(int orderId);

    [Event]
    void Validated(Activity? activity);
}

public class OrderService(IOrderServiceTracing tracing)
{
    public void PlaceOrder(int orderId)
    {
        using var activity = tracing.PlaceOrder(orderId);
        tracing.Validated(activity);
    }
}
```

Register the interface in DI:

```csharp
services.AddOrderServiceTracing();
```

## Common conversions

| Original code | Generated method signature |
| --- | --- |
| `_source.StartActivity("PlaceOrder")` | `[Activity] Activity? PlaceOrder();` |
| `_source.StartActivity("Order", ActivityKind.Internal)` | `[Activity(ActivityKind.Internal)] Activity? Order();` |
| `activity.AddEvent(new ActivityEvent("Loaded"))` | `[Event] void Loaded(Activity? activity);` |
| `activity.SetBaggage("tenant", tenantId)` | `[Context] void SetTenant(Activity? activity, [Baggage] string tenantId);` |
| `activity.SetTag("tenant", tenantId)` | `[Context] void SetTenant(Activity? activity, [Tag] string tenantId);` |

## Return types

- Activity-starting methods should return `Activity?` so callers can dispose and reuse the Activity.
- Event and Context methods should accept the `Activity?` as their first parameter and typically return `void`.

## Tags and baggage

Use `[Tag]` and `[Baggage]` on parameters to control how values are attached — see [Tags, Baggage, and Parameter Attributes](../telemetry/Tags-and-Baggage.md).

## Naming

The ActivitySource name defaults to the assembly name with casing preserved. Set `[ActivitySource("MyApp")]` to override. v4+ converts tag/baggage keys to snake_case by default; revert with `[assembly: TelemetryGeneration(NamingConvention = NamingConvention.Legacy)]`.

## Checklist

1. Add the `Purview.Telemetry.SourceGenerator` package.
2. Create the `[ActivitySource]` interface (or run the [ActivitySource refactoring](../reference/Refactorings.md#convert-activitysource-to-iclassnametracing)).
3. Replace `ActivitySource` fields and `StartActivity`/`AddEvent`/`SetTag`/`SetBaggage` calls with the interface.
4. Register with `services.Add{InterfaceNameWithoutI}()`.
5. Rebuild and review the [Diagnostics](../reference/Diagnostics.md).

## Next steps

- [Refactorings](../reference/Refactorings.md) — the shipped IDE conversions
- [Activities](../telemetry/Activities.md) — full attribute reference
- [Multi-Targeting](../telemetry/Multi-Targeting.md) — combine with Logging and Metrics