using Purview.SourceGeneratorFramework.Testing.TUnit;
using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator;

/// <summary>
/// Base class for generator integration tests. The framework's <c>GenerateAsync</c>
/// is used directly; custom behaviour is expressed via <see cref="TelemetrySourceGeneratorTestOptions"/>
/// instances passed per call (e.g. <see cref="TelemetrySourceGeneratorTestOptions.NoValidation"/>).
/// </summary>
public abstract class IncrementalSourceGeneratorTestBase<TGenerator>
	: TUnitSourceGeneratorTestBase<TGenerator, TelemetrySourceGeneratorTestOptions>
	where TGenerator : class, IIncrementalGenerator, new()
{
	/// <summary>
	/// Options that keep the dependency-injection extension generated. The default options
	/// disable it, so tests that assert DI output pass these instead.
	/// </summary>
	protected static TelemetrySourceGeneratorTestOptions GenerateDependencyInjection() =>
		new() { AdditionalSources = [] };
}
