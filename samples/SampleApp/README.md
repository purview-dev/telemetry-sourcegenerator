# Purview Telemetry Sample App

A real-world .NET Aspire weather forecast application demonstrating the **Purview Telemetry Source Generator** with all three telemetry targets — Activities, Logging, and Metrics — across two services, including multi-target methods that emit multiple telemetry types from a single call.

## Overview

| Project | Description |
| --- | --- |
| `SampleApp.AppHost` | .NET Aspire orchestrator — wires up all resources and the Aspire dashboard |
| `SampleApp.APIService` | Weather REST API backend; contains the primary telemetry interfaces |
| `SampleApp.Web` | Blazor Server frontend that calls the API with its own HTTP client telemetry |
| `SampleApp.Shared` | Shared `WeatherForecast` DTO used by both API and frontend |
| `SampleApp.ServiceDefaults` | Common Aspire service defaults (OpenTelemetry, health checks, resilience) |
| `SampleApp.APIService.UnitTests` | Unit tests demonstrating how to mock and assert generated telemetry interfaces |

## What It Demonstrates

### Three telemetry interfaces

#### `IEntityStoreTelemetry` (global namespace, `SampleApp.APIService`)

A self-contained reference example matching the [Quick Start](../../README.md) guide. Not wired into the API endpoints — its purpose is to show a clean before/after comparison and demonstrate that generated code is inspectable with `EmitCompilerGeneratedFiles`.

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

#### `IWeatherServiceTelemetry` (`SampleApp.APIService.Services`)

The primary backend telemetry. Injected into `WeatherService` and registered via `builder.Services.AddWeatherServiceTelemetry()`. Demonstrates the full range of multi-target patterns:

```csharp
[ActivitySource]
[Logger]
[Meter]
public interface IWeatherServiceTelemetry
{
    // MULTI-TARGET: starts Activity (Client kind) + logs Trace
    [Activity(ActivityKind.Client)]
    [Trace]
    Activity? GettingWeatherForecast([Baggage] string someRandomBaggageInfo, int requestedCount);

    // SINGLE-TARGET: adds ActivityEvent
    [Event]
    void ForecastReceived(Activity? activity, int minTempInC, int maxTempInC);

    // SINGLE-TARGET: adds ActivityEvent with Error status
    [Event(ActivityStatusCode.Error)]
    void FailedToRetrieveForecast(Activity? activity, Exception ex);

    // SINGLE-TARGET: adds ActivityEvent with Ok status
    [Event(ActivityStatusCode.Ok)]
    void TemperaturesReceived(Activity? activity, TimeSpan elapsed);

    // MULTI-TARGET: increments counter + logs Warning + adds ActivityEvent
    [AutoCounter]
    [Warning]
    [Event]
    void ItsTooCold(Activity? activity, int minTempInC, int tooColdCount);

    // SINGLE-TARGET: histogram per temperature reading
    [Histogram]
    void HistogramOfTemperature(int temperature);

    // MULTI-TARGET: logs Error + increments counter
    [Error]
    [AutoCounter]
    void RequestedCountIsOutOfRange(int requestCount);

    // SINGLE-TARGET: Info log with enumerable expansion (up to 100 items)
    [Info]
    void TemperaturesWithinRange([ExpandEnumerable(maximumValueCount: 100)] int[] temperaturesInC);
}
```

#### `IWeatherAPIClientTelemetry` (`SampleApp.Web.Clients`)

Frontend HTTP client telemetry. Registered via `builder.Services.AddWeatherAPIClientTelemetry()` and injected into `WeatherAPIClient`. Demonstrates `[ExcludeTargets]` and `[ExpandEnumerable]`:

