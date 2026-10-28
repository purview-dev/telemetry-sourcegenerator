using Microsoft.AspNetCore.TestHost;
using PackedTelemetry;
using PackedTelemetry.Components;
using PackedTelemetry.Components.Pages;
using PackedTelemetry.Library;

const string OrderId = Orders.OrderId;
const string CustomerId = Orders.CustomerId;
const double Total = Orders.Total;

var selfTest = args.Contains("--self-test");

using var capture = new TelemetryCapture(Expectations.Names);

var builder = WebApplication.CreateBuilder(args);
if (selfTest)
	builder.WebHost.UseTestServer();

// Trace minimum, so the logging infrastructure filters nothing: the capture decides what is enabled.
builder.Logging.ClearProviders().SetMinimumLevel(LogLevel.Trace).AddProvider(capture);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// .NET 11: the ActivitySourceFactory IOrderTelemetry's generated constructor takes (UseActivitySourceFactory).
builder.Services.AddTracing();
builder.Services.AddOrderTelemetry();
builder.Services.AddInventory();
builder.Services.AddSingleton<OrderWorkflow>();

var app = builder.Build();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

if (!selfTest)
{
	await app.RunAsync();
	return 0;
}

await app.StartAsync();
List<string> failures = [];
try
{
	using var client = app.GetTestClient();
	using var response = await client.GetAsync("/");
	var html = await response.Content.ReadAsStringAsync();

	if (!response.IsSuccessStatusCode)
		failures.Add($"GET / returned {(int)response.StatusCode}: {html}");
	else if (!html.Contains($"Placed {OrderId}.", StringComparison.Ordinal))
		failures.Add($"the page did not render the placed order: {html}");
	else if (!html.Contains("_framework/blazor.web.js", StringComparison.Ordinal))
		failures.Add("the page is not an interactive Blazor page (no blazor.web.js)");
	else
		Console.WriteLine($"GET / rendered 'Placed {OrderId}.' on the server");
}
finally
{
	await app.StopAsync();
}

failures.AddRange(Expectations.Verify(capture, OrderId, CustomerId, Total));

Console.WriteLine(Expectations.Dump(capture));
if (failures.Count > 0)
{
	foreach (var failure in failures)
		Console.Error.WriteLine($"MISMATCH: {failure}");

	return 1;
}

Console.WriteLine("Blazor Server: the page rendered and emitted the generated activities, metrics and log entries.");
return 0;
