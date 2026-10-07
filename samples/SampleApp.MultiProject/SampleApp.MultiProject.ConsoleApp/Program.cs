using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SampleApp.MultiProject.Observability;
using SampleApp.MultiProject.Ordering;

(string OrderId, decimal Total)[] Orders = [("ord-1001", 49.99m), ("ord-1002", 12.50m), ("ord-1003", 149.00m)];

// TelemetryNames is generated for this project only, and already contains the names from
// every assembly in the reference graph. ServiceDefaults-style projects cannot build this
// list themselves, so the host hands it over.
using var subscription = TelemetryDefaults.Listen(TelemetryNames.ActivitySourceNames, TelemetryNames.MeterNames);

PrintAggregatedNames();

ServiceCollection services = new();
services.AddLogging(logging => logging.AddSimpleConsole(console => console.SingleLine = true));

// Registers IMeterFactory, which the generated metric classes take as a constructor parameter.
services.AddMetrics();

// One call per direct reference: Ordering registers Payments, which registers Notifications.
services.AddOrdering();
services.AddBatchTelemetry();

using var provider = services.BuildServiceProvider();

var orderBook = provider.GetRequiredService<OrderBook>();
var telemetry = provider.GetRequiredService<IBatchTelemetry>();

Console.WriteLine($"Placing {Orders.Length} orders:");
Console.WriteLine();

using (telemetry.RunningBatch(Orders.Length))
{
	foreach (var (orderId, total) in Orders)
	{
		Console.WriteLine($"  {orderId} ({total:C})");
		orderBook.PlaceOrder(orderId, total);
		Console.WriteLine();
	}
}

static void PrintAggregatedNames()
{
	Console.WriteLine("=== Purview Telemetry — multi-project aggregation ===");
	Console.WriteLine();
	Console.WriteLine(
		"Reference graph: Host -> Ordering -> Payments -> Notifications (all four reference Observability)."
	);
	Console.WriteLine("Only Ordering is referenced directly by the host.");
	Console.WriteLine();

	Console.WriteLine($"TelemetryNames.ActivitySourceNames ({TelemetryNames.ActivitySourceNames.Length}):");
	foreach (var name in TelemetryNames.ActivitySourceNames)
		Console.WriteLine($"  - {name}");

	Console.WriteLine();
	Console.WriteLine($"TelemetryNames.MeterNames ({TelemetryNames.MeterNames.Length}):");
	foreach (var name in TelemetryNames.MeterNames)
		Console.WriteLine($"  - {name}");

	Console.WriteLine();
}
