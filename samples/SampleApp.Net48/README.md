# SampleApp.Net48 — .NET Framework 4.8 Sample

Demonstrates [Purview Telemetry Source Generator](../../README.md) running on **.NET Framework 4.8**
via a simple console application.

## What it shows

- **Multi-target telemetry** — a single interface method generates Activity, Log, and Metric
  instrumentation simultaneously, just like the main .NET Aspire sample.
- **NET48 compatibility** — because `NET48_OR_GREATER` is automatically defined by the SDK when
  targeting `net48`, the injected attribute source files use `string` (not `string?`) and skip the
  `#nullable enable` directive. No extra configuration is needed.
- **Full telemetry stack on net48** — ActivitySource, ILogger, and System.Diagnostics.Metrics all
  work via NuGet packages (`System.Diagnostics.DiagnosticSource`, `Microsoft.Extensions.*`).

## Running the sample

```
cd samples/SampleApp.Net48
dotnet run --project SampleApp.Net48.ConsoleApp --configuration Release
```

### Expected output (abbreviated)

```
=== Purview Telemetry — .NET Framework 4.8 Sample ===
Activity source : sample-weather-app-net48
Meter           : sample-weather-app-net48

--- Request 1 ---
  [TRACE START] GettingWeatherForecast (00-...)
  [METRIC] sample-weather-app-net48/histogram-of-temperature: 42
  ...
  [TRACE STOP]  GettingWeatherForecast — Ok (12.3 ms)
  Got 10 forecast(s). Min: -4°C, Max: 42°C
```

## How NET48 affects template generation

The source generator injects attribute definition files (e.g. `ErrorAttribute.g.cs`) into the
consuming project. These files contain `#if` guards so they compile cleanly on **both** modern
.NET and .NET Framework 4.8:

```csharp
// In the injected ErrorAttribute.g.cs:
#if !NET48_OR_GREATER && !PURVIEW_TELEMETRY_NON_NULLABLE
#nullable enable
#endif

#if NET48_OR_GREATER || PURVIEW_TELEMETRY_NON_NULLABLE
public ErrorAttribute(string messageTemplate = null, string name = null)
#else
public ErrorAttribute(string? messageTemplate = null, string? name = null)
#endif
```

On net48, `NET48_OR_GREATER` is defined → the non-nullable branch is used, `#nullable enable` is
omitted, and the file compiles without requiring C# 8 nullable reference type support.

### Opt-out for any project

Projects that prefer non-nullable attribute files regardless of target framework can define
`PURVIEW_TELEMETRY_NON_NULLABLE` in their project file:

```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);PURVIEW_TELEMETRY_NON_NULLABLE</DefineConstants>
</PropertyGroup>
```

## Project structure

```
SampleApp.Net48/
├── Directory.Build.props              # net48, LangVersion=latest, Nullable=disable
├── SampleApp.Net48.slnx               # Solution file
├── README.md
└── SampleApp.Net48.ConsoleApp/
    ├── SampleApp.Net48.ConsoleApp.csproj
    ├── Program.cs                     # ActivityListener + MeterListener + DI setup
    ├── Properties/
    │   └── AssemblyInfo.cs            # [assembly: ActivitySourceGeneration("...")]
    └── Services/
        ├── IWeatherService.cs         # Business interface + WeatherForecast DTO
        ├── IWeatherServiceTelemetry.cs  # Multi-target telemetry interface
        └── WeatherService.cs          # Implementation consuming telemetry
```

## Packages

| Package | Purpose |
|---|---|
| `System.Diagnostics.DiagnosticSource` | ActivitySource, ActivityListener, Meter, MeterListener |
| `Microsoft.Extensions.DependencyInjection` | Service registration (`AddWeatherServiceTelemetry()`) |
| `Microsoft.Extensions.Logging.Console` | Console log output |
