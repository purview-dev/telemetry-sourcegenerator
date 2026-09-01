namespace Purview.Telemetry.SourceGenerator;

/// <summary>
/// Capabilities detected for the current compilation.
/// </summary>
public sealed record TelemetryCapabilities : IGenerationCapabilities
{
	public static readonly TelemetryCapabilities Instance = new();
}
