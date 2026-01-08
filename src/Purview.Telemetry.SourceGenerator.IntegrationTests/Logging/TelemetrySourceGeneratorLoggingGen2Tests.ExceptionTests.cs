namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingGen2Tests
{
	[Test]
	public async Task Generate_GivenMethodWithNonSpecificException_UsesExceptionParameter()
	{
		// Arrange
		var basicLogger =
			@"
using Purview.Telemetry.Logging;

namespace Testing;

[Logger]
public interface ITestLogger {
	void LogEntryWithCustomExceptionType(NullReferenceException nrf);
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			includeLoggerTypes: IncludeLoggerTypes.Telemetry
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenMethodWithCustomException_UsesExceptionParameter()
	{
		// Arrange
		var basicLogger =
			@"
using Purview.Telemetry.Logging;

namespace Testing;

[Logger]
public interface ITestLogger {
	void LogEntryWithCustomExceptionType(BadLuckException custom);
}

public class BadLuckException : Exception { }
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			includeLoggerTypes: IncludeLoggerTypes.Telemetry
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenMethodWithMultipleExceptionParameters_GeneratesEntry()
	{
		// Arrange
		var basicLogger =
			@"
using Purview.Telemetry.Logging;

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
			includeLoggerTypes: IncludeLoggerTypes.Telemetry
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}
}
