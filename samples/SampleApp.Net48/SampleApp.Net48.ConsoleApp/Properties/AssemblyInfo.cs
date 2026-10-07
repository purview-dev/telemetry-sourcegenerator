using Purview.Telemetry;

// Sets the default ActivitySource name used by TelemetryNames.ActivitySourceNames.
[assembly: ActivitySourceGeneration("sample-weather-app-net48")]
// Generates the TelemetryNames class. It is opt-in: the generated names are always recorded in
// assembly metadata, and only the project that registers them needs the class.
[assembly: TelemetryGeneration(GenerateTelemetryNamesClass = true)]
