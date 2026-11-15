# Migration from ILogger

This guide covers converting hand-written `ILogger` usage to generated telemetry interfaces. For the fastest path, use the [IDE refactorings](Refactorings.md) — this page documents the manual fallback.

## Before

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

## After

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

Register the interface in DI:

```csharp
services.AddOrderServiceLogs();
```

## Log-level mapping

| `ILogger` call | Generated attribute |
| --- | --- |
| `LogTrace(...)` | `[Trace]` |
| `LogDebug(...)` | `[Debug]` |
| `LogInformation(...)` | `[Info]` |
| `LogWarning(...)` | `[Warning]` |
| `LogError(...)` | `[Error]` |
| `LogCritical(...)` | `[Critical]` |
| `Log(LogLevel.X, ...)` | `[Log(LogLevel.X, ...)]` |

## Scoped logging

Return `IDisposable?` from the method to generate a scoped log entry:

```csharp
[Logger]
public interface IOrderServiceLogs
{
    [Info("Processing order {OrderId}")]
    IDisposable? ProcessingOrder(int orderId);
}
```

## `LoggerMessage.Define`

Replace a `LoggerMessage.Define` static field with a generated method:

```csharp
// Before
static readonly Action<ILogger<OrderService>, int, Exception?> _failed =
    LoggerMessage.Define<int>(LogLevel.Error, 0, "Order {OrderId} failed");

_failed(_logger, orderId, ex);
```

```csharp
// After
[Error("Order {OrderId} failed")]
void OrderFailed(int orderId, Exception ex);

logger.OrderFailed(orderId, ex);
```

## Structured data

For collections, use `[ExpandEnumerable]`; for objects, use `[LogProperties]` — see [Logging Generation v2](Logging-Generation-v2.md).

## Naming

v4+ converts parameter names to snake_case by default (OpenTelemetry convention). To revert to v3 naming, apply `[assembly: TelemetryGeneration(NamingConvention = NamingConvention.Legacy)]`. See [Naming conventions](Generation.md#naming-conventions).

## Checklist

1. Add the `Purview.Telemetry.SourceGenerator` package and `Microsoft.Extensions.Logging.Abstractions`.
2. Create the `[Logger]` interface (or run the [ILogger refactoring](Refactorings.md#convert-ilogger-to-iclassnamelogs)).
3. Replace `ILogger` members and calls with the interface.
4. Register with `services.Add{InterfaceNameWithoutI}()`.
5. Rebuild and review the [Diagnostics](Diagnostics.md).

## Next steps

- [Refactorings](Refactorings.md) — the shipped IDE conversions
- [Logging](Logging.md) — v1/v2 generation modes
- [Multi-Targeting](Multi-Targeting.md) — combine with Activities and Metrics