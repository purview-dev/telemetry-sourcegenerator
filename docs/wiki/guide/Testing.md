# Testing

Telemetry interfaces are plain interfaces, so they are easy to mock or substitute in unit tests. Because the generated implementation only lives at the interface boundary, standard mocking frameworks (NSubstitute, Moq, TUnitMocks, ...) work directly.

## Inject the interface

```csharp
public class OrderService(IOrderServiceTelemetry telemetry)
{
    public void PlaceOrder(int orderId, string customerName)
    {
        using var activity = telemetry.PlacingOrder(orderId, customerName);
        // ...
    }
}
```

## Mock it in tests

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

```csharp
// Moq equivalent
var telemetry = new Mock<IOrderServiceTelemetry>();
var service = new OrderService(telemetry.Object);
service.PlaceOrder(42, "Alice", "EMEA");
telemetry.Verify(x => x.PlacingOrder(42, "Alice", "EMEA"), Times.Once);
```

## Testing real emission

To assert against real telemetry output, set up a listener or collector:

```csharp
using var listener = new ActivityListener
{
    ShouldListenTo = _ => true,
    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
};
ActivitySource.AddActivityListener(listener);

var telemetry = Substitute.For<IOrderServiceTelemetry>();
// Configure the substitute to return a real Activity when the method is called:
telemetry.PlacingOrder(Arg.Any<int>(), Arg.Any<string>())
    .Returns(_ => new Activity("placing-order").Start());
```

For full multi-target scenarios, the [sample application](Sample-Application.md) demonstrates a TUnit + NSubstitute setup.

## Integration tests in this repository

The repository's own test suite lives in `src/tests/SourceGenerator.IntegrationTests` and uses `Purview.SourceGeneratorFramework.Testing.TUnit` (TUnit with the framework's `CodeQuery` syntax-lookup API and assertion extensions). See [Contributing](Contributing.md) for running them.