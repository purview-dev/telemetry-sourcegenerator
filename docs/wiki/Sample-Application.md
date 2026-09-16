# Sample Application

The [.NET Aspire sample](https://github.com/purview-dev/telemetry-sourcegenerator/tree/main/samples/SampleApp) demonstrates Activities, Logs, and Metrics generation working together with the Aspire Dashboard.

## Solution layout

| Project | Purpose |
| --- | --- |
| `SampleApp.AppHost` | The Aspire orchestrator. |
| `SampleApp.APIService` | Backend service exposing the telemetry interfaces and the generated `TelemetryNames` registration. |
| `SampleApp.APIService.UnitTests` | TUnit + NSubstitute unit tests over the generated interfaces. |
| `SampleApp.Shared` | Shared DTOs (e.g. `WeatherForecast`). |
| `SampleApp.Web` | Frontend client that also generates its own telemetry. |
| `SampleApp.ServiceDefaults` | Aspire service defaults; registers the generated meter/activity source names. |

The sample targets `net10.0` and enables `EmitCompilerGeneratedFiles`, so generated output can be inspected under:

```
obj/<Configuration>/net10.0/generated/Purview.Telemetry.SourceGenerator/Purview.Telemetry.SourceGenerator.TelemetrySourceGenerator/
```

## Telemetry interfaces

### `IEntityStoreTelemetry` (APIService)

The multi-target interface from the README — one method emits an Activity, an Info log, and an AutoCounter simultaneously:

```csharp
[ActivitySource]
[Logger]
[Meter]
interface IEntityStoreTelemetry
{
    [Activity]
    [Info]
    [AutoCounter]
    Activity? GettingEntityFromStore(int entityId, [Baggage] string serviceUrl);

    [Event]
    [Trace]
    void GetDuration(Activity? activity, int durationInMS);

    [Context]
    void RetrievedEntity(Activity? activity, float totalValue, int lastUpdatedByUserId);

    [Warning]
    void EntityNotFound(int entityId);

    [Histogram]
    void RecordEntitySize(int sizeInBytes);
}
```

### `IWeatherServiceTelemetry` (APIService)

Demonstrates single-method multi-targeting, Activity status codes, and enumerable expansion:

```csharp
[ActivitySource]
[Logger]
[Meter]
public interface IWeatherServiceTelemetry
{
    [Activity(ActivityKind.Client)]
    [Trace]
    Activity? GettingWeatherForecast([Baggage] string someRandomBaggageInfo, int requestedCount);

    [Event]
    void ForecastReceived(Activity? activity, int minTempInC, int maxTempInC);

    [Event(ActivityStatusCode.Error)]
    void FailedToRetrieveForecast(Activity? activity, Exception exception);

    [Event(ActivityStatusCode.Ok)]
    void TemperaturesReceived(Activity? activity, TimeSpan elapsed);

    [AutoCounter]
    [Warning]
    [Event]
    void ItsTooCold(Activity? activity, int minTempInC, int tooColdCount);

    [Histogram]
    void HistogramOfTemperature(int temperature);

    [Error]
    [AutoCounter]
    void RequestedCountIsOutOfRange(int requestCount);

    [Info]
    void TemperaturesWithinRange([ExpandEnumerable(maximumValueCount: 100)] int[] temperaturesInC);
}
```

### `IWeatherAPIClientTelemetry` (Web)

Demonstrates `[ExcludeTargets]`, an instrument prefix, and `HttpStatusCode` tags:

```csharp
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
    void FailedToGetForecast(Activity? activity, Exception ex, [ExcludeTargets(Targets.Activities)] int? count);

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
        [ExpandEnumerable(100), ExcludeTargets(Targets.Activities)]
        WeatherForecast[] weatherForecasts
    );
}
```

## Registering names with Aspire

`SampleApp.ServiceDefaults` wires the generated names into Aspire's OpenTelemetry setup:

```csharp
builder.AddServiceDefaults(TelemetryNames.MeterNames, TelemetryNames.ActivitySourceNames);
```

## Unit testing

`SampleApp.APIService.UnitTests` uses TUnit with NSubstitute to verify `WeatherService` behaviour against the generated interfaces without emitting real telemetry. See [Testing](Testing.md) for the pattern.

## Running

```bash
just build-s
just test-s
```

With `dotnet run --project samples/SampleApp/SampleApp.AppHost`, the Aspire Dashboard shows the generated Activities, Logs, and Metrics in real time.

## See also

- [Getting Started](Getting-Started.md)
- [Multi-Targeting](Multi-Targeting.md)
- [Generated Output](Generated-Output.md) — real generated code from this sample