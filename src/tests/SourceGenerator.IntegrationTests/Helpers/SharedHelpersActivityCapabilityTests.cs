using Microsoft.CodeAnalysis.CSharp;

namespace Purview.Telemetry.SourceGenerator.Helpers;

/// <summary>
/// Proves the <c>Activity.AddException</c> capability probe on both sides: a compilation that references
/// System.Diagnostics.DiagnosticSource resolves the method, and one that does not reports it unavailable so the
/// emitter keeps its hand-built event.
/// </summary>
public class SharedHelpersActivityCapabilityTests
{
	[Test]
	public async Task IsActivityAddExceptionAvailable_GivenNoDiagnosticSource_ReturnsFalse()
	{
		// Arrange: an empty reference set cannot resolve System.Diagnostics.Activity at all.
		var compilation = CSharpCompilation.Create(
			"ProbeAssembly",
			[SyntaxFactory.ParseSyntaxTree("internal sealed class Probe { }")],
			[],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);

		// Act
		var available = SharedHelpers.IsActivityAddExceptionAvailable(compilation);

		// Assert
		await Assert.That(available).IsFalse();
	}

	[Test]
	public async Task IsActivityAddExceptionAvailable_GivenDiagnosticSource_ReturnsTrue()
	{
		// Arrange
		var compilation = CSharpCompilation.Create(
			"ProbeAssembly",
			[SyntaxFactory.ParseSyntaxTree("internal sealed class Probe { }")],
			[MetadataReference.CreateFromFile(typeof(System.Diagnostics.Activity).Assembly.Location)],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);

		// Act
		var available = SharedHelpers.IsActivityAddExceptionAvailable(compilation);

		// Assert
		await Assert.That(available).IsTrue();
	}
}
