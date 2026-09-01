# Design reference

## Interface shape decision matrix

| Situation | Recommendation |
|---|---|
| Small service with < 10 telemetry operations | One combined `I<ClassName>Telemetry` |
| Service only logs | `I<ClassName>Logs` |
| Service only traces | `I<ClassName>Tracing` |
| Service only emits metrics | `I<ClassName>Metrics` |
| Large service with distinct teams owning logs vs. metrics | Split into per-area interfaces |
| Need to mock only one telemetry type in many tests | Split into per-area interfaces |

## Naming examples

### OpenTelemetry mode (default)

```csharp
[ActivitySource] // name = "MyApp"
[Logger]
[Meter]
public interface IOrderServiceTelemetry
{
    [Activity]
    Activity? PlacingOrder(int orderId); // activity name = "placing-order"

    [Info("Placing order {OrderId}")]
    void PlacingOrderInfo(int orderId); // log

    [AutoCounter]
    void OrderPlaced(); // instrument = "myapp.orders.order-placed"
}
```

### Legacy mode

```csharp
[assembly: TelemetryGeneration(NamingConvention = NamingConvention.Legacy)]
```

Use only when exact v3 names must be preserved.

## Multi-target example

```csharp
[ActivitySource]
[Logger]
[Meter]
public interface IOrderServiceTelemetry
{
    // Starts Activity, logs Info, increments Counter
    [Activity(ActivityKind.Internal)]
    [Info("Placing order {OrderId} for {CustomerName}")]
    [AutoCounter]
    Activity? PlacingOrder(int orderId, string customerName, [Baggage] string region);

    // Adds event to the Activity and logs Trace
    [Event]
    [Trace]
    void OrderValidated(Activity? activity, int orderId);

    // Records histogram
    [Histogram]
    void OrderProcessingTime(double latencyMs);
}
```

## Parameter attribute examples

```csharp
[Activity]
[Info]
Activity? FetchingOrder(
    int orderId,
    [Baggage] string region,      // carried in Activity baggage
    [Tag] string tenantId);        // Activity tag only

[Event]
void OrderLoaded(
    Activity? activity,
    int orderId,
    [ExcludeTargets(Targets.Activities)] string[] rawLines); // not in Activity
```

## Testing example

```csharp
public class OrderServiceTests
{
    [Test]
    public void PlaceOrder_EmitsTelemetry()
    {
        var telemetry = Substitute.For<IOrderServiceTelemetry>();
        var service = new OrderService(telemetry);

        service.PlaceOrder(42, "Alice", "EMEA");

        telemetry.Received().PlacingOrder(42, "Alice", "EMEA");
    }
}
```
