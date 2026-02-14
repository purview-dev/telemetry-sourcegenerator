using System.Diagnostics;
using System.Security.Cryptography;

namespace SampleApp.APIService.Services;

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

	public async Task<ErrorOr<IEnumerable<WeatherForecast>>> GetWeatherForecastsAsync(
		int requestCount,
		CancellationToken cancellationToken = default
	)
	{
		// Simulate some variable latency, like a database call or something.
		var simulatedWait = RandomNumberGenerator.GetInt32(0, 4) * 1000;
		await Task.Delay(simulatedWait, cancellationToken);

		const int minRequestCount = 5;
		const int maxRequestCount = 20;

		if (requestCount < minRequestCount || requestCount > maxRequestCount)
		{
			telemetry.RequestedCountIsTooSmall(requestCount);

			return Error.Validation(
				"RequestCount.Invalid",
				$"Requested count must be at least {minRequestCount}, and no greater than {maxRequestCount}."
			);

			throw new ArgumentOutOfRangeException(
				nameof(requestCount),
				$"Requested count must be at least {minRequestCount}, and no greater than {maxRequestCount}."
			);
		}

		var sw = Stopwatch.StartNew();

		// MULTI-TARGET: This single call starts Activity + logs info + increments counter
		using var activity = telemetry.GettingWeatherForecast($"{Guid.NewGuid()}", requestCount);

		// This would usually be async of course...
		cancellationToken.ThrowIfCancellationRequested();
		if (ShouldThrow())
		{
			try
			{
				throw new Exception(
					"Simulated failure - maybe a database or something went ~{fizz-bang}~."
				);
			}
			catch (Exception ex)
			{
				// MULTI-TARGET: This single call adds error event to activity + logs critical
				telemetry.FailedToRetrieveForecast(activity, ex);

				return Error.Failure(
					"WeatherForecast.RetrievalFailed",
					"Failed to retrieve weather forecast data."
				);
			}
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

		// MULTI-TARGET: This single call adds event to activity + logs info
		telemetry.ForecastReceived(activity, minTempInC, results.Max(wf => wf.TemperatureC));

		if (minTempInC < TooColdTempInC)
		{
			// MULTI-TARGET: This single call increments counter + logs warning
			telemetry.ItsTooCold(
				activity,
				minTempInC,
				results.Count(wf => wf.TemperatureC < TooColdTempInC)
			);
		}
		else
		{
			telemetry.TemperaturesWithinRange([.. results.Select(m => m.TemperatureC)]);
		}

		sw.Stop();

		// MULTI-TARGET: This single call adds OK event to activity + logs info
		telemetry.TemperaturesReceived(activity, sw.Elapsed);

		return results.ToArray();
	}

	bool ShouldThrow()
	{
		// Dial it up to 11, and if we hit an 8, throw an exception.
		return (rng?.Invoke() ?? RandomNumberGenerator.GetInt32(1, 11)) == 8;
	}
}
