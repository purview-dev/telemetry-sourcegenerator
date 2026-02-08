# Purview Telemetry Sample App

This sample demonstrates the **Purview Telemetry Source Generator** in a real-world .NET Aspire application with comprehensive telemetry instrumentation using Activities, Logging, and Metrics.

## Overview

The sample app is a weather forecast API built with:

- **Backend API** (`SampleApp.APIService`) - RESTful weather service with generated telemetry
- **Blazor Web Frontend** (`SampleApp.Web`) - Interactive UI consuming the API
- **.NET Aspire** (`SampleApp.AppHost`) - Orchestration and observability dashboard

## What It Demonstrates

### Multi-Target Telemetry Generation

The `IWeatherServiceTelemetry` interface showcases **v4.0 multi-target generation**, where a single method call generates multiple telemetry types simultaneously:

```csharp
[ActivitySource]
[Logger]
[Meter]
public interface IWeatherServiceTelemetry
{
    // ✨ MULTI-TARGET: Single call creates Activity + Logs Trace + Increments Counter
    [Activity(ActivityKind.Client)]
    [Trace]
    Activity? GettingWeatherForecast([Baggage] string someRandomBaggageInfo, int requestedCount);

    // ✨ MULTI-TARGET: AutoCounter + Warning Log entry + Event
    [AutoCounter]
    [Warning]
    [Event]
    void ItsTooCold(Activity? activity, int minTempInC, int tooColdCount);

    // Single-target examples
    [Event]
    void ForecastReceived(Activity? activity, int minTempInC, int maxTempInC);

    [Histogram]
    void HistogramOfTemperature(int temperature);
}
```

### Key Features Demonstrated

1. **Activity Generation** - Distributed tracing with OpenTelemetry
   - Activity start/stop with automatic timing
   - Baggage propagation across service boundaries
   - Activity events for key milestones
   - Activity status codes (Ok, Error)

2. **Structured Logging** - ILogger integration
   - Log levels: Trace, Debug, Info, Warning, Error, Critical
   - Structured log properties from method parameters
   - Enumerable expansion with bounds checking
   - Correlation with Activity trace IDs

3. **Metrics Collection** - OpenTelemetry Metrics
   - Counters (manual and auto-incrementing)
   - Histograms for distribution tracking
   - Tagged metrics for dimensional analysis

4. **Dependency Injection** - Generated DI extensions
   - Automatic service registration
   - Scoped lifecycle management
   - Constructor injection

5. **Unit Testing** - Test-friendly generated code
   - Mock telemetry interfaces
   - Validate telemetry calls in tests
   - See `SampleApp.APIService.UnitTests` for examples

## Project Structure

```text
SampleApp/
├── SampleApp.AppHost/              # .NET Aspire orchestrator
│   └── Program.cs                  # Aspire app configuration
├── SampleApp.APIService/           # Weather API backend
│   ├── Endpoints/
│   │   └── WeatherEndpoints.cs    # Minimal API endpoints
│   ├── Services/
│   │   ├── IWeatherService.cs     # Business logic interface
│   │   ├── WeatherService.cs      # Business logic implementation
│   │   └── IWeatherServiceTelemetry.cs  # 🔥 Telemetry interface
│   └── Program.cs                  # API startup
├── SampleApp.Web/                  # Blazor frontend
│   ├── Components/                 # Blazor components
│   └── Program.cs                  # Web startup
├── SampleApp.ServiceDefaults/      # Shared Aspire config
├── SampleApp.Shared/               # Shared DTOs
└── SampleApp.APIService.UnitTests/ # Unit tests with telemetry mocking
```

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [.NET Aspire workload](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling)

Install the Aspire workload:

```bash
dotnet workload install aspire
```

### Running the Application

1. **Start the Aspire App Host:**

   ```bash
   cd samples/SampleApp
   dotnet run --project SampleApp.AppHost
   ```

2. **Access the Applications:**
   - **Aspire Dashboard**: <http://localhost:15888> (or port shown in console)
   - **Web Frontend**: Listed in Aspire dashboard
   - **API Service**: Listed in Aspire dashboard
   - **Scalar API Docs**: Listed in Aspire dashboard

3. **Navigate to the Weather Page** in the web frontend to trigger API calls and generate telemetry.

### Running Tests

Execute the unit tests that demonstrate telemetry mocking:

```bash
cd samples/SampleApp
dotnet test
```

Tests validate business logic while mocking telemetry calls. See `SampleApp.APIService.UnitTests` for examples of:

- Mocking telemetry interfaces
- Verifying telemetry method calls
- Testing with and without telemetry

## Monitoring Telemetry

### Using the Aspire Dashboard

The .NET Aspire dashboard provides a comprehensive view of your application's telemetry:

1. **Traces Tab** - View distributed traces
   - See the full request path through your services
   - Inspect Activity spans, duration, and events
   - View baggage and tags on each span

2. **Metrics Tab** - Monitor counter and histogram values
   - View `ItsTooCold` counter increments
   - Analyze `HistogramOfTemperature` distributions
   - Filter by tags and time ranges

3. **Logs Tab** - Search structured logs
   - Filter by log level (Trace, Debug, Info, Warning, Error)
   - View correlated logs within trace contexts
   - Search by properties and message content

4. **Console Output Tab** - Real-time application output

### Using dotnet-counter

[`dotnet-counter`](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters) is a command-line tool for monitoring .NET metrics in real-time.

#### Use dotnet-counter

Either via `dnx` (recommended):

