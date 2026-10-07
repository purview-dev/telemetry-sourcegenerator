using Purview.Telemetry;

// Generates the TelemetryNames class, aggregating this project's generated names with the names
// recorded by every referenced assembly. It is opt-in: the generated names are always recorded in
// assembly metadata, and only the project that registers them with OpenTelemetry needs the class.
[assembly: TelemetryGeneration(GenerateTelemetryNamesClass = true)]
