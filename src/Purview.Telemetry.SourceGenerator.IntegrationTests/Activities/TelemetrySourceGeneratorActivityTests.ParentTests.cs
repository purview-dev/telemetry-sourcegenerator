namespace Purview.Telemetry.SourceGenerator.Activities;

partial class TelemetrySourceGeneratorActivityTests
{
	[Test]
	public async Task Generate_GivenActivityContext_GeneratesActivityAndSetsActivityContext()
	{
		// Arrange
		const string basicActivity =
			@"
using Purview.Telemetry.Activities;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
public interface ITestActivities {
	[Activity]
	System.Diagnostics.Activity? Activity(System.Diagnostics.ActivityContext parentContext);
}
";

		// Act
		var generationResult = await GenerateAsync(basicActivity);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenNullableActivityContext_GeneratesActivityAndSetsActivityContextOrDefault()
	{
		// Arrange
		const string basicActivity =
			@"
using Purview.Telemetry.Activities;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
public interface ITestActivities {
	[Activity]
	System.Diagnostics.Activity? Activity(System.Diagnostics.ActivityContext? parentContext);
}
";

		// Act
		var generationResult = await GenerateAsync(basicActivity);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenParentId_GeneratesActivityAndSetsParentId()
	{
		// Arrange
		const string basicActivity =
			@"
using Purview.Telemetry.Activities;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
public interface ITestActivities {
	[Activity]
	System.Diagnostics.Activity? Activity(string parentId);
}
";

		// Act
		var generationResult = await GenerateAsync(basicActivity);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenNullableParentId_GeneratesActivityAndSetsParentId()
	{
		// Arrange
		const string basicActivity =
			@"
using Purview.Telemetry.Activities;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
public interface ITestActivities {
	[Activity]
	System.Diagnostics.Activity? Activity(string? parentId);
}
";

		// Act
		var generationResult = await GenerateAsync(basicActivity);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}
}
