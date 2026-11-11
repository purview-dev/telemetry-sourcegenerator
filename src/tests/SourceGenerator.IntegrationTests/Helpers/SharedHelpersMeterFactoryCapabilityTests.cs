using Microsoft.CodeAnalysis.CSharp;

namespace Purview.Telemetry.SourceGenerator.Helpers;

/// <summary>
/// Proves the <c>IMeterFactory</c> capability probe on both sides. The probe exists precisely so the generated
/// constructor follows the referenced packages rather than the target framework: a .NET Framework project that
/// references Microsoft.Extensions.Diagnostics.Abstractions has the type, and a modern one pinned to an older line
/// may not. These tests assert the type is what decides, which a define could not.
/// </summary>
public class SharedHelpersMeterFactoryCapabilityTests
{
	[Test]
	public async Task IsMeterFactoryAvailable_GivenNoDiagnosticsAbstractions_ReturnsFalse()
	{
		// Arrange: an empty reference set resolves neither IMeterFactory nor System.Diagnostics.Metrics.
		var compilation = CSharpCompilation.Create(
			"ProbeAssembly",
			[SyntaxFactory.ParseSyntaxTree("internal sealed class Probe { }")],
			[],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);

		// Act
		var available = SharedHelpers.IsMeterFactoryAvailable(compilation);

		// Assert
		await Assert.That(available).IsFalse();
	}

	[Test]
	public async Task IsMeterFactoryAvailable_GivenDiagnosticsAbstractions_ReturnsTrue()
	{
		// Arrange
		var compilation = CSharpCompilation.Create(
			"ProbeAssembly",
			[SyntaxFactory.ParseSyntaxTree("internal sealed class Probe { }")],
			[MetadataReference.CreateFromFile(typeof(System.Diagnostics.Metrics.IMeterFactory).Assembly.Location)],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);

		// Act
		var available = SharedHelpers.IsMeterFactoryAvailable(compilation);

		// Assert
		await Assert.That(available).IsTrue();
	}
}
