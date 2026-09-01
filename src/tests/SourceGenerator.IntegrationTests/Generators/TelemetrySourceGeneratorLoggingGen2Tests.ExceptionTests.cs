using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingGen2Tests
{
	[Test]
	public async Task Generate_GivenMethodWithNonSpecificException_UsesExceptionParameter(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLogger =
			@"

namespace Testing;

[Logger(GenerationMode = LoggerGenerationMode.V2)]
public interface ITestLogger {
	void LogEntryWithCustomExceptionType(NullReferenceException nrf);
}
";

		// Act
		var generationResult = await GenerateAsync(basicLogger, cancellationToken: cancellationToken);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}

	[Test]
	public async Task Generate_GivenMethodWithCustomException_UsesExceptionParameter(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLogger =
			@"

namespace Testing;

[Logger(GenerationMode = LoggerGenerationMode.V2)]
public interface ITestLogger {
	void LogEntryWithCustomExceptionType(BadLuckException custom);
}

public class BadLuckException : Exception { }
";

		// Act
		var generationResult = await GenerateAsync(basicLogger, cancellationToken: cancellationToken);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}

	[Test]
	public async Task Generate_GivenMethodWithMultipleExceptionParameters_GeneratesEntry(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLogger =
			@"

namespace Testing;

[Logger]
public interface ITestLogger {
	void LogEntryWithMoreThanSixParams(int one, int two, int three, int four, int five, BadLuckException six, InvalidOperationException seven, ArgumentException eight, Exception nine, Exception? ten, Exception eleven);
}

public class BadLuckException : Exception { }
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, expectsDiagnostics: true, cancellationToken: cancellationToken);
	}
}
