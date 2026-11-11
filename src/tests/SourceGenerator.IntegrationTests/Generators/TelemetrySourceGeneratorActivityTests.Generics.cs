namespace Purview.Telemetry.SourceGenerator.Activities;

partial class TelemetrySourceGeneratorActivityTests
{
	[Test]
	public async Task Generate_GivenGenericActivityMethod_RecordsTheTypeParameterAsAValue(
		CancellationToken cancellationToken
	)
	{
		// Arrange: StartActivity accepts object? tags, so a generic parameter is recorded as its value.
		const string source = """
			namespace Testing;

			[ActivitySource("testing-activity-source")]
			public interface ITestActivities
			{
				[Activity]
				System.Diagnostics.Activity? Start<T>(T value) where T : struct;
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		await Assert.That(generationResult).DoesNotHaveDiagnostic("TSG1005");
		var generated = generationResult.GetSource("TestActivitiesCore.Activity.g.cs");
		await Assert
			.That(generated)
			.Contains("Start<T>(T value)")
			.Because("the generated activity method mirrors the interface's type parameter");
		await Assert.That(generated).Contains("where T : struct");
	}
}