```bash
dnx dotnet-counters [options]
```

Or install the tool globally:

```bash
dotnet tool install --global dotnet-counters
```

#### Monitor Metrics

1. **Find the Process ID:**

   ```bash
   dnx dotnet-counters ps
   // or
   dotnet-counters ps
   ```

   Look for `SampleApp.APIService` in the output and note its PID.

2. **Monitor All Meters:**

   ```bash
   dnx dotnet-counters monitor --process-id <PID>
   // or
   dotnet-counters monitor --process-id <PID>
   ```

3. **Monitor Specific Meter:**

   The sample app uses a meter named based on the interface. To monitor only weather service metrics:

   ```bash
   dnx dotnet-counters monitor --process-id <PID> --counters SampleApp.APIService.Services.IWeatherServiceTelemetry
   // or
   dotnet-counters monitor --process-id <PID> --counters SampleApp.APIService.Services.IWeatherServiceTelemetry
   ```

4. **Watch Specific Counters:**

   ```bash
   # Monitor the "too cold" counter
   dnx dotnet-counters monitor --process-id <PID> --counters SampleApp.APIService.Services.IWeatherServiceTelemetry[its-too-cold]
   // or
   dotnet-counters monitor --process-id <PID> --counters SampleApp.APIService.Services.IWeatherServiceTelemetry[its-too-cold]

   # Monitor temperature histogram
   dnx dotnet-counters monitor --process-id <PID> --counters SampleApp.APIService.Services.IWeatherServiceTelemetry[histogram-of-temperature]
   // or
   dotnet-counters monitor --process-id <PID> --counters SampleApp.APIService.Services.IWeatherServiceTelemetry[histogram-of-temperature]
   ```

#### Example Output

```plain
Press p to pause, r to resume, q to quit.
    Status: Running

[SampleApp.APIService.Services.IWeatherServiceTelemetry]
    getting-weather-forecast (Count / 1 sec)                    2
    its-too-cold (Count / 1 sec)                               1
    histogram-of-temperature
        Percentile=50                                           15
        Percentile=95                                           28
        Percentile=99                                           30
```

### Using dotnet-trace

Capture detailed trace files for offline analysis:

```bash
# Install dotnet-trace
dotnet tool install --global dotnet-trace

# Collect traces
dotnet-trace collect --process-id <PID> --format <Chromium|NetTrace|Speedscope>
```

### Using OpenTelemetry Collector

For production scenarios, configure the Aspire app to export to an OpenTelemetry Collector, which can forward to:

- **Jaeger** - Distributed tracing visualization
- **Prometheus** - Metrics storage and querying
- **Grafana** - Dashboards and alerting
- **Azure Monitor** / **Application Insights**
- **AWS X-Ray** / **CloudWatch**

See the [.NET Aspire telemetry documentation](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/telemetry) for configuration details.

## Exploring Generated Code

The source generator creates implementation code that you can inspect:

**Location:** `SampleApp.APIService/obj/Debug/net10.0/generated/`

Generated files include:

- `IWeatherServiceTelemetry.Activity.g.cs` - Activity source implementation
- `IWeatherServiceTelemetry.Logging.g.cs` - ILogger implementation  
- `IWeatherServiceTelemetry.Metrics.g.cs` - Metrics implementation
- `IWeatherServiceTelemetry.DI.g.cs` - Dependency injection extensions

**Enable in IDE:** The project already has `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` enabled, so generated files appear in your IDE's file explorer.

## Key Concepts

### Multi-Target Methods

Methods can have multiple telemetry attributes to generate Activity + Log + Metric from a single call:

```csharp
// Before (manual, verbose):
var activity = _activitySource.StartActivity("Getting Weather");
_logger.LogTrace("Getting weather forecast for {Count}", count);
_counter.Add(1, new KeyValuePair<string, object>("operation", "get-weather"));

// After (generated, concise):
var activity = _telemetry.GettingWeatherForecast(baggageInfo, count);
// ↑ Single call: Activity started, Trace logged, Counter incremented
```

### Activity Event Chaining

Add events to the current activity throughout the operation lifecycle:

```csharp
using var activity = telemetry.GettingWeatherForecast(info, count);
// ... do work ...
telemetry.ForecastReceived(activity, minTemp, maxTemp);  // Adds event to activity
// ... more work ...
telemetry.TemperaturesReceived(activity, elapsed);       // Adds another event
```

### Enumerable Expansion

Control how collections are logged:

```csharp
// Expands array items into individual log properties (up to 100 items)
[Info]
void TemperaturesWithinRange([ExpandEnumerable(maximumValueCount: 100)] int[] temperaturesInC);
```

## Learn More

- [Purview Telemetry Documentation](../../README.md) - Full feature documentation
- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) - Cloud-native app development
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/) - Observability framework
- [dotnet-counters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters) - Performance monitoring
- [dotnet-trace](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace) - Trace collection

## Troubleshooting

### "Unable to find project 'SampleApp.AppHost'"

Ensure you're running from the `samples/SampleApp` directory.

### Port Already in Use

If the default ports are in use, Aspire will automatically assign different ports. Check the console output for the actual URLs.

### Telemetry Not Appearing

1. Ensure you're triggering the API by using the web frontend
2. Check the Aspire dashboard's Traces/Logs/Metrics tabs
3. Verify the API service is running (check Aspire dashboard Resources tab)

### Generated Code Not Visible

Ensure `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` is in your `.csproj` file and rebuild the project.
