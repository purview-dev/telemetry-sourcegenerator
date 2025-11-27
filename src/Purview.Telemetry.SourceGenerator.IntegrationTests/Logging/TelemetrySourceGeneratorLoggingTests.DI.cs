namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingTests
{
	[Test]
	public async Task Generate_GivenAssemblyEnableDI_GeneratesLog()
	{
		// Arrange
		const string basicLog =
			@"
using Purview.Telemetry;
using Purview.Telemetry.Logging;

[assembly: TelemetryGeneration(GenerateDependencyExtension = true)]

namespace Testing;

[Logger]
public interface ITestLogger {
	[Log]
	void Log(string stringParam, int intParam, bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(basicLog, disableDependencyInjection: false);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenInterfaceEnableDI_GeneratesLog()
	{
		// Arrange
		const string basicLog =
			@"
using Purview.Telemetry;
using Purview.Telemetry.Logging;

namespace Testing;

[TelemetryGeneration(GenerateDependencyExtension = true)]
[Logger]
public interface ITestLogger {
	[Log]
	void Log(string stringParam, int intParam, bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(basicLog, disableDependencyInjection: false);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenDIDisabledAtAssemblyAndInterfaceEnableDI_GeneratesLog()
	{
		// Arrange
		const string basicLog =
			@"
using Purview.Telemetry;
using Purview.Telemetry.Logging;

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
		var generationResult = await GenerateAsync(basicLog, disableDependencyInjection: false);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenDIEnabledAtAssemblyAndInterfaceDisabledDI_GeneratesLog()
	{
		// Arrange
		const string basicLog =
			@"
using Purview.Telemetry;
using Purview.Telemetry.Logging;

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
		var generationResult = await GenerateAsync(basicLog, disableDependencyInjection: false);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}
}
