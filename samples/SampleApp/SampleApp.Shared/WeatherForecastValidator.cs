namespace SampleApp;

/// <summary>
/// Behaviour owned by the shared library, instrumented with the shared library's own telemetry.
/// Hosts call it and never register its activity source or meter themselves: the generator records
/// both names in this assembly's metadata, and each host aggregates them into its own
/// <c>TelemetryNames</c>.
/// </summary>
public sealed class WeatherForecastValidator(IWeatherForecastTelemetry telemetry)
{
	const int MinimumPlausibleTemperatureC = -30;
	const int MaximumPlausibleTemperatureC = 60;

	/// <summary>Returns the forecasts whose temperature is plausible.</summary>
	public IReadOnlyList<WeatherForecast> Validate(IEnumerable<WeatherForecast> forecasts)
	{
		ArgumentNullException.ThrowIfNull(forecasts);

		List<WeatherForecast> plausible = [];
		foreach (var forecast in forecasts)
		{
			using var activity = telemetry.ValidatingForecast(forecast.Date);

			if (forecast.TemperatureC is >= MinimumPlausibleTemperatureC and <= MaximumPlausibleTemperatureC)
				plausible.Add(forecast);
		}

		telemetry.ForecastsValidated(plausible.Count);

		return plausible;
	}
}
