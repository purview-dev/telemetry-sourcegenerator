using System.Diagnostics;
using System.Net;
using Purview.Telemetry;

namespace SampleApp.Web.Clients;

[ActivitySource]
[Logger]
[Meter(InstrumentPrefix = "weather")]
public interface IWeatherAPIClientTelemetry
{
	[Activity(ActivityKind.Client)]
	[Info]
	[AutoCounter]
	Activity? GetWeatherForecasts(int? count);

	[Event]
	[Error]
	[AutoCounter]
	void FailedToGetForecast(Activity? activity, Exception ex, [ExcludeTargets(TargetsEnum.Activities)] int? count);

	[Event]
	void RequestComplete(Activity? activity, HttpStatusCode statusCode, bool isSuccessStatusCode);

	[AutoCounter]
	void RequestSuccess();

	[Event]
	[Warning]
	void NoForecastsRecieved(Activity? activity);

	[Event(ActivityStatusCode.Ok)]
	[Debug]
	void ForecastsRecieved(
		Activity? activity,
		int forecastCount,
		[ExpandEnumerable(100), ExcludeTargets(TargetsEnum.Activities)]
#pragma warning disable TSG2008 // Unbounded enumeration possible
		WeatherForecast[] weatherForecasts
	);
#pragma warning restore TSG2008 // Unbounded enumeration possible
}
