using System.Diagnostics;
using Purview.Telemetry;

namespace SampleApp;

/// <summary>
/// Telemetry owned by the shared library, which has no idea how the host registers telemetry. The
/// generator records <c>sample-shared-library</c> in this assembly's metadata, and each host project
/// aggregates it into its own <c>TelemetryNames</c> class — see the <c>[TelemetryGeneration]</c> in
/// the API service and web projects.
/// </summary>
[ActivitySource("sample-shared-library")]
[TelemetryGeneration(GenerateDependencyExtension = false)]
public interface IWeatherForecastTelemetry
{
	[Activity]
	Activity? ValidatingForecast([Tag] DateOnly forecastDate);
}
