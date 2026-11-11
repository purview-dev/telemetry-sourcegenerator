namespace Purview.Telemetry.SourceGenerator.Activities;

partial class TelemetrySourceGeneratorActivityTests
{
	[Test]
	public async Task Generate_GivenGenericActivityInterface_GeneratesAGenericClass(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			namespace Testing;

			[ActivitySource("testing-activity-source")]
			public interface ITestActivities<T> where T : struct
			{
				[Activity]
				System.Diagnostics.Activity? Start(T value);
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		await Assert.That(generationResult).DoesNotHaveDiagnostic("TSG1004");
		var generated = generationResult.GetSource("TestActivitiesCore.Activity.g.cs");
		await Assert.That(generated).Contains("class TestActivitiesCore<T>");
		await Assert.That(generated).Contains("where T : struct");
	}
}