```csharp
[ActivitySource]
[Logger]
[Meter(InstrumentPrefix = "weather")]
public interface IWeatherAPIClientTelemetry
{
    // MULTI-TARGET: starts Activity (Client kind) + logs Info + increments counter
    [Activity(ActivityKind.Client)]
    [Info]
    [AutoCounter]
    Activity? GetWeatherForecasts(int? count);

    // MULTI-TARGET: adds ActivityEvent + logs Error + increments counter
    // count excluded from Activities
    [Event]
    [Error]
    [AutoCounter]
    void FailedToGetForecast(Activity? activity, Exception ex,
        [ExcludeTargets(TargetsEnum.Activities)] int? count);

    // SINGLE-TARGET: adds ActivityEvent with HTTP status details
    [Event]
    void RequestComplete(Activity? activity, HttpStatusCode statusCode, bool isSuccessStatusCode);

    // SINGLE-TARGET: increments success counter
    [AutoCounter]
    void RequestSuccess();

    // MULTI-TARGET: adds ActivityEvent + logs Warning
    [Event]
    [Warning]
    void NoForecastsRecieved(Activity? activity);

    // MULTI-TARGET: adds ActivityEvent with Ok status + logs Debug
    // forecasts excluded from Activities
    [Event(ActivityStatusCode.Ok)]
    [Debug]
    void ForecastsRecieved(Activity? activity, int forecastCount,
        [ExpandEnumerable(100), ExcludeTargets(TargetsEnum.Activities)] WeatherForecast[] weatherForecasts);
}
```

### Key features shown

1. **Multi-target generation** — single method call emits Activity + Log + Metric simultaneously
2. **Activity lifecycle** — start, events, status codes (Ok / Error), baggage propagation
3. **Structured logging** — all log levels (Trace → Critical), structured properties, enumerable expansion
4. **Metrics** — auto-counters, histograms, `InstrumentPrefix` customisation
5. **`[ExcludeTargets]`** — exclude a parameter from specific telemetry types in multi-target methods
6. **`[ExpandEnumerable]`** — log individual array/IEnumerable elements as separate properties
7. **DI registration** — generated `Add*Telemetry()` extension methods
8. **`TelemetryNames`** — generated static class with all meter and activity source names for OTel registration
9. **Unit testing** — mock telemetry interfaces with NSubstitute; see `SampleApp.APIService.UnitTests`

## Project Structure

