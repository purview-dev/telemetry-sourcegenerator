namespace Purview.Telemetry.SourceGenerator;

partial class TelemetrySourceGeneratorTests
{
	[Test]
	public async Task Generate_GivenDuplicateActivityMethodNames_GeneratesDiagnostic()
	{
		// Arrange
		const string basicTelemetry =
			@"
using Purview.Telemetry.Activities;

[ActivitySource(""activity-source"")]
public interface ITestTelemetry
{
	[Activity]
	System.Diagnostics.Activity? DuplicateMethodName([Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Activity]
	System.Diagnostics.Activity? DuplicateMethodName();
}
";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG1003"]
		);
	}

	[Test]
	public async Task Generate_GivenDuplicateActivityEventContextMethodNames_GeneratesDiagnostic()
	{
		// Arrange
		const string basicTelemetry =
			@"
using Purview.Telemetry.Activities;

[ActivitySource(""activity-source"")]
public interface ITestTelemetry
{
	[Activity]
	System.Diagnostics.Activity? DuplicateMethodName([Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Event]
	void DuplicateMethodName(System.Diagnostics.Activity? activity, string stringParam);

	[Context]
	void DuplicateMethodName(System.Diagnostics.Activity? activity, int intParam);
}
";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG1003"]
		);
	}

	[Test]
	[Arguments(IncludeLoggerTypes.LoggerOnly)]
	[Arguments(IncludeLoggerTypes.Telemetry)]
	public async Task Generate_GivenDuplicateLoggingMethodNames_GeneratesDiagnostic(
		IncludeLoggerTypes includeLoggerTypes
	)
	{
		// Arrange
		const string basicTelemetry =
			@"
using Purview.Telemetry.Logging;

[Logger]
public interface ITestTelemetry
{
	[Log]
	IDisposable? DuplicateMethodName(string stringParam, int intParam, bool boolParam);

	[Log]
	void DuplicateMethodName();
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicTelemetry,
			includeLoggerTypes: includeLoggerTypes
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG1003"],
			parameters: [includeLoggerTypes]
		);
	}

	[Test]
	public async Task Generate_GivenDuplicateMetricsMethodNames_GeneratesDiagnostic()
	{
		// Arrange
		const string basicTelemetry =
			@"
using Purview.Telemetry.Metrics;

[Meter]
public interface ITestTelemetry
{
	[AutoCounter]
	void DuplicateMethodName(string stringParam, int intParam, bool boolParam);

	[Counter]
	void DuplicateMethodName(int measurementValue);
}
";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG1003"]
		);
	}

	[Test]
	[Arguments(IncludeLoggerTypes.LoggerOnly)]
	[Arguments(IncludeLoggerTypes.Telemetry)]
	public async Task Generate_GivenDuplicateMultiTargetMethodNames_GeneratesDiagnostic(
		IncludeLoggerTypes includeLoggerType
	)
	{
		// Arrange
		const string basicTelemetry =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;
using Purview.Telemetry.Metrics;

[ActivitySource(""activity-source"")]
[Logger]
[Meter]
public interface ITestTelemetry
{
	[Activity]
	System.Diagnostics.Activity? DuplicateMethodName([Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Event]
	void DuplicateMethodName(System.Diagnostics.Activity? activity, string stringParam);

	[Context]
	void DuplicateMethodName(System.Diagnostics.Activity? activity, int intParam);

	[Log]
	IDisposable? DuplicateMethodName(string stringParam, int intParam, object objectParam);

	[Log]
	void DuplicateMethodName();

	[AutoCounter]
	void DuplicateMethodName(string stringParam, int intParam, uint uintParam);

	[Counter]
	void DuplicateMethodName(int measurementValue);
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicTelemetry,
			includeLoggerTypes: includeLoggerType
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG1003"],
			parameters: [includeLoggerType]
		);
	}
}
