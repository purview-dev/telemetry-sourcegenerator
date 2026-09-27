using System.Diagnostics;
using System.Globalization;
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
			.That(implClass.HasMethod("Activity"))
			.IsTrue()
			.Because("the generated implementation must contain the activity method");
		await Assert
			.That(
				implClass.HasMethod(
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
			.That(implClass.HasMethod("Activity"))
			.IsTrue()
			.Because("the generated implementation must contain the activity method");
		await Assert
			.That(
				implClass.HasMethod(
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
			.That(implClass.HasMethod("Activity"))
			.IsTrue()
			.Because("the generated implementation must contain the activity method");
		await Assert
			.That(
				implClass.HasMethod(
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
			.That(implClass.HasMethod("Activity"))
			.IsTrue()
			.Because("the generated implementation must contain the activity method");
		await Assert
			.That(implClass.HasMethod("Event", TypeReference.Create<Activity>()))
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
			.That(implClass.HasMethod("Activity"))
			.IsTrue()
			.Because("the generated implementation must contain the activity method");
		await Assert
			.That(implClass.HasMethod("Event", TypeReference.Create<Activity>()))
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
			.That(implClass.HasMethod("Event", TypeReference.Create<Activity>(), TypeReference.Create<string>()))
			.IsTrue()
			.Because("the generated implementation must contain the first status-description event method");
		await Assert
			.That(implClass.HasMethod("Event2", TypeReference.Create<Activity>(), TypeReference.Create<string>()))
			.IsTrue()
			.Because("the generated implementation must contain the second status-description event method");
	}

	[Test]
	[Arguments("\"exception\"")]
	[Arguments("name: \"exception\"")]
	[Arguments("Name = \"exception\"")]
	public async Task Generate_GivenEventWithExceptionAndOpenTelemetryName_DoesNotRaiseTSG3021(
		string eventAttributeArguments,
		CancellationToken cancellationToken
	)
	{
		// Arrange: the event name can be set through the constructor (positionally or by name) or the
		// Name property; all three forms must resolve to the OpenTelemetry standard name.
		var basicActivity = $$"""

using System.Diagnostics;

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Event({{eventAttributeArguments}})]
	void FailedToRetrieveRepositories(Activity? activity, Exception exception);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicActivity, cancellationToken: cancellationToken);

		// Assert
		await Assert.That(generationResult).DoesNotHaveDiagnostic("TSG3021");
	}

	[Test]
	public async Task Generate_GivenEventWithExceptionAndNameOnLoggingAttribute_RaisesTSG3021WithLogEntryHint(
		CancellationToken cancellationToken
	)
	{
		// Arrange: 'Name' on a logging attribute renames the log entry, not the activity event.
		const string basicTelemetry = """

using System.Diagnostics;

namespace Testing;

[ActivitySource("testing-activity-source")]
[Logger]
public interface ITestTelemetry
{
	[Activity]
	[Info]
	System.Diagnostics.Activity? Activity();

	[Event]
	[Error(Name = "exception")]
	void FailedToRetrieveRepositories(Activity? activity, Exception exception);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry, cancellationToken: cancellationToken);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG3021");

		var message = generationResult
			.AnalyzerResult!.Diagnostics.Single(diagnostic => diagnostic.Id == "TSG3021")
			.GetMessage(CultureInfo.InvariantCulture);

		await Assert.That(message).Contains("The Name on [Error] renames the log entry, not the activity event");
	}

	[Test]
	public async Task Generate_GivenEventWithDisabledOTelExceptionRules_DoesNotRaiseTSG3021(
		CancellationToken cancellationToken
	)
	{
		// Arrange: with the rules disabled the exception becomes an ordinary tag, so the
		// OpenTelemetry standard event name is not required.
		const string basicTelemetry = """

using System.Diagnostics;

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Event(UseRecordExceptionRules = false)]
	void FailedToRetrieveRepositories(Activity? activity, Exception exception);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry, cancellationToken: cancellationToken);

		// Assert
		await Assert.That(generationResult).DoesNotHaveDiagnostic("TSG3021");
	}

	[Test]
	public async Task Generate_GivenEventWithBaggageException_DoesNotRaiseTSG3021(CancellationToken cancellationToken)
	{
		// Arrange: a baggage exception is set as baggage, never recorded as an exception event.
		const string basicTelemetry = """

using System.Diagnostics;

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Event]
	void FailedToRetrieveRepositories(Activity? activity, [Baggage] Exception exception);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry, cancellationToken: cancellationToken);

		// Assert
		await Assert.That(generationResult).DoesNotHaveDiagnostic("TSG3021");
	}

	[Test]
	public async Task Generate_GivenEventWithExceptionExcludedFromActivities_DoesNotRaiseTSG3021OrRecordException(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicTelemetry = """

using System.Diagnostics;

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Event]
	void FailedToRetrieveRepositories(Activity? activity, [ExcludeTargets(Targets.Activities)] Exception exception);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry, cancellationToken: cancellationToken);

		// Assert
		await Assert.That(generationResult).DoesNotHaveDiagnostic("TSG3021");

		var implClass = generationResult.Generated().GetClass("TestActivitiesCore", "Testing");
		await Assert
			.That(
				implClass.HasMethod(
					"FailedToRetrieveRepositories",
					TypeReference.Create<Activity>(),
					TypeReference.Create<Exception>()
				)
			)
			.IsTrue()
			.Because("a parameter excluded from the Activities target stays in the generated signature");

		await Assert
			.That(generationResult.GetSource("TestActivitiesCore.Activity.g.cs"))
			.DoesNotContain("RecordExceptionInternal(activity:")
			.Because("an exception excluded from the Activities target must not be recorded on the Activity");
	}

	[Test]
	public async Task Generate_GivenEventWithTagExcludedFromActivities_DoesNotSetExcludedTag(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicTelemetry = """

using System.Diagnostics;

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Event]
	void Event(Activity? activity, [Tag] string kept, [ExcludeTargets(Targets.Activities)] string dropped);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry, cancellationToken: cancellationToken);

		// Assert
		await Assert
			.That(generationResult.GetSource("TestActivitiesCore.Activity.g.cs"))
			.ContainsGeneratedCode("\"kept\"");

		await Assert
			.That(generationResult.GetSource("TestActivitiesCore.Activity.g.cs"))
			.DoesNotContain("\"dropped\"")
			.Because("a parameter excluded from the Activities target must not be set as a tag");
	}
}
