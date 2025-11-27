namespace Purview.Telemetry.SourceGenerator.Metrics;

partial class TelemetrySourceGeneratorMetricsTests
{
	[Test]
	public async Task Generate_GivenAssemblyEnableDI_GeneratesMetrics()
	{
		// Arrange
		const string basicMetric =
			@"
using Purview.Telemetry;
using Purview.Telemetry.Metrics;

[assembly: TelemetryGeneration(GenerateDependencyExtension = true)]

namespace Testing;

[Meter(""testing-meter"")]
public interface ITestMetrics {
	[Counter]
	void Counter(int counterValue, [Tag]int intParam, [Tag]bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(basicMetric, disableDependencyInjection: false);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenInterfaceEnableDI_GeneratesMetrics()
	{
		// Arrange
		const string basicMetric =
			@"
using Purview.Telemetry;
using Purview.Telemetry.Metrics;

namespace Testing;

[Meter(""testing-meter"")]
[TelemetryGeneration(GenerateDependencyExtension = true)]
public interface ITestMetrics {
	[Counter]
	void Counter(int counterValue, [Tag]int intParam, [Tag]bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(basicMetric, disableDependencyInjection: false);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenDIDisabledAtAssemblyAndInterfaceEnableDI_GeneratesMetrics()
	{
		// Arrange
		const string basicMetric =
			@"
using Purview.Telemetry;
using Purview.Telemetry.Metrics;

[assembly: TelemetryGeneration(GenerateDependencyExtension = false)]

namespace Testing;

[Meter(""testing-meter"")]
[TelemetryGeneration(GenerateDependencyExtension = true)]
public interface ITestMetrics {
	[Counter]
	void Counter(int counterValue, [Tag]int intParam, [Tag]bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(basicMetric, disableDependencyInjection: false);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenDIEnabledAtAssemblyAndInterfaceDisabledDI_GeneratesMetrics()
	{
		// Arrange
		const string basicMetric =
			@"
using Purview.Telemetry;
using Purview.Telemetry.Metrics;

[assembly: TelemetryGeneration(GenerateDependencyExtension = true)]

namespace Testing;

[Meter(""testing-meter"")]
[TelemetryGeneration(GenerateDependencyExtension = false)]
public interface ITestMetrics {
	[Counter]
	void Counter(int counterValue, [Tag]int intParam, [Tag]bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(basicMetric, disableDependencyInjection: false);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}
}
