namespace Purview.Telemetry.SourceGenerator.Logging;

public partial class TelemetrySourceGeneratorLoggingGen2Tests
{
	[Test]
	public async Task Generate_GivenV2InterfaceWithPerMethodV1Override_GeneratesV1StyleForOverriddenMethod(
		CancellationToken cancellationToken
	)
	{
		// Arrange: v2 interface (default), one method forces v1 via DisableMSLoggingTelemetryGeneration
		const string source =
			@"
namespace Testing;

[Logger]
public interface ITestLogger {
	void RegularV2LogEntry(int value);

	[Log(DisableMSLoggingTelemetryGeneration = true)]
	void HotPathV1LogEntry(int value);
}
";

		// Act
		var generationResult = await GenerateAsync(
			source,
			includeLoggerTypes: IncludeLoggerTypes.Telemetry,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}

	[Test]
	public async Task Generate_GivenV2InterfaceWithSpecificAttributeV1Override_GeneratesV1StyleForOverriddenMethod(
		CancellationToken cancellationToken
	)
	{
		// Arrange: using a specific level attribute with DisableMSLoggingTelemetryGeneration
		const string source =
			@"
namespace Testing;

[Logger]
public interface ITestLogger {
	void RegularV2LogEntry(string message);

	[Debug(DisableMSLoggingTelemetryGeneration = true)]
	void HotPathDebugEntry(string message);
}
";

		// Act
		var generationResult = await GenerateAsync(
			source,
			includeLoggerTypes: IncludeLoggerTypes.Telemetry,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}

	[Test]
	public async Task Generate_GivenV2InterfaceWithPerMethodV1Override_WithTooManyParams_RaisesDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange: v1 override on a method with 7 params (v1 limit is 6) — should raise diagnostic
		const string source =
			@"
namespace Testing;

[Logger]
public interface ITestLogger {
	[Log(DisableMSLoggingTelemetryGeneration = true)]
	void HotPathV1LogEntry(int a, int b, int c, int d, int e, int f, int g);
}
";

		// Act
		var generationResult = await GenerateAsync(
			source,
			includeLoggerTypes: IncludeLoggerTypes.Telemetry,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			s => s.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG2001"],
			cancellationToken: cancellationToken
		);
	}
}
