namespace Purview.Telemetry.SourceGenerator.Records;

/// <summary>
/// Capabilities detected for the current compilation.
/// </summary>
public sealed record TelemetryCapabilities(
	//bool SupportsNullableAnnotations,
	bool SupportsIMeterFactory
) : IGenerationCapabilities;
