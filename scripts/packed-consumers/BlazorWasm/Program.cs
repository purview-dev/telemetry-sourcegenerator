using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using PackedTelemetry;
using PackedTelemetry.Library;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The generated registrations: this assembly's AddOrderTelemetry, and the library's (through AddInventory).
builder.Services.AddMetrics();

// .NET 11: the ActivitySourceFactory IOrderTelemetry's generated constructor takes (UseActivitySourceFactory).
builder.Services.AddTracing();
builder.Services.AddOrderTelemetry();
builder.Services.AddInventory();
builder.Services.AddSingleton<OrderWorkflow>();

await builder.Build().RunAsync();
