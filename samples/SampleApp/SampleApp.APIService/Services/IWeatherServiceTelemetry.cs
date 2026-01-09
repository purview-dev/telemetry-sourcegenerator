using System.Diagnostics;
using Purview.Telemetry;

namespace SampleApp.APIService.Services;

/*
 * v4.0 Multi-Target Interface: demonstrates single-method multi-targeting
 * where ONE method can generate Activity + Log + Metric simultaneously.
 *
 * This reduces boilerplate by combining related telemetry into single calls.
*/

[ActivitySource]
[Logger]
[Meter]
public interface IWeatherServiceTelemetry
{
	// --> SINGLE-TARGET: Activity only
	// Single Activity method
	[Activity(ActivityKind.Client)]
	Activity? GettingWeatherForecast([Baggage] string someRandomBaggageInfo, int requestedCount);

	// --> SINGLE-TARGET: Event
	[Event]
	void ForecastReceived(Activity? activity, int minTempInC, int maxTempInC);

	// --> SINGLE-TARGET: Event (Error)
	[Event(ActivityStatusCode.Error)]
	void FailedToRetrieveForecast(Activity? activity, Exception ex);

	// --> SINGLE-TARGET: Event (Ok)
	[Event(ActivityStatusCode.Ok)]
	void TemperaturesReceived(Activity? activity, TimeSpan elapsed);

	// --> MULTI-TARGET: AutoCounter + Warning Log (v4.0 feature)
	// Increments "too cold" counter AND logs warning in a single call
	[AutoCounter]
	[Log(LogLevel.Warning)]
	[Event]
	void ItsTooCold(Activity? activity, int minTempInC, int tooColdCount);

	// --> SINGLE-TARGET: Histogram only
	// Records temperature distribution
	[Histogram]
	void HistogramOfTemperature(int temperature);

	// --> SINGLE-TARGET: Error Log only
	// Request count validation error
	[Error]
	void RequestedCountIsTooSmall(int requestCount);

	// --> SINGLE-TARGET: Info Log with enumerable expansion
#pragma warning disable TSG2008 // Unbounded enumeration possible
	[Info]
	void TemperaturesWithinRange([ExpandEnumerable(maximumValueCount: 100)] int[] temperaturesInC);
#pragma warning restore TSG2008 // Unbounded enumeration possible
}
