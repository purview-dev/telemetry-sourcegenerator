namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingTests
{
	[Test]
	public async Task Generate_GivenNoReferenceToILoggerAndNoLoggerRequested_DoesNotGenerateDiagnostic()
	{
		// Arrange
		const string basicActivity =
			@"
using Purview.Telemetry.Activities;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
public interface ITestActivities {
	[Activity]
	System.Diagnostics.Activity? Activity([Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Event]
	void Event(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicActivity,
			includeLoggerTypes: IncludeLoggerTypes.None
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenNoReferenceToILoggerAndNoLoggerRequested_CompilesWithoutILoggerRef()
	{
		// Arrange
		const string basicActivity =
			@"
using Purview.Telemetry.Activities;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
public interface ITestActivities {
	[Activity]
	System.Diagnostics.Activity? Activity([Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Event]
	void Event(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicActivity,
			includeLoggerTypes: IncludeLoggerTypes.None
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			expectsDiagnostics: false,
			whenValidatingDiagnosticsIgnoreNonErrors: true
		);
	}
}
