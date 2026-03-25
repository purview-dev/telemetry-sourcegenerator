using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SampleApp.Net48.ConsoleApp.Services
{
    sealed class WeatherService : IWeatherService
    {
        static readonly Random _rng = new Random();

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

        const int TooColdTempInC = -10;

        readonly IWeatherServiceTelemetry _telemetry;

        public WeatherService(IWeatherServiceTelemetry telemetry)
        {
            _telemetry = telemetry;
        }

        public IReadOnlyList<WeatherForecast> GetWeatherForecasts(int requestCount)
        {
            const int minRequestCount = 5;
            const int maxRequestCount = 20;

            if (requestCount < minRequestCount || requestCount > maxRequestCount)
            {
                // MULTI-TARGET: This single call logs error + increments counter
                _telemetry.RequestedCountIsOutOfRange(requestCount);

                throw new ArgumentOutOfRangeException(
                    nameof(requestCount),
                    $"Must be between {minRequestCount} and {maxRequestCount}."
                );
            }

            var sw = Stopwatch.StartNew();

            // MULTI-TARGET: This single call starts Activity + logs info
            using var activity = _telemetry.GettingWeatherForecast(
                Guid.NewGuid().ToString(),
                requestCount
            );

            // Simulate some variable latency, like a database call or something.
            System.Threading.Thread.Sleep(_rng.Next(0, 50));

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
                    _telemetry.FailedToRetrieveForecast(activity, ex);
                    throw;
                }
            }

            var results = new WeatherForecast[requestCount];
            for (var i = 0; i < requestCount; i++)
            {
                results[i] = new WeatherForecast
                {
                    Date = DateTime.UtcNow.AddDays(i),
                    TemperatureC = _rng.Next(-20, 55),
                    Summary = Summaries[_rng.Next(Summaries.Length)],
                };
            }

            foreach (var wf in results)
                _telemetry.HistogramOfTemperature(wf.TemperatureC);

            var minTempInC = results.Min(m => m.TemperatureC);

            // MULTI-TARGET: This single call adds event to activity + logs info
            _telemetry.ForecastReceived(activity, minTempInC, results.Max(wf => wf.TemperatureC));

            if (minTempInC < TooColdTempInC)
            {
                // MULTI-TARGET: This single call increments counter + logs warning
                _telemetry.ItsTooCold(
                    activity,
                    minTempInC,
                    results.Count(wf => wf.TemperatureC < TooColdTempInC)
                );
            }

            sw.Stop();

            // MULTI-TARGET: This single call adds OK event to activity + logs info
            _telemetry.TemperaturesReceived(activity, sw.Elapsed);

            return results;
        }

        static bool ShouldThrow()
        {
            // Dial it up to 11, and if we hit an 8, throw an exception.
            return _rng.Next(1, 12) == 8;
        }
    }
}
