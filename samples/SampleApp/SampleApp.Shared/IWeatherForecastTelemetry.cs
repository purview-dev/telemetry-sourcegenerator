using System.Diagnostics;
using Purview.Telemetry;

namespace SampleApp;

/// <summary>
/// Telemetry owned by the shared library, which has no idea how the host registers telemetry. Both
/// names default to this assembly's name, <c>SampleApp.Shared</c>, so they are easy to spot in the
/// hosts' aggregated <c>TelemetryNames</c> — see the <c>[TelemetryGeneration]</c> in the API service
/// and web projects.
/// </summary>
/// <remarks>
/// <see cref="WeatherForecastValidator"/> consumes this; the names reach the hosts entirely through
/// assembly metadata.
/// </remarks>
[ActivitySource]
[Meter]
public interface IWeatherForecastTelemetry
{
	[Activity]
	Activity? ValidatingForecast([Tag] DateOnly forecastDate);

	[Counter]
	void ForecastsValidated([InstrumentMeasurement] int count);
}
