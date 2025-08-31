namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingGen2Tests
{
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task Generate_GivenBasicScopedMethod_GeneratesLogMethodCorrectly(
		bool nullableDisposable
	)
	{
		// Arrange
		char? suffix = nullableDisposable ? '?' : null;
		var basicLogger =
			@$"
using Purview.Telemetry.Logging;

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
			includeLoggerTypes: IncludeLoggerTypes.Telemetry
		);

		// Assert
		await TestHelpers.Verify(
			generationResult,
			c => c.ScrubInlineGuids(),
			parameters: nullableDisposable
		);
	}

	[Fact]
	public async Task Generate_GivenBasicScopedMethodWithOtherParameters_GeneratesLogMethodCorrectly()
	{
		// Arrange
		const string basicLogger =
			@"
using Purview.Telemetry.Logging;

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
			includeLoggerTypes: IncludeLoggerTypes.Telemetry
		);

		// Assert
		await TestHelpers.Verify(generationResult, c => c.ScrubInlineGuids());
	}

	[Fact]
	public async Task Generate_GivenBasicScopedMethodWithOtherParametersAndUsedInMessageTemplate_GeneratesLogMethodCorrectly()
	{
		// Arrange
		const string basicLogger =
			@"
using Purview.Telemetry.Logging;

namespace Testing;

[Logger]
public interface ITestLogger
{
	[Log(MessageTemplate = ""intValue: {intValue} nullableStringValue: {nullableStringValue} uintValue: {uintValue}"")]
	IDisposable BasicScoped(int intValue, string? nullableStringValue, uint uintValue);
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			includeLoggerTypes: IncludeLoggerTypes.Telemetry
		);

		// Assert
		await TestHelpers.Verify(generationResult, c => c.ScrubInlineGuids());
	}

	[Fact]
	public async Task Generate_GivenBasicScopedMethodWithOtherParametersPartiallyUsedInMessageTemplate_GeneratesLogMethodCorrectly()
	{
		// Arrange
		const string basicLogger =
			@"
using Purview.Telemetry.Logging;

namespace Testing;

[Logger]
public interface ITestLogger
{
	[Log(MessageTemplate = ""intValue: {intValue} uintValue: {uintValue}"")]
	IDisposable BasicScoped(int intValue, string? UNUSEDnullableStringValue, uint uintValue);
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			includeLoggerTypes: IncludeLoggerTypes.Telemetry
		);

		// Assert
		await TestHelpers.Verify(generationResult, c => c.ScrubInlineGuids());
	}

	[Fact]
	public async Task Generate_GivenBasicScopedAndLogHasLevelSet_GeneratesDiagnostic()
	{
		// Arrange
		const string basicLogger =
			@"
using Purview.Telemetry.Logging;
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
			includeLoggerTypes: IncludeLoggerTypes.Telemetry
		);

		// Assert
		await TestHelpers.Verify(generationResult, expectsDiagnostics: true);
	}

	[Fact]
	public async Task Generate_GivenBasicScopedAndLevelSetBySpecificAttribute_GeneratesDiagnostic()
	{
		// Arrange
		const string basicLogger =
			@"
using Purview.Telemetry.Logging;
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
			includeLoggerTypes: IncludeLoggerTypes.Telemetry
		);

		// Assert
		await TestHelpers.Verify(generationResult, expectsDiagnostics: true);
	}
}
