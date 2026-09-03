using Purview.SourceGeneratorFramework;
using Purview.Telemetry.SourceGenerator.Infra;

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
		var generationResult = await GenerateAsync(basicLogger, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var loggerClass = query.GetClass("TestLoggerCore", "Testing");
		await Assert
			.That(loggerClass.HasMethod(query, "BasicScoped"))
			.IsTrue()
			.Because("the generated logger must contain the scoped log method");
		await Assert
			.That(loggerClass.HasMethodReturnType(query, "BasicScoped", TypeReference.Create<IDisposable>()))
			.IsTrue()
			.Because("the scoped log method must return IDisposable");
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
		var generationResult = await GenerateAsync(basicLogger, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var loggerClass = query.GetClass("TestLoggerCore", "Testing");
		await Assert
			.That(
				loggerClass.HasMethod(
					query,
					"BasicScoped",
					TypeReference.Create<int>(),
					TypeReference.Create<string>(),
					TypeReference.Create<uint>()
				)
			)
			.IsTrue()
			.Because("the generated logger must contain the scoped log method with its parameter signature");
		await Assert
			.That(loggerClass.HasMethodReturnType(query, "BasicScoped", TypeReference.Create<IDisposable>()))
			.IsTrue()
			.Because("the scoped log method must return IDisposable");
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
		var generationResult = await GenerateAsync(basicLogger, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var loggerClass = query.GetClass("TestLoggerCore", "Testing");
		await Assert
			.That(
				loggerClass.HasMethod(
					query,
					"BasicScoped",
					TypeReference.Create<int>(),
					TypeReference.Create<string>(),
					TypeReference.Create<uint>()
				)
			)
			.IsTrue()
			.Because("the generated logger must contain the scoped log method with its parameter signature");
		await Assert
			.That(loggerClass.HasMethodReturnType(query, "BasicScoped", TypeReference.Create<IDisposable>()))
			.IsTrue()
			.Because("the scoped log method must return IDisposable");
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
		var generationResult = await GenerateAsync(basicLogger, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var loggerClass = query.GetClass("TestLoggerCore", "Testing");
		await Assert
			.That(
				loggerClass.HasMethod(
					query,
					"BasicScoped",
					TypeReference.Create<int>(),
					TypeReference.Create<string>(),
					TypeReference.Create<uint>()
				)
			)
			.IsTrue()
			.Because("the generated logger must contain the scoped log method with its parameter signature");
		await Assert
			.That(loggerClass.HasMethodReturnType(query, "BasicScoped", TypeReference.Create<IDisposable>()))
			.IsTrue()
			.Because("the scoped log method must return IDisposable");
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
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG2007");
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
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG2007");
	}
}
