namespace Purview.Telemetry.SourceGenerator.Activities;

partial class TelemetrySourceGeneratorActivityTests
{
	[Test]
	public async Task Generate_GivenActivityContext_GeneratesActivityAndSetsActivityContext(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicActivity = """


namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities {
	[Activity]
	System.Diagnostics.Activity? Activity(System.Diagnostics.ActivityContext parentContext);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicActivity, cancellationToken: cancellationToken);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}

	[Test]
	public async Task Generate_GivenNullableActivityContext_GeneratesActivityAndSetsActivityContextOrDefault(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicActivity = """


namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities {
	[Activity]
	System.Diagnostics.Activity? Activity(System.Diagnostics.ActivityContext? parentContext);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicActivity, cancellationToken: cancellationToken);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}

	[Test]
	public async Task Generate_GivenParentId_GeneratesActivityAndSetsParentId(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicActivity = """


namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities {
	[Activity]
	System.Diagnostics.Activity? Activity(string parentId);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicActivity, cancellationToken: cancellationToken);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}

	[Test]
	public async Task Generate_GivenNullableParentId_GeneratesActivityAndSetsParentId(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicActivity = """


namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities {
	[Activity]
	System.Diagnostics.Activity? Activity(string? parentId);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicActivity, cancellationToken: cancellationToken);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}
}
