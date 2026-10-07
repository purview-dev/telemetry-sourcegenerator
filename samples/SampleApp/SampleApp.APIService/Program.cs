using SampleApp.APIService.Endpoints;
using SampleApp.APIService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(TelemetryNames.MeterNames, TelemetryNames.ActivitySourceNames);

builder.Services.AddScoped<IWeatherService, WeatherService>().AddWeatherServiceTelemetry();

// The shared library registers its own services and telemetry; its generated names already arrived
// in TelemetryNames above, through the project reference.
builder.Services.AddWeatherForecastValidation();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapDefaultEndpoints().MapWeatherAPIv1();

await app.RunAsync();
