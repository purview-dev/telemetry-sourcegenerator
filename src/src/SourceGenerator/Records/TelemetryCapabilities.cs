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
/// Whether meters are created through an injected <c>IMeterFactory</c>. Probed on the compilation, never on the target
/// framework: <c>IMeterFactory</c> arrives with Microsoft.Extensions.Diagnostics.Abstractions (or the shared framework on
/// .NET 8+), so a .NET Framework project that references that package has it too, and a modern project that pins an
/// older line may not. A consumer with the generated factory-taking constructor must register the factory
/// (<c>services.AddMetrics()</c>).
/// </param>
/// <param name="UseActivitySourceFactory">
/// Whether activity sources are created through an injected <c>System.Diagnostics.ActivitySourceFactory</c>: the
/// assembly opted in with <c>[ActivitySourceGeneration(UseActivitySourceFactory = true)]</c> <em>and</em> the
/// compilation can resolve the type (.NET 11, or System.Diagnostics.DiagnosticSource 11+). Gated on the symbol rather
/// than the target framework because the package brings the type to net462 and netstandard2.0 consumers too. It lives
/// here, rather than on the activity target, because the constructor that receives the factory is emitted by
/// whichever target owns the constructor.
/// </param>
/// <param name="SupportsGauge">
/// Whether the compilation can resolve <c>System.Diagnostics.Metrics.Gauge&lt;T&gt;</c> (.NET 9, or
/// System.Diagnostics.DiagnosticSource 9+ on any target). Gated on the symbol, not the target framework, because an
/// analyzer-only package reference does not bring the type to a consumer that pins an older DiagnosticSource.
/// </param>
/// <param name="SupportsInstrumentAdvice">
/// Whether the compilation can resolve <c>System.Diagnostics.Metrics.InstrumentAdvice&lt;T&gt;</c> (.NET 9, or
/// System.Diagnostics.DiagnosticSource 9+). Gates the histogram bucket boundaries.
/// </param>
sealed record TelemetryCapabilities(
	//bool SupportsNullableAnnotations,
	bool SupportsIMeterFactory,
	bool UseActivitySourceFactory = false,
	bool SupportsGauge = false,
	bool SupportsInstrumentAdvice = false,
	bool SupportsActivityAddException = false,
	string? AssemblyVersion = null
) : IGenerationCapabilities;
