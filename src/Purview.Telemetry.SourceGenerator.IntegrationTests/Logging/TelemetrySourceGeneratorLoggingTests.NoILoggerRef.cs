namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingTests
{
	[Test]
	public async Task Generate_GivenNoReferenceToILoggerAndNoLoggerRequested_DoesNotGenerateDiagnostic(
		CancellationToken cancellationToken
	)
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
		var generationResult = await GenerateAsync(
			basicActivity,
			includeLoggerTypes: IncludeLoggerTypes.None,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}

	[Test]
	public async Task Generate_GivenNoReferenceToILoggerAndNoLoggerRequested_CompilesWithoutILoggerRef(
		CancellationToken cancellationToken
	)
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
		var generationResult = await GenerateAsync(
			basicActivity,
			includeLoggerTypes: IncludeLoggerTypes.None,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			expectsDiagnostics: false,
			whenValidatingDiagnosticsIgnoreNonErrors: true,
			cancellationToken: cancellationToken
		);
	}
}
