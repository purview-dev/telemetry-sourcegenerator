using Microsoft.CodeAnalysis.CSharp;

namespace Purview.Telemetry.SourceGenerator.Generators;

/// <summary>
/// The marker attributes switch between <c>string</c> and <c>string?</c> on <c>NET48_OR_GREATER</c>
/// alone. They read the post-initialization output directly, because the generator test options
/// exclude the marker attributes from the generated-source queries.
/// </summary>
public class MarkerAttributeNullabilityTests
{
	[Test]
	public async Task Generate_GivenAnyCompilation_GuardsMarkerNullabilityOnNet48Only(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string hintName = "ActivityAttribute.g.cs";

		// Act
		var activityAttribute = GetMarkerSource(hintName, cancellationToken);

		// Assert
		await Assert.That(activityAttribute).Contains("#if !NET48_OR_GREATER");
		await Assert.That(activityAttribute).Contains("#if NET48_OR_GREATER");
		await Assert.That(activityAttribute).DoesNotContain("PURVIEW_TELEMETRY_NON_NULLABLE");
	}

	static string GetMarkerSource(string hintName, CancellationToken cancellationToken)
	{
		var compilation = CSharpCompilation.Create(
			"MarkerAttributeNullabilityTests",
			[CSharpSyntaxTree.ParseText("namespace Testing;", cancellationToken: cancellationToken)],
			[MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);

		var runResult = CSharpGeneratorDriver
			.Create(new TelemetrySourceGenerator())
			.RunGenerators(compilation, cancellationToken)
			.GetRunResult();

		return runResult
			.Results.SelectMany(static result => result.GeneratedSources)
			.Single(source => string.Equals(source.HintName, hintName, StringComparison.Ordinal))
			.SourceText.ToString();
	}
}
