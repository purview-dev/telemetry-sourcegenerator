namespace Purview.Telemetry.SourceGenerator.Records;

/// <summary>
/// Capabilities detected for the current compilation.
/// </summary>
/// <remarks>
/// Internal because it implements a <c>Purview.SourceGeneratorFramework</c> interface, which the merge
/// internalizes in the shipped analyzer (seen by <c>PSGFR41</c>); the BuildSdk grants the test
/// assemblies access to internals.
/// </remarks>
sealed record TelemetryCapabilities(
	//bool SupportsNullableAnnotations,
	bool SupportsIMeterFactory
) : IGenerationCapabilities;
