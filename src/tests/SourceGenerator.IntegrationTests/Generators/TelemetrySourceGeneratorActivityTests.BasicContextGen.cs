using Purview.Telemetry.SourceGenerator.Infra;

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
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
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
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
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
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
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
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
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
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
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
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}
}
