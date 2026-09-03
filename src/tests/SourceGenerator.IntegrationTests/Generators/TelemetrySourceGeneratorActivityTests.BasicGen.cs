using System.Diagnostics;
using Purview.SourceGeneratorFramework;
using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator.Activities;

partial class TelemetrySourceGeneratorActivityTests
{
	[Test]
	public async Task Generate_GivenBasicGen_GeneratesActivity(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicActivity = """


namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities {
	[Activity]
	System.Diagnostics.Activity? Activity([Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Event]
	void Event(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);
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
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the activity method with its parameter signature");
		await Assert
			.That(
				implClass.HasMethodReturnType(
					query,
					"Activity",
					TypeReference.Create<Activity>().Nullable(GenerationSettings.Create<TelemetrySourceGenerator>())
				)
			)
			.IsTrue()
			.Because("the activity method must return a nullable Activity");
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
	public async Task Generate_GivenInterfaceWithNoActivityButOtherActivityBasedMethods_GeneratesDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicActivity = """


namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities {
	[Context]
	void Context(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Event]
	void Event(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicActivity,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG3012");
	}

	[Test]
	public async Task Generate_GivenBasicGenAndNoActivityName_GeneratesActivity(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicActivity =
			@"
namespace Testing;

[ActivitySource]
public interface ITestActivities
{
	[Activity]
	System.Diagnostics.Activity? Activity([Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Event]
	void Event(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);
}
";

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
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
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
			.Because("the generated implementation must contain the event method");
	}

	[Test]
	public async Task Generate_GivenWithNonStringBaggage_RaisesDiagnosticAndGenerates(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicActivity = """


namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities {
	[Activity]
	System.Diagnostics.Activity?  Activity([Baggage]string stringNonNullParam, [Baggage]int intParam, [Baggage]bool boolParam);

	[Event]
	void Event(System.Diagnostics.Activity? activity, [Baggage]string? stringNullableParam, [Baggage]int? intParam, [Baggage]bool? boolParam);

	[Context]
	void Context(System.Diagnostics.Activity? activity, [Baggage]object? objectParam, [Baggage]string stringNonNullParam, [Baggage]float? floatParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicActivity,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG3000");
	}

	[Test]
	public async Task Generate_GivenBasicGenWithReturningActivity_GeneratesActivity(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicActivity = """
using System.Diagnostics;

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities {
	[Activity]
	Activity Activity([Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Event]
	void Event(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicActivity,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG3022");
	}

	[Test]
	public async Task Generate_GivenBasicGenWithReturningNullableActivity_GeneratesActivity(
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
	Activity Activity([Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Activity]
	Activity? ActivityWithNullableReturnActivity([Baggage]string stringParam, [Tag]int intParam, bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicActivity,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG3022");
	}

	[Test]
	public async Task Generate_GivenBasicGenWithNullableParams_GeneratesActivity(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicActivity = """

using System.Diagnostics;

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities {
	[Activity]
	Activity? Activity([Baggage]string? stringParam, [Tag]int? intParam, bool? boolParam);

	[Activity]
	Activity? ActivityWithNullableParams([Baggage]string? stringParam, [Tag]int? intParam, bool? boolParam);
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
					TypeReference.Create<string>(),
					TypeReference.Create<int>().Nullable(GenerationSettings.Create<TelemetrySourceGenerator>()),
					TypeReference.Create<bool>().Nullable(GenerationSettings.Create<TelemetrySourceGenerator>())
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the activity method with nullable parameters");
		await Assert
			.That(
				implClass.HasMethod(
					query,
					"ActivityWithNullableParams",
					TypeReference.Create<string>(),
					TypeReference.Create<int>().Nullable(GenerationSettings.Create<TelemetrySourceGenerator>()),
					TypeReference.Create<bool>().Nullable(GenerationSettings.Create<TelemetrySourceGenerator>())
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the nullable-parameter activity method");
	}

	[Test]
	public async Task Generate_GivenNonNullableActivityReturnType_RaisesDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicActivity = """

using System.Diagnostics;

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities {
	[Activity]
	Activity Activity([Baggage]string stringParam, [Tag]int intParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicActivity,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG3022");
	}
}
