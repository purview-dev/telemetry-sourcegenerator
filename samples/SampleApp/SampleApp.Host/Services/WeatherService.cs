using System.Diagnostics;
using System.Security.Cryptography;

namespace SampleApp.Host.Services;

sealed class WeatherService(IWeatherServiceTelemetry telemetry, Func<int>? rng = null)
	: IWeatherService
{
	const int TooColdTempInC = -10;

	static readonly string[] Summaries =
	[
		"Freezing",
		"Bracing",
		"Chilly",
		"Cool",
		"Mild",
		"Warm",
		"Balmy",
		"Hot",
		"Sweltering",
		"Scorching",
	];

	public Task<IEnumerable<WeatherForecast>> GetWeatherForecastsAsync(
		int requestCount,
		CancellationToken cancellationToken = default
	)
	{
		const int minRequestCount = 5;
		const int maxRequestCount = 20;

		if (requestCount < minRequestCount || requestCount > maxRequestCount)
		{
			telemetry.RequestedCountIsTooSmall(requestCount);

			throw new ArgumentOutOfRangeException(
				nameof(requestCount),
				$"Requested count must be at least {minRequestCount}, and no greater than {maxRequestCount}."
			);
		}

		var sw = Stopwatch.StartNew();
		using var activity = telemetry.GettingWeatherForecastFromUpstreamService(
			$"{Guid.NewGuid()}",
			requestCount
		);

		telemetry.WeatherForecastRequested();

		// This would usually be async of course...
		cancellationToken.ThrowIfCancellationRequested();

		var shouldThrow = (rng?.Invoke() ?? RandomNumberGenerator.GetInt32(1, 11)) == 8; // Eight ball says boom.
		if (shouldThrow)
		{
			Exception ex = new("Failed to retrieve forecast from (simulated) upstream service.");

			telemetry.FailedToRetrieveForecast(activity, ex);
			telemetry.WeatherForecastRequestFailed(ex);

			throw ex;
		}

		var results = Enumerable
			.Range(1, requestCount)
			.Select(index => new WeatherForecast
			{
				Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(--index)),
				TemperatureC = RandomNumberGenerator.GetInt32(-20, 55),
				Summary = Summaries[RandomNumberGenerator.GetInt32(Summaries.Length)],
			})
			.ToArray();

		foreach (var wf in results)
			telemetry.HistogramOfTemperature(wf.TemperatureC);

		var minTempInC = results.Min(m => m.TemperatureC);
		telemetry.ForecastReceived(activity, minTempInC, results.Max(wf => wf.TemperatureC));

		if (minTempInC < TooColdTempInC)
		{
			telemetry.TemperatureOutOfRange(minTempInC);
			telemetry.ItsTooCold(results.Count(wf => wf.TemperatureC < TooColdTempInC));
		}
		else
		{
			telemetry.TemperaturesWithinRange([.. results.Select(m => m.TemperatureC)]);
		}

		sw.Stop();

		telemetry.TemperaturesReceived(activity, sw.Elapsed);

		// This isn't really async, we're just pretending.
		return Task.FromResult(results.AsEnumerable());
	}
}
