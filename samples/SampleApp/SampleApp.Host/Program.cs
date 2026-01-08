using SampleApp.Host.APIs;
using SampleApp.Host.Services;

var builder = WebApplication.CreateBuilder(args);

builder
	.AddServiceDefaults()
	.AddDefaultOpenAPI();

builder.Services.AddScoped<IWeatherService, WeatherService>().AddWeatherServiceTelemetry();

var app = builder.Build();

app.UseHttpsRedirection();

app
	.MapDefaultEndpoints()
	.MapWeatherAPIv1();

app.UseDefaultOpenAPI();

await app.RunAsync();