```text
SampleApp/
├── SampleApp.AppHost/
│   └── Program.cs                          # Aspire orchestration
├── SampleApp.APIService/
│   ├── Endpoints/
│   │   └── WeatherEndpoints.cs             # Minimal API endpoints
│   ├── Properties/
│   │   └── AssemblyInfo.cs                 # [assembly: ActivitySourceGeneration]
│   ├── Services/
│   │   ├── IEntityStoreTelemetry.cs        # Reference telemetry interface (Quick Start)
│   │   ├── IWeatherService.cs              # Business logic interface
│   │   ├── IWeatherServiceTelemetry.cs     # Primary telemetry interface
│   │   └── WeatherService.cs              # Business logic implementation
│   └── Program.cs
├── SampleApp.Web/
│   ├── Clients/
│   │   ├── IWeatherAPIClientTelemetry.cs   # Frontend HTTP client telemetry
│   │   └── WeatherAPIClient.cs
│   ├── Components/                         # Blazor components
│   ├── Properties/
│   │   └── AssemblyInfo.cs
│   └── Program.cs
├── SampleApp.ServiceDefaults/              # OpenTelemetry, health checks, resilience
├── SampleApp.Shared/                       # WeatherForecast DTO
└── SampleApp.APIService.UnitTests/         # Unit tests with telemetry mocking
```

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [Aspire CLI](https://learn.microsoft.com/en-us/dotnet/aspire/) — the AppHost SDK (`Aspire.AppHost.Sdk`) and
  orchestration binaries are resolved from NuGet and the Aspire CLI bundle, so no legacy Aspire workload is required.

### Running the Application

```bash
cd samples/SampleApp
dotnet run --project SampleApp.AppHost
```

The .NET Aspire dashboard opens automatically. It lists three running resources: `api-service`, `web`, and `scalar`.

To generate telemetry:
- **Web Frontend** — click the `web` resource endpoint, go to the **Weather** page, and click **Load Weather**
- **Scalar API Docs** — click the `scalar` endpoint, expand the Weather API, and use the Send button

### Running Tests

```bash
cd samples/SampleApp
dotnet test
```

Tests validate business logic while mocking telemetry calls using NSubstitute. See `SampleApp.APIService.UnitTests` for examples of mocking and asserting generated telemetry interfaces.

## Exploring Generated Code

Both `SampleApp.APIService` and `SampleApp.Web` have `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` enabled, so generated files appear in your IDE and on disk.

**`SampleApp.APIService`** generates:

```
obj/Release/net10.0/generated/Purview.Telemetry.SourceGenerator/Purview.Telemetry.SourceGenerator.TelemetrySourceGenerator/
  EntityStoreTelemetryCore.Activity.g.cs
  EntityStoreTelemetryCore.Logging.g.cs
  EntityStoreTelemetryCore.Metric.g.cs
  EntityStoreTelemetryCoreDIExtension.DependencyInjection.g.cs
  SampleApp.APIService.Services.WeatherServiceTelemetryCore.Activity.g.cs
  SampleApp.APIService.Services.WeatherServiceTelemetryCore.Logging.g.cs
  SampleApp.APIService.Services.WeatherServiceTelemetryCore.Metric.g.cs
  SampleApp.APIService.Services.WeatherServiceTelemetryCoreDIExtension.DependencyInjection.g.cs
  SampleApp.APIService.TelemetryNames.g.cs
```

**`SampleApp.Web`** generates:

```
obj/Release/net10.0/generated/Purview.Telemetry.SourceGenerator/Purview.Telemetry.SourceGenerator.TelemetrySourceGenerator/
  SampleApp.Web.Clients.WeatherAPIClientTelemetryCore.Activity.g.cs
  SampleApp.Web.Clients.WeatherAPIClientTelemetryCore.Logging.g.cs
  SampleApp.Web.Clients.WeatherAPIClientTelemetryCore.Metric.g.cs
  SampleApp.Web.Clients.WeatherAPIClientTelemetryCoreDIExtension.DependencyInjection.g.cs
  SampleApp.Web.TelemetryNames.g.cs
```

The `TelemetryNames.g.cs` files expose static arrays used in `Program.cs` to register all sources with OpenTelemetry:

```csharp
builder.AddServiceDefaults(TelemetryNames.MeterNames, TelemetryNames.ActivitySourceNames);
```

## Monitoring Telemetry

### Aspire Dashboard

After generating some traffic, explore each tab:

| Tab | What you see |
| --- | --- |
| **Traces** | Distributed traces spanning both `web` and `api-service` hops with full end-to-end duration, Activity span hierarchy, ActivityEvents, baggage, and tag properties |
| **Structured Logs** | Log entries with Level, Message, and all structured properties (e.g. `RequestedCount`, `MinTempInC`) |
| **Metrics** | Generated counters and histograms from both services: `getting-weather-forecast`, `its-too-cold`, `histogram-of-temperature`, `get-weather-forecasts`, etc. |
| **Console Logs** | Raw stdout output per service |
| **Resources** | Failed requests surface as error badges on `api-service` and/or `web` |

### Using dotnet-counters

Monitor metrics from the running API service in real time:

```bash
# Find the process ID
dotnet-counters ps

# Monitor all meters
dotnet-counters monitor --process-id <PID>

# Monitor a specific meter
dotnet-counters monitor --process-id <PID> --counters SampleApp.APIService.Services.IWeatherServiceTelemetry

# Watch specific instruments
dotnet-counters monitor --process-id <PID> --counters SampleApp.APIService.Services.IWeatherServiceTelemetry[its-too-cold]
```

## Learn More

- [Purview Telemetry Source Generator docs](../../README.md)
- [Wiki: Sample Application](https://github.com/kjldev/purview-telemetry-sourcegenerator/wiki/Sample-Application) — annotated walkthroughs, sequence diagrams, dashboard screenshots
- [Wiki: Generated Output](https://github.com/kjldev/purview-telemetry-sourcegenerator/wiki/Generated-Output) — full annotated examples of generated code
- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)

