using System.Diagnostics;

namespace Purview.Telemetry.SourceGenerator.Infra;

/// <summary>
/// Whether the runtime the tests run on - and so the references the test compilations are built from - provides
/// <c>System.Diagnostics.ActivitySourceFactory</c> (.NET 11). The generator gates the factory on the same symbol.
/// </summary>
static class ActivitySourceFactoryAvailability
{
	public static bool IsAvailable { get; } =
		typeof(ActivitySource).Assembly.GetType("System.Diagnostics.ActivitySourceFactory") is not null;
}

/// <summary>
/// Runs a test only where <c>ActivitySourceFactory</c> exists: the <c>net11.0</c> leg of the test project.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
sealed class RequiresActivitySourceFactoryAttribute : SkipAttribute
{
	public RequiresActivitySourceFactoryAttribute()
		: base("System.Diagnostics.ActivitySourceFactory needs .NET 11; this leg runs an earlier runtime.") { }

	public override Task<bool> ShouldSkip(TestRegisteredContext context) =>
		Task.FromResult(!ActivitySourceFactoryAvailability.IsAvailable);
}

/// <summary>
/// Runs a test only where <c>ActivitySourceFactory</c> does not exist: every leg before <c>net11.0</c>, which is where
/// the fallback (and TSG3023) apply.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
sealed class RequiresNoActivitySourceFactoryAttribute : SkipAttribute
{
	public RequiresNoActivitySourceFactoryAttribute()
		: base("This test covers the fallback for runtimes without System.Diagnostics.ActivitySourceFactory.") { }

	public override Task<bool> ShouldSkip(TestRegisteredContext context) =>
		Task.FromResult(ActivitySourceFactoryAvailability.IsAvailable);
}
