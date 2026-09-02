using System.Runtime.InteropServices;

namespace Purview.Telemetry.SourceGenerator.Infra;

/// <summary>
/// Skips a test (or test class) when running on .NET Framework, where APIs such as
/// <c>System.Diagnostics.Metrics</c> are unavailable.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
sealed class SkipOnNetFrameworkAttribute : SkipAttribute
{
	public SkipOnNetFrameworkAttribute()
		: base("This test requires .NET 6+ APIs that are unavailable on .NET Framework.") { }

	public override Task<bool> ShouldSkip(TestRegisteredContext context) =>
		Task.FromResult(RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.Ordinal));
}
