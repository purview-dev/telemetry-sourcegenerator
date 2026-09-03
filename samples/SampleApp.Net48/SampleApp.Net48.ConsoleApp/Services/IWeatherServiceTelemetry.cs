using System.Diagnostics;
using Purview.Telemetry;

namespace SampleApp.Net48.ConsoleApp.Services
{
	/*
	 * Multi-Target Interface: single methods generate Activity + Log + Metric simultaneously.
	 * NET48_OR_GREATER is defined automatically, so injected attribute files use plain string
	 * instead of string? and omit #nullable enable.
	 */

	[ActivitySource]
	[Logger]
	[Meter]
	public interface IWeatherServiceTelemetry
	{
		// --> MULTI-TARGET: Activity + Info log
		[Activity(ActivityKind.Client)]
		[Info]
		Activity GettingWeatherForecast([Baggage] string requestId, int requestedCount);

		// --> SINGLE-TARGET: Event
		[Event]
		void ForecastReceived(Activity activity, int minTempInC, int maxTempInC);

		// --> SINGLE-TARGET: Event (Error)
		[Event(ActivityStatusCode.Error)]
		void FailedToRetrieveForecast(Activity activity, Exception ex);

		// --> SINGLE-TARGET: Event (Ok)
		[Event(ActivityStatusCode.Ok)]
		void TemperaturesReceived(Activity activity, TimeSpan elapsed);

		// --> MULTI-TARGET: AutoCounter + Warning log + Event
		[AutoCounter]
		[Warning]
		[Event]
		void ItsTooCold(Activity activity, int minTempInC, int tooColdCount);

		// --> SINGLE-TARGET: Histogram
		[Histogram]
		void HistogramOfTemperature(int temperature);

		// --> MULTI-TARGET: Error log + AutoCounter
		[Error]
		[AutoCounter]
		void RequestedCountIsOutOfRange(int requestCount);
	}
}
