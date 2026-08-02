namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingGen2Tests
{
	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task Generate_GivenBasicScopedMethod_GeneratesLogMethodCorrectly(
		bool nullableDisposable,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		char? suffix = nullableDisposable ? '?' : null;
		var basicLogger =
			@$"

namespace Testing;

[Logger]
public interface ITestLogger
{{
	IDisposable{suffix} BasicScoped();
}}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			includeLoggerTypes: IncludeLoggerTypes.Telemetry,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			cancellationToken: cancellationToken,
			parameters: nullableDisposable
		);
	}

	[Test]
	public async Task Generate_GivenBasicScopedMethodWithOtherParameters_GeneratesLogMethodCorrectly(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLogger =
			@"

namespace Testing;

[Logger]
public interface ITestLogger
{
	IDisposable BasicScoped(int intValue, string? nullableStringValue, uint uintValue);
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			includeLoggerTypes: IncludeLoggerTypes.Telemetry,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			cancellationToken: cancellationToken
		);
	}

	[Test]
	public async Task Generate_GivenBasicScopedMethodWithOtherParametersAndUsedInMessageTemplate_GeneratesLogMethodCorrectly(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLogger = """


namespace Testing;

[Logger]
public interface ITestLogger
{
	[Log(MessageTemplate = "intValue: {intValue} nullableStringValue: {nullableStringValue} uintValue: {uintValue}")]
	IDisposable BasicScoped(int intValue, string? nullableStringValue, uint uintValue);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			includeLoggerTypes: IncludeLoggerTypes.Telemetry,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			cancellationToken: cancellationToken
		);
	}

	[Test]
	public async Task Generate_GivenBasicScopedMethodWithOtherParametersPartiallyUsedInMessageTemplate_GeneratesLogMethodCorrectly(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLogger = """


namespace Testing;

[Logger]
public interface ITestLogger
{
	[Log(MessageTemplate = "intValue: {intValue} uintValue: {uintValue}")]
	IDisposable BasicScoped(int intValue, string? UNUSEDnullableStringValue, uint uintValue);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			includeLoggerTypes: IncludeLoggerTypes.Telemetry,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			cancellationToken: cancellationToken
		);
	}

	[Test]
	public async Task Generate_GivenBasicScopedAndLogHasLevelSet_GeneratesDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLogger =
			@"
using Microsoft.Extensions.Logging;

namespace Testing;

[Logger]
public interface ITestLogger
{
	[Log(Level = LogLevel.Information)]
	IDisposable BasicScoped();
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			includeLoggerTypes: IncludeLoggerTypes.Telemetry,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			expectsDiagnostics: true,
			cancellationToken: cancellationToken
		);
	}

	[Test]
	public async Task Generate_GivenBasicScopedAndLevelSetBySpecificAttribute_GeneratesDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLogger =
			@"
using Microsoft.Extensions.Logging;

namespace Testing;

[Logger]
public interface ITestLogger
{
	[Info]
	IDisposable BasicScoped();
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			includeLoggerTypes: IncludeLoggerTypes.Telemetry,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			expectsDiagnostics: true,
			cancellationToken: cancellationToken
		);
	}
}
