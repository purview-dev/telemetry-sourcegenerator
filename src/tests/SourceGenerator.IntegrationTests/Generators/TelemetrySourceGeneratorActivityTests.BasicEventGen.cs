using System.Diagnostics;
using Purview.SourceGeneratorFramework;

namespace Purview.Telemetry.SourceGenerator.Activities;

partial class TelemetrySourceGeneratorActivityTests
{
	[Test]
	[Arguments("Activity")]
	[Arguments("Activity?")]
	[Arguments("System.Diagnostics.Activity")]
	[Arguments("System.Diagnostics.Activity?")]
	public async Task Generate_GivenEventWithFirstParameterAsActivityAndNoEventAttribute_GeneratesEventByInference(
		string activityType,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var basicActivity = $$"""

using System.Diagnostics;

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	void ThisIsAMethod({{activityType}} activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicActivity, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var implClass = query.GetClass("TestActivitiesCore", "Testing");
		await Assert
			.That(implClass.HasMethod(query, "Activity"))
			.IsTrue()
			.Because("the generated implementation must contain the activity method");
		await Assert
			.That(
				implClass.HasMethod(
					query,
					"ThisIsAMethod",
					TypeReference.Create<Activity>(),
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the inferred event method");
	}

	[Test]
	public async Task Generate_GivenBasicEventWithActivityParameter_GeneratesEvent(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicActivity = """

using System.Diagnostics;

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Event]
	void Event(Activity activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicActivity, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var implClass = query.GetClass("TestActivitiesCore", "Testing");
		await Assert
			.That(implClass.HasMethod(query, "Activity"))
			.IsTrue()
			.Because("the generated implementation must contain the activity method");
		await Assert
			.That(
				implClass.HasMethod(
					query,
					"Event",
					TypeReference.Create<Activity>(),
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the event method with its parameter signature");
	}

	[Test]
	public async Task Generate_GivenBasicEventWithNullableActivityParameter_GeneratesEvent(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicActivity = """

using System.Diagnostics;

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Event]
	void Event(Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicActivity, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var implClass = query.GetClass("TestActivitiesCore", "Testing");
		await Assert
			.That(implClass.HasMethod(query, "Activity"))
			.IsTrue()
			.Because("the generated implementation must contain the activity method");
		await Assert
			.That(
				implClass.HasMethod(
					query,
					"Event",
					TypeReference.Create<Activity>(),
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the event method with a nullable activity parameter");
	}

	[Test]
	public async Task Generate_GivenBasicEventStatusCodeParameterSetToOk_GeneratesEvent(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicActivity = """

using System.Diagnostics;

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Event(ActivityStatusCode.Ok)]
	void Event(Activity? activity);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicActivity, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var implClass = query.GetClass("TestActivitiesCore", "Testing");
		await Assert
			.That(implClass.HasMethod(query, "Activity"))
			.IsTrue()
			.Because("the generated implementation must contain the activity method");
		await Assert
			.That(implClass.HasMethod(query, "Event", TypeReference.Create<Activity>()))
			.IsTrue()
			.Because("the generated implementation must contain the event method");
	}

	[Test]
	public async Task Generate_GivenBasicEventStatusCodeParameterSetToError_GeneratesEvent(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicActivity = """

using System.Diagnostics;

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Event(ActivityStatusCode.Error)]
	void Event(Activity? activity);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicActivity, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var implClass = query.GetClass("TestActivitiesCore", "Testing");
		await Assert
			.That(implClass.HasMethod(query, "Activity"))
			.IsTrue()
			.Because("the generated implementation must contain the activity method");
		await Assert
			.That(implClass.HasMethod(query, "Event", TypeReference.Create<Activity>()))
			.IsTrue()
			.Because("the generated implementation must contain the error-status event method");
	}

	[Test]
	public async Task Generate_GivenBasicEventStatusCodeParameterSetToErrorWithException_GeneratesEvent(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicActivity = """

using System.Diagnostics;

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Event(ActivityStatusCode.Error)]
	void Event(Activity? activity, Exception exception);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicActivity, cancellationToken: cancellationToken);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG3021");
	}

	[Test]
	public async Task Generate_GivenBasicEventStatusCodeParameterSetToErrorWithStatusDescriptionOnEventAttribute_GeneratesEvent(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicActivity = """

using System.Diagnostics;

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Event(ActivityStatusCode.Error, StatusDescription = "This is a Test")]
	void Event(Activity? activity, Exception exception);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicActivity, cancellationToken: cancellationToken);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG3021");
	}

	[Test]
	public async Task Generate_GivenBasicEventStatusCodeParameterSetToErrorWithStatusDescriptionOnParameter_GeneratesEvent(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicActivity = """

using System.Diagnostics;

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Event(ActivityStatusCode.Error)]
	void Event(Activity? activity, [StatusDescription]string? statusDescription);

	[Event(ActivityStatusCode.Error)]
	void Event2(Activity? activity, [StatusDescription]string statusDescription_another);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicActivity, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var implClass = query.GetClass("TestActivitiesCore", "Testing");
		await Assert
			.That(implClass.HasMethod(query, "Event", TypeReference.Create<Activity>(), TypeReference.Create<string>()))
			.IsTrue()
			.Because("the generated implementation must contain the first status-description event method");
		await Assert
			.That(
				implClass.HasMethod(query, "Event2", TypeReference.Create<Activity>(), TypeReference.Create<string>())
			)
			.IsTrue()
			.Because("the generated implementation must contain the second status-description event method");
	}
}
