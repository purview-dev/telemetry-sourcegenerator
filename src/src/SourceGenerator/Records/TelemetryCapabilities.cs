namespace Purview.Telemetry.SourceGenerator.Records;

/// <summary>
/// Capabilities detected for the current compilation.
/// </summary>
/// <remarks>
/// Internal because it implements a <c>Purview.SourceGeneratorFramework</c> interface, which the merge
/// internalizes in the shipped analyzer (seen by <c>PSGFR41</c>); the BuildSdk grants the test
/// assemblies access to internals.
/// </remarks>
/// <param name="SupportsIMeterFactory">
/// Whether meters are created through an injected <c>IMeterFactory</c> (.NET 8+; never .NET Framework).
/// </param>
/// <param name="UseActivitySourceFactory">
/// Whether activity sources are created through an injected <c>System.Diagnostics.ActivitySourceFactory</c>: the
/// assembly opted in with <c>[ActivitySourceGeneration(UseActivitySourceFactory = true)]</c> <em>and</em> the
/// compilation can resolve the type (.NET 11, or System.Diagnostics.DiagnosticSource 11+). Gated on the symbol rather
/// than the target framework because the package brings the type to net462 and netstandard2.0 consumers too. It lives
/// here, rather than on the activity target, because the constructor that receives the factory is emitted by
/// whichever target owns the constructor.
/// </param>
sealed record TelemetryCapabilities(
	//bool SupportsNullableAnnotations,
	bool SupportsIMeterFactory,
	bool UseActivitySourceFactory = false
) : IGenerationCapabilities;
