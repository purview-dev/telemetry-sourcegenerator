using Purview.Telemetry;

// This is the only project in the solution that opts into the TelemetryNames class. Every other
// project records its generated names in assembly metadata automatically, and this one aggregates
// them — its own, its direct references' and its transitive references' — into a single pair of
// arrays. AggregateReferencedTelemetryNames defaults to true, so no further configuration is needed.
[assembly: TelemetryGeneration(GenerateTelemetryNamesClass = true)]
