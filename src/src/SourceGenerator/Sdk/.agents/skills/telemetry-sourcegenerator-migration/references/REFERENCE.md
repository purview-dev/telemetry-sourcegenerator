# Migration reference

Detailed before/after examples for migrating hand-written telemetry to Purview telemetry interfaces.

## ILogger to generated logging interface

### Before

```csharp
using Microsoft.Extensions.Logging;

public class OrderService(ILogger<OrderService> logger)
{
    public void PlaceOrder(int orderId, string customerName)
    {
        logger.LogInformation("Placing order {OrderId} for {CustomerName}", orderId, customerName);
    }

    public void CancelOrder(int orderId, Exception ex)
    {
        logger.LogError(ex, "Order {OrderId} cancelled", orderId);
    }
}
```

### After

```csharp
using Purview.Telemetry;

[Logger]
public interface IOrderServiceLogs
{
    [Info("Placing order {OrderId} for {CustomerName}")]
    void OrderPlaced(int orderId, string customerName);

    [Error("Order {OrderId} cancelled")]
    void OrderCancelled(Exception ex, int orderId);
}

public class OrderService(IOrderServiceLogs logger)
{
    public void PlaceOrder(int orderId, string customerName)
    {
        logger.OrderPlaced(orderId, customerName);
    }

    public void CancelOrder(int orderId, Exception ex)
    {
        logger.OrderCancelled(ex, orderId);
    }
}
```

## ActivitySource to generated tracing interface

### Before

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

### After

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

## Metrics to generated metrics interface

### Before

```csharp
using System.Diagnostics.Metrics;

public class OrderService(IMeterFactory meterFactory)
{
    readonly Counter<long> _orders = meterFactory.Create("OrderService").CreateCounter<long>("orders-placed");
    readonly Histogram<double> _latency = meterFactory.Create("OrderService").CreateHistogram<double>("processing-latency-ms");

    public void PlaceOrder(int orderId, double latencyMs)
    {
        _orders.Add(1);
        _latency.Record(latencyMs);
    }
}
```

### After

```csharp
using Purview.Telemetry;

[Meter]
public interface IOrderServiceMetrics
{
    [AutoCounter]
    void OrderPlaced();

    [Histogram]
    void OrderProcessingTime(double latencyMs);
}

public class OrderService(IOrderServiceMetrics metrics)
{
    public void PlaceOrder(int orderId, double latencyMs)
    {
        metrics.OrderPlaced();
        metrics.OrderProcessingTime(latencyMs);
    }
}
```

## Combined telemetry interface

When a class uses multiple telemetry types, prefer the combined refactoring:

```csharp
using System.Diagnostics;
using Purview.Telemetry;

[ActivitySource]
[Logger]
[Meter]
public interface IOrderServiceTelemetry
{
    [Activity]
    [Info]
    [AutoCounter]
    Activity? PlacingOrder(int orderId);

    [Event]
    void OrderShipped(Activity? activity, int orderId);

    [Histogram]
    void OrderProcessingTime(double latencyMs);
}
```

## Common conversions

| Original code | Generated method signature |
|---|---|
| `logger.LogError(ex, "Failed {OrderId}", orderId)` | `[Error("Failed {OrderId}")] void Failed(Exception exception, int orderId);` |
| `activity.AddEvent(new ActivityEvent("Loaded"))` | `[Event] void Loaded(Activity? activity);` |
| `activity.SetBaggage("tenant", tenantId)` | `[Context] void SetTenant(Activity? activity, [Baggage] string tenantId);` |
| `activity.SetTag("tenant", tenantId)` | `[Context] void SetTenant(Activity? activity, [Tag] string tenantId);` |
| `counter.Add(1, new KeyValuePair<string, object?>("region", region))` | `[AutoCounter] void RequestCompleted([Tag] string region);` |
| `histogram.Record(value, new KeyValuePair<string, object?>("region", region))` | `[Histogram] void RequestDuration(long value, [Tag] string region);` |
| `upDownCounter.Add(1)` | `[UpDownCounter] void Increment();` |
