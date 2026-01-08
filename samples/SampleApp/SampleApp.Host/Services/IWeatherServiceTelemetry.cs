using System.Diagnostics;
using Purview.Telemetry;

namespace SampleApp.Host.Services;

/*
 * A multi-target interface that defines
 * the telemetry methods for the WeatherService, including
 * Activities, Events, Logs, and Metrics.
 *
 * As it's multi-target, each target (method) needs to be
 * explicitly defined.
*/

[ActivitySource]
[Logger]
[Meter]
public interface IWeatherServiceTelemetry
{
	// --> Start: Activities

	[Activity(ActivityKind.Client)]
	Activity? GettingWeatherForecastFromUpstreamService(
		[Baggage] string someRandomBaggageInfo,
		int requestedCount
	);

	[Event]
	void ForecastReceived(Activity? activity, int minTempInC, int maxTempInC);

	[Event(ActivityStatusCode.Error)]
	void FailedToRetrieveForecast(Activity? activity, Exception ex);

	[Event(ActivityStatusCode.Ok)]
	void TemperaturesReceived(Activity? activity, TimeSpan elapsed);

	// --> END: Activities

	// --> START: Meters

	[AutoCounter]
	void WeatherForecastRequested();

	[AutoCounter]
	void ItsTooCold(int tooColdCount);

	[Histogram]
	void HistogramOfTemperature(int temperature);

	// --> END: Meters

	// --> START: Logs

	[Log(LogLevel.Warning)]
	void TemperatureOutOfRange(int minTempInC);

	[Error]
	void RequestedCountIsTooSmall(int requestCount);

	// maximumValueCount is optional, and defaults to 5.
	// But recommended to NOT use it for performance reasons.
	[Info]
#pragma warning disable TSG2008 // Unbounded enumeration possible
	void TemperaturesWithinRange([ExpandEnumerable(maximumValueCount: 100)] int[] temperaturesInC);
#pragma warning restore TSG2008 // Unbounded enumeration possible

	[Critical]
	void WeatherForecastRequestFailed(Exception ex);

	// --> END: Logs
}
