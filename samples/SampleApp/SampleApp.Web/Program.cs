using SampleApp.Web.Clients;
using SampleApp.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (OpenTelemetry, health checks, service discovery, resiliency)
builder.AddServiceDefaults(TelemetryNames.MeterNames, TelemetryNames.ActivitySourceNames, false);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Configure typed HttpClient for the API service with resiliency
// ServiceDefaults configures standard resilience handler (retries, circuit breaker, timeout) by default
builder.Services.AddHttpClient<WeatherAPIClient>(client =>
{
	// Use service discovery to resolve the API service URL
	client.BaseAddress = new Uri("https+http://api-service");
});

builder.Services.AddWeatherAPIClientTelemetry();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseHttpsRedirection();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

// Map Aspire health check endpoints
app.MapDefaultEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await app.RunAsync();
