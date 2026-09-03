using System.Diagnostics;
using Purview.SourceGeneratorFramework;

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
		var query = generationResult.Generated();
		var implClass = query.GetClass("TestActivitiesCore", "Testing");
		await Assert
			.That(implClass.HasMethod(query, "Activity", TypeReference.Create<ActivityContext>()))
			.IsTrue()
			.Because("the generated implementation must contain the activity method with an ActivityContext parameter");
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
		var query = generationResult.Generated();
		var implClass = query.GetClass("TestActivitiesCore", "Testing");
		await Assert
			.That(
				implClass.HasMethod(
					query,
					"Activity",
					TypeReference
						.Create<ActivityContext>()
						.Nullable(GenerationSettings.Create<TelemetrySourceGenerator>())
				)
			)
			.IsTrue()
			.Because(
				"the generated implementation must contain the activity method with a nullable ActivityContext parameter"
			);
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
		var query = generationResult.Generated();
		var implClass = query.GetClass("TestActivitiesCore", "Testing");
		await Assert
			.That(implClass.HasMethod(query, "Activity", TypeReference.Create<string>()))
			.IsTrue()
			.Because("the generated implementation must contain the activity method with a parent-id string parameter");
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
		var query = generationResult.Generated();
		var implClass = query.GetClass("TestActivitiesCore", "Testing");
		await Assert
			.That(implClass.HasMethod(query, "Activity", TypeReference.Create<string>()))
			.IsTrue()
			.Because(
				"the generated implementation must contain the activity method with a nullable parent-id string parameter"
			);
	}
}
