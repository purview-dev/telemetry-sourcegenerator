namespace SampleApp.Web.Clients;

/// <summary>
/// Typed HTTP client for communicating with the Weather API service.
/// Uses Aspire service discovery and HTTP resiliency (retries, circuit breaker).
/// </summary>
public sealed class WeatherAPIClient(HttpClient httpClient, IWeatherAPIClientTelemetry telemetry)
{
	/// <summary>
	/// Gets weather forecasts from the API service.
	/// </summary>
	/// <param name="count">Number of forecasts to retrieve (5-20).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Array of weather forecasts.</returns>
	public async Task<WeatherForecast[]> GetWeatherForecastsAsync(
		int? count = null,
		CancellationToken cancellationToken = default
	)
	{
		using var activity = telemetry.GetWeatherForecasts(count);

		try
		{
			Uri url = new(
				count.HasValue ? $"/weatherforecast/{count}" : "/weatherforecast",
				UriKind.Relative
			);
			var result = await httpClient.GetAsync(url, cancellationToken);

			telemetry.RequestComplete(activity, result.StatusCode, result.IsSuccessStatusCode);

			result.EnsureSuccessStatusCode();

			telemetry.RequestSuccess();

			var forecasts =
				await result.Content.ReadFromJsonAsync<WeatherForecast[]>(
					cancellationToken: cancellationToken
				) ?? [];

			if (forecasts.Length > 0)
				telemetry.NoForecastsRecieved(activity);
			else
				telemetry.ForecastsRecieved(activity, forecasts.Length, forecasts);

			return forecasts;
		}
		catch (Exception ex)
		{
			telemetry.FailedToGetForecast(activity, ex, count);

			// The frontend will deal with exceptions in different ways based on type.
			throw;
		}
	}
}
