using System.Diagnostics;
using Purview.SourceGeneratorFramework;

namespace Purview.Telemetry.SourceGenerator.Activities;

partial class TelemetrySourceGeneratorActivityTests
{
	[Test]
	public async Task Generate_GivenBasicContextGen_GeneratesActivity(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicActivity = """

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Context]
	void Context(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);
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
					"Context",
					TypeReference.Create<Activity>(),
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the context method with its parameter signature");
	}

	[Test]
	public async Task Generate_GivenBasicContextGenWithReturningActivity_GeneratesActivity(
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

	[Context]
	Activity Context(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);
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
					"Context",
					TypeReference.Create<Activity>(),
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the context method");
		await Assert
			.That(implClass.HasMethodReturnType(query, "Context", TypeReference.Create<Activity>()))
			.IsTrue()
			.Because("the context method must return an Activity");
	}

	[Test]
	public async Task Generate_GivenBasicContextGenWithReturningNullableActivity_GeneratesActivity(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicActivity = """

using System.Diagnostics;

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities {
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Context]
	Activity Context(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Context]
	Activity? ContextWithNullableReturnActivity(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);
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
					"Context",
					TypeReference.Create<Activity>(),
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the context method");
		await Assert
			.That(
				implClass.HasMethod(
					query,
					"ContextWithNullableReturnActivity",
					TypeReference.Create<Activity>(),
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the nullable-return context method");
		await Assert
			.That(
				implClass.HasMethodReturnType(
					query,
					"ContextWithNullableReturnActivity",
					TypeReference.Create<Activity>().Nullable(GenerationSettings.Create<TelemetrySourceGenerator>())
				)
			)
			.IsTrue()
			.Because("the nullable-return context method must return a nullable Activity");
	}

	[Test]
	public async Task Generate_GivenBasicContextGenWithNullableParams_GeneratesActivity(
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

	[Context]
	Activity Context(System.Diagnostics.Activity? activity, [Baggage]string? stringParam, [Tag]int? intParam, bool? boolParam);

	[Context]
	Activity? ContextWithNullableParams(System.Diagnostics.Activity? activity, [Baggage]string? stringParam, [Tag]int? intParam, bool? boolParam);
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
					"Context",
					TypeReference.Create<Activity>(),
					TypeReference.Create<string>(),
					TypeReference.Create<int>().Nullable(GenerationSettings.Create<TelemetrySourceGenerator>()),
					TypeReference.Create<bool>().Nullable(GenerationSettings.Create<TelemetrySourceGenerator>())
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the context method with nullable parameters");
		await Assert
			.That(
				implClass.HasMethod(
					query,
					"ContextWithNullableParams",
					TypeReference.Create<Activity>(),
					TypeReference.Create<string>(),
					TypeReference.Create<int>().Nullable(GenerationSettings.Create<TelemetrySourceGenerator>()),
					TypeReference.Create<bool>().Nullable(GenerationSettings.Create<TelemetrySourceGenerator>())
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the nullable-parameter context method");
	}

	[Test]
	public async Task Generate_GivenBasicContextGenWithActivity_GeneratesActivity(CancellationToken cancellationToken)
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

	[Context]
	Activity Context(Activity activityParameter, [Baggage]string? stringParam, [Tag]int? intParam, bool? boolParam);

	[Context]
	Activity? ContextWithNullableParams(Activity? activityParameter, [Baggage]string? stringParam, [Tag]int? intParam, bool? boolParam);
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
					"Context",
					TypeReference.Create<Activity>(),
					TypeReference.Create<string>(),
					TypeReference.Create<int>().Nullable(GenerationSettings.Create<TelemetrySourceGenerator>()),
					TypeReference.Create<bool>().Nullable(GenerationSettings.Create<TelemetrySourceGenerator>())
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the context method");
		await Assert
			.That(
				implClass.HasMethod(
					query,
					"ContextWithNullableParams",
					TypeReference.Create<Activity>(),
					TypeReference.Create<string>(),
					TypeReference.Create<int>().Nullable(GenerationSettings.Create<TelemetrySourceGenerator>()),
					TypeReference.Create<bool>().Nullable(GenerationSettings.Create<TelemetrySourceGenerator>())
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the nullable-activity context method");
	}

	[Test]
	public async Task Generate_GivenBasicContextGenWithActivityAndNoReturn_GeneratesActivity(
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

	[Context]
	void Context(Activity activityParameter, [Baggage]string? stringParam, [Tag]int? intParam, bool? boolParam);

	[Context]
	void ContextWithNullableParams(Activity? activityParameter, [Baggage]string? stringParam, [Tag]int? intParam, bool? boolParam);
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
					"Context",
					TypeReference.Create<Activity>(),
					TypeReference.Create<string>(),
					TypeReference.Create<int>().Nullable(GenerationSettings.Create<TelemetrySourceGenerator>()),
					TypeReference.Create<bool>().Nullable(GenerationSettings.Create<TelemetrySourceGenerator>())
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the void context method");
		await Assert
			.That(
				implClass.HasMethod(
					query,
					"ContextWithNullableParams",
					TypeReference.Create<Activity>(),
					TypeReference.Create<string>(),
					TypeReference.Create<int>().Nullable(GenerationSettings.Create<TelemetrySourceGenerator>()),
					TypeReference.Create<bool>().Nullable(GenerationSettings.Create<TelemetrySourceGenerator>())
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the void nullable-activity context method");
	}
}
