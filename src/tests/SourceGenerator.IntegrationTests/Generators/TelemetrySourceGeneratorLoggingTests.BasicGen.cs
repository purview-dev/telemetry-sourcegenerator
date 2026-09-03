using Purview.SourceGeneratorFramework;
using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingTests
{
	[Test]
	public async Task Generate_GivenInterfaceWithSingleBasicExplicitLogEntry_GenerateLogger(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLogger =
			@"

namespace Testing;

[Logger]
public interface ITestLogger {
	[Log]
	void Log(string stringParam, int intParam, bool boolParam);
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
					"Log",
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated logger must contain the log method with its parameter signature");
	}

	[Test]
	public async Task Generate_GivenInterfaceWithSingleBasicImplicitLogEntry_GenerateLogger(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLogger =
			@"

namespace Testing;

[Logger]
public interface ITestLogger {
	void Log(string stringParam, int intParam, bool boolParam);
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
					"Log",
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated logger must contain the implicit log method with its parameter signature");
	}

	[Test]
	[Arguments("Level = Microsoft.Extensions.Logging.LogLevel.Trace")]
	[Arguments("level: Microsoft.Extensions.Logging.LogLevel.Trace")]
	[Arguments("Microsoft.Extensions.Logging.LogLevel.Trace")]
	public async Task Generate_GivenInterfaceWithExplicitLogLevelAndAnExceptionParameter_GenerateLogger(
		string level,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var basicLogger =
			@$"

namespace Testing;

[Logger]
public interface ITestLogger {{
	[Log({level})]
	void Log(string stringParam, int intParam, bool boolParam, Exception exception);
}}
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
					"Log",
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>(),
					TypeReference.Create<Exception>()
				)
			)
			.IsTrue()
			.Because("the generated logger must contain the log method with its parameter signature");
	}

	[Test]
	public async Task Generate_GivenInterfaceWithoutExplicitLogLevelAndAnExceptionParameter_GenerateLogger(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLogger =
			@"

namespace Testing;

[Logger]
public interface ITestLogger {
	void Log(string stringParam, int intParam, bool boolParam, Exception exception);
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG2002");
	}

	[Test]
	public async Task Generate_GivenInterfaceMoreThanSixParameters_RaisesDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange: explicit V1 mode with 9 params — exceeds the 6-param v1 limit, raises TSG2001.
		const string basicLogger =
			@"

namespace Testing;

[Logger(GenerationMode = LoggerGenerationMode.V1)]
public interface ITestLogger {
	void Log(string stringParam, int intParam, bool boolParam, string stringParam1, int intParam1, bool boolParam1, string stringParam2, int intParam2, bool boolParam2);
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG2001");
	}

	[Test]
	public async Task Generate_GivenInterfaceMoreThanOneExceptionParameter_RaisesDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLogger =
			@"

namespace Testing;

[Logger]
public interface ITestLogger {
	void Log(string stringParam, Exception exception1, Exception exception2);
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG2000");
	}

	[Test]
	public async Task Generate_GivenMethodReturnsIDisposable_GeneratesScopedLogEntry(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLogger =
			@"

namespace Testing;

[Logger]
public interface ITestLogger {
	IDisposable Log();
}
";

		// Act
		var generationResult = await GenerateAsync(basicLogger, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var loggerClass = query.GetClass("TestLoggerCore", "Testing");
		await Assert
			.That(loggerClass.HasMethod(query, "Log"))
			.IsTrue()
			.Because("the generated logger must contain the scoped log method");
		await Assert
			.That(loggerClass.HasMethodReturnType(query, "Log", TypeReference.Create<IDisposable>()))
			.IsTrue()
			.Because("the scoped log method must return IDisposable");
	}

	[Test]
	public async Task Generate_GivenMethodWithParamsAndExceptionReturnsIDisposable_GeneratesScopedLogEntry(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLogger =
			@"

namespace Testing;

[Logger]
public interface ITestLogger {
	IDisposable Log(string stringParam, int intParam, Exception exception);
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
					"Log",
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<Exception>()
				)
			)
			.IsTrue()
			.Because("the generated logger must contain the scoped log method with its parameter signature");
		await Assert
			.That(loggerClass.HasMethodReturnType(query, "Log", TypeReference.Create<IDisposable>()))
			.IsTrue()
			.Because("the scoped log method must return IDisposable");
	}

	[Test]
	public async Task Generate_GivenMethodWithParamsReturnsIDisposable_GeneratesScopedLogEntry(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLogger =
			@"

namespace Testing;

[Logger]
public interface ITestLogger {
	IDisposable Log(string stringParam, int intParam);
}
";

		// Act
		var generationResult = await GenerateAsync(basicLogger, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var loggerClass = query.GetClass("TestLoggerCore", "Testing");
		await Assert
			.That(loggerClass.HasMethod(query, "Log", TypeReference.Create<string>(), TypeReference.Create<int>()))
			.IsTrue()
			.Because("the generated logger must contain the scoped log method with its parameter signature");
		await Assert
			.That(loggerClass.HasMethodReturnType(query, "Log", TypeReference.Create<IDisposable>()))
			.IsTrue()
			.Because("the scoped log method must return IDisposable");
	}
}
