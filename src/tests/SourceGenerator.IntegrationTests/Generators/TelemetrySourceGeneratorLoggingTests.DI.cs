using Microsoft.Extensions.DependencyInjection;
using Purview.SourceGeneratorFramework;

namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingTests
{
	[Test]
	public async Task Generate_GivenAssemblyEnableDI_GeneratesLog(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicLog =
			@"

[assembly: TelemetryGeneration(GenerateDependencyExtension = true)]

namespace Testing;

[Logger]
public interface ITestLogger {
	[Log]
	void Log(string stringParam, int intParam, bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLog,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

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
			.Because("the generated logger must contain the log method");
		var diClass = query.GetClass("TestLoggerCoreDIExtension", "Microsoft.Extensions.DependencyInjection");
		await Assert
			.That(diClass.HasMethod(query, "AddTestLogger", TypeReference.Create<IServiceCollection>()))
			.IsTrue()
			.Because("the DI extension must register the logger via AddTestLogger");
	}

	[Test]
	public async Task Generate_GivenInterfaceEnableDI_GeneratesLog(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicLog =
			@"

namespace Testing;

[TelemetryGeneration(GenerateDependencyExtension = true)]
[Logger]
public interface ITestLogger {
	[Log]
	void Log(string stringParam, int intParam, bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLog,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

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
			.Because("the generated logger must contain the log method");
		var diClass = query.GetClass("TestLoggerCoreDIExtension", "Microsoft.Extensions.DependencyInjection");
		await Assert
			.That(diClass.HasMethod(query, "AddTestLogger", TypeReference.Create<IServiceCollection>()))
			.IsTrue()
			.Because("the DI extension must be generated when the interface opts in");
	}

	[Test]
	public async Task Generate_GivenDIDisabledAtAssemblyAndInterfaceEnableDI_GeneratesLog(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLog =
			@"

[assembly: TelemetryGeneration(GenerateDependencyExtension = false)]

namespace Testing;

[TelemetryGeneration(GenerateDependencyExtension = true)]
[Logger]
public interface ITestLogger {
	[Log]
	void Log(string stringParam, int intParam, bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLog,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

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
			.Because("the generated logger must contain the log method");
		var diClass = query.GetClass("TestLoggerCoreDIExtension", "Microsoft.Extensions.DependencyInjection");
		await Assert
			.That(diClass.HasMethod(query, "AddTestLogger", TypeReference.Create<IServiceCollection>()))
			.IsTrue()
			.Because("the DI extension must be generated when the interface overrides the disabled assembly default");
	}

	[Test]
	public async Task Generate_GivenDIEnabledAtAssemblyAndInterfaceDisabledDI_GeneratesLog(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLog =
			@"

[assembly: TelemetryGeneration(GenerateDependencyExtension = true)]

namespace Testing;

[TelemetryGeneration(GenerateDependencyExtension = false)]
[Logger]
public interface ITestLogger {
	[Log]
	void Log(string stringParam, int intParam, bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLog,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

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
			.Because("the generated logger must contain the log method");
		await Assert
			.That(query.HasClass("TestLoggerCoreDIExtension", "Microsoft.Extensions.DependencyInjection"))
			.IsFalse()
			.Because("the DI extension must not be generated when the interface opts out");
	}
}
