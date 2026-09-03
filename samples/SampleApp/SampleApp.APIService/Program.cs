using SampleApp.APIService.Endpoints;
using SampleApp.APIService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(TelemetryNames.MeterNames, TelemetryNames.ActivitySourceNames);

builder.Services.AddScoped<IWeatherService, WeatherService>().AddWeatherServiceTelemetry();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapDefaultEndpoints().MapWeatherAPIv1();

await app.RunAsync();
