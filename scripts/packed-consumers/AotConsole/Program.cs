using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PackedTelemetry;
using PackedTelemetry.Library;
using PackedTelemetryAot;
using Purview.Telemetry;

// Only this project opts into the TelemetryNames class, which aggregates the names of every assembly in its reference
// graph - here its own and PackedTelemetry.Library's - from assembly metadata, at compile time. It is generated into
// the namespace named for the assembly, PackedTelemetryAot.
[assembly: TelemetryGeneration(GenerateTelemetryNamesClass = true)]

const string OrderId = "ord-42";
const string CustomerId = "cust-7";
const double Total = 129.5;

using var capture = new TelemetryCapture(Expectations.Names);

ServiceCollection services = new();
services.AddLogging(logging => logging.ClearProviders().SetMinimumLevel(LogLevel.Trace).AddProvider(capture));
services.AddMetrics();
services.AddOrderTelemetry();
services.AddInventory();
services.AddSingleton<OrderWorkflow>();

using (var provider = services.BuildServiceProvider())
{
	provider.GetRequiredService<OrderWorkflow>().Place(OrderId, CustomerId, Total);
}

var failures = Expectations.Verify(capture, OrderId, CustomerId, Total);

foreach (var name in Expectations.Names)
{
	if (Array.IndexOf(TelemetryNames.ActivitySourceNames, name) < 0)
		failures.Add(
			$"TelemetryNames.ActivitySourceNames does not aggregate '{name}' ({string.Join(", ", TelemetryNames.ActivitySourceNames)})"
		);

	if (Array.IndexOf(TelemetryNames.MeterNames, name) < 0)
		failures.Add(
			$"TelemetryNames.MeterNames does not aggregate '{name}' ({string.Join(", ", TelemetryNames.MeterNames)})"
		);
}

Console.WriteLine(Expectations.Dump(capture));
if (failures.Count > 0)
{
	foreach (var failure in failures)
		Console.Error.WriteLine($"MISMATCH: {failure}");

	return 1;
}

Console.WriteLine(
	$"Native AOT: {capture.Activities.Count} activities, {capture.Measurements.Count} measurements and the expected log entries, as generated."
);
return 0;
