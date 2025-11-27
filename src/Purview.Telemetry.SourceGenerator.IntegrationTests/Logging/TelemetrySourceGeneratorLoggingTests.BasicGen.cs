namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingTests
{
	[Test]
	public async Task Generate_GivenInterfaceWithSingleBasicExplicitLogEntry_GenerateLogger()
	{
		// Arrange
		const string basicLogger =
			@"
using Purview.Telemetry.Logging;

namespace Testing;

[Logger]
public interface ITestLogger {
	[Log]
	void Log(string stringParam, int intParam, bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(basicLogger);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenInterfaceWithSingleBasicImplicitLogEntry_GenerateLogger()
	{
		// Arrange
		const string basicLogger =
			@"
using Purview.Telemetry.Logging;

namespace Testing;

[Logger]
public interface ITestLogger {
	void Log(string stringParam, int intParam, bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(basicLogger);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	[Arguments("Level = Microsoft.Extensions.Logging.LogLevel.Trace")]
	[Arguments("level: Microsoft.Extensions.Logging.LogLevel.Trace")]
	[Arguments("Microsoft.Extensions.Logging.LogLevel.Trace")]
	public async Task Generate_GivenInterfaceWithExplicitLogLevelAndAnExceptionParameter_GenerateLogger(
		string level
	)
	{
		// Arrange
		var basicLogger =
			@$"
using Purview.Telemetry.Logging;

namespace Testing;

[Logger]
public interface ITestLogger {{
	[Log({level})]
	void Log(string stringParam, int intParam, bool boolParam, Exception exception);
}}
";

		// Act
		var generationResult = await GenerateAsync(basicLogger);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, c => c.ScrubInlineGuids(), parameters: level);
	}

	[Test]
	public async Task Generate_GivenInterfaceWithoutExplicitLogLevelAndAnExceptionParameter_GenerateLogger()
	{
		// Arrange
		const string basicLogger =
			@"
using Purview.Telemetry.Logging;

namespace Testing;

[Logger]
public interface ITestLogger {
	void Log(string stringParam, int intParam, bool boolParam, Exception exception);
}
";

		// Act
		var generationResult = await GenerateAsync(basicLogger);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			expectsDiagnostics: true
		);
	}

	[Test]
	public async Task Generate_GivenInterfaceMoreThanSixParameters_RaisesDiagnostic()
	{
		// Arrange
		const string basicLogger =
			@"
using Purview.Telemetry.Logging;

namespace Testing;

[Logger]
public interface ITestLogger {
	void Log(string stringParam, int intParam, bool boolParam, string stringParam1, int intParam1, bool boolParam1, string stringParam2, int intParam2, bool boolParam2);
}
";

		// Act
		var generationResult = await GenerateAsync(basicLogger);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			expectsDiagnostics: true,
			validationCompilation: false
		);
	}

	[Test]
	public async Task Generate_GivenInterfaceMoreThanOneExceptionParameter_RaisesDiagnostic()
	{
		// Arrange
		const string basicLogger =
			@"
using Purview.Telemetry.Logging;

namespace Testing;

[Logger]
public interface ITestLogger {
	void Log(string stringParam, Exception exception1, Exception exception2);
}
";

		// Act
		var generationResult = await GenerateAsync(basicLogger);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			expectsDiagnostics: true,
			validationCompilation: false
		);
	}

	[Test]
	public async Task Generate_GivenMethodReturnsIDisposable_GeneratesScopedLogEntry()
	{
		// Arrange
		const string basicLogger =
			@"
using Purview.Telemetry.Logging;

namespace Testing;

[Logger]
public interface ITestLogger {
	IDisposable Log();
}
";

		// Act
		var generationResult = await GenerateAsync(basicLogger);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, c => c.ScrubInlineGuids());
	}

	[Test]
	public async Task Generate_GivenMethodWithParamsAndExceptionReturnsIDisposable_GeneratesScopedLogEntry()
	{
		// Arrange
		const string basicLogger =
			@"
using Purview.Telemetry.Logging;

namespace Testing;

[Logger]
public interface ITestLogger {
	IDisposable Log(string stringParam, int intParam, Exception exception);
}
";

		// Act
		var generationResult = await GenerateAsync(basicLogger);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, c => c.ScrubInlineGuids());
	}

	[Test]
	public async Task Generate_GivenMethodWithParamsReturnsIDisposable_GeneratesScopedLogEntry()
	{
		// Arrange
		const string basicLogger =
			@"
using Purview.Telemetry.Logging;

namespace Testing;

[Logger]
public interface ITestLogger {
	IDisposable Log(string stringParam, int intParam);
}
";

		// Act
		var generationResult = await GenerateAsync(basicLogger);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, c => c.ScrubInlineGuids());
	}
}
