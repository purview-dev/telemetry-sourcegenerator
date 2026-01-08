namespace Purview.Telemetry.SourceGenerator.Activities;

partial class TelemetrySourceGeneratorActivityTests
{
	[Test]
	public async Task Generate_GivenBasicGen_GeneratesActivity()
	{
		// Arrange
		const string basicActivity =
			"""

using Purview.Telemetry.Activities;

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
		var generationResult = await GenerateAsync(basicActivity);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenInterfaceWithNoActivityButOtherActivityBasedMethods_GeneratesDiagnostic()
	{
		// Arrange
		const string basicActivity =
			"""

using Purview.Telemetry.Activities;

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
		var generationResult = await GenerateAsync(basicActivity);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG3012"]
		);
	}

	[Test]
	public async Task Generate_GivenBasicGenAndNoActivityName_GeneratesActivity()
	{
		// Arrange
		const string basicActivity =
			@"
using Purview.Telemetry.Activities;

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
		var generationResult = await GenerateAsync(basicActivity);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenWithNonStringBaggage_RaisesDiagnosticAndGenerates()
	{
		// Arrange
		const string basicActivity =
			"""

using Purview.Telemetry.Activities;

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
		var generationResult = await GenerateAsync(basicActivity);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			s => s.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG3000"]
		);
	}

	[Test]
	public async Task Generate_GivenBasicGenWithReturningActivity_GeneratesActivity()
	{
		// Arrange
		const string basicActivity =
			"""

using Purview.Telemetry.Activities;
using System.Diagnostics;

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities {
	[Activity]
	Activity Activity([Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Event]
	Activity Event(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicActivity);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenBasicGenWithReturningNullableActivity_GeneratesActivity()
	{
		// Arrange
		const string basicActivity =
			"""

using Purview.Telemetry.Activities;
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
		var generationResult = await GenerateAsync(basicActivity);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenBasicGenWithNullableParams_GeneratesActivity()
	{
		// Arrange
		const string basicActivity =
			"""

using Purview.Telemetry.Activities;
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
		var generationResult = await GenerateAsync(basicActivity);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}
}
