using Purview.Telemetry;

// Sets the default ActivitySource name used by TelemetryNames.ActivitySourceNames. The meter name is not set
// here, so it falls back to the assembly name.
[assembly: ActivitySourceGeneration("sample-weather-app-net48")]
// Generates the TelemetryNames class. It is opt-in: the generated names are always recorded in
// assembly metadata, and only the project that registers them needs the class.
[assembly: TelemetryGeneration(GenerateTelemetryNamesClass = true)]
// Names the meter, so the reported meter name does not depend on the assembly name.
[assembly: MeterGeneration("sample-weather-app-net48")]
