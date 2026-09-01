using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SampleApp.Net48.ConsoleApp.Services;

namespace SampleApp.Net48.ConsoleApp
{
	static class Program
	{
		static void Main()
		{
			using var activityListener = new ActivityListener
			{
				ShouldListenTo = source => source.Name == TelemetryNames.ActivitySourceNames[0],
				Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
					ActivitySamplingResult.AllDataAndRecorded,
				SampleUsingParentId = (ref ActivityCreationOptions<string> options) =>
					ActivitySamplingResult.AllDataAndRecorded,
				ActivityStarted = a => Console.WriteLine($"  [TRACE START] {a.DisplayName} ({a.Id})"),
				ActivityStopped = a =>
					Console.WriteLine(
						$"  [TRACE STOP]  {a.DisplayName} - {a.Status} ({a.Duration.TotalMilliseconds:F1} ms)"
					),
			};
			ActivitySource.AddActivityListener(activityListener);

			using var meterListener = new MeterListener();
			meterListener.InstrumentPublished = (instrument, listener) =>
			{
				if (Array.IndexOf(TelemetryNames.MeterNames, instrument.Meter.Name) >= 0)
					listener.EnableMeasurementEvents(instrument);
			};
			meterListener.SetMeasurementEventCallback<int>(
				(instrument, value, tags, state) =>
					Console.WriteLine($"  [METRIC] {instrument.Meter.Name}/{instrument.Name}: {value}")
			);
			meterListener.SetMeasurementEventCallback<long>(
				(instrument, value, tags, state) =>
					Console.WriteLine($"  [METRIC] {instrument.Meter.Name}/{instrument.Name}: {value}")
			);
			meterListener.Start();

			var services = new ServiceCollection();

			services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Trace));

			services.AddScoped<IWeatherService, WeatherService>();
			services.AddWeatherServiceTelemetry();

			using var provider = services.BuildServiceProvider();
			using var scope = provider.CreateScope();

			var weatherService = scope.ServiceProvider.GetRequiredService<IWeatherService>();

			Console.WriteLine("=== Purview Telemetry - .NET Framework 4.8 Sample ===");
			Console.WriteLine($"Activity source : {TelemetryNames.ActivitySourceNames[0]}");
			Console.WriteLine($"Meter           : {TelemetryNames.MeterNames[0]}");
			Console.WriteLine();

			for (var i = 0; i < 5; i++)
			{
				Console.WriteLine($"--- Request {i + 1} ---");
				try
				{
					var forecasts = weatherService.GetWeatherForecasts(10);
					Console.WriteLine(
						$"  Got {forecasts.Count} forecast(s). Min: {forecasts.Min(f => f.TemperatureC)}C, Max: {forecasts.Max(f => f.TemperatureC)}C"
					);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"  Request failed: {ex.Message}");
				}

				Console.WriteLine();
			}

			Console.WriteLine("Done. Press any key to exit.");
			Console.ReadKey();
		}
	}
}
