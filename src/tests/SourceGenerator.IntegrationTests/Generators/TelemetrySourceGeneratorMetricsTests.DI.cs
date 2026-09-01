using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator.Metrics;

partial class TelemetrySourceGeneratorMetricsTests
{
	[Test]
	public async Task Generate_GivenAssemblyEnableDI_GeneratesMetrics(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicMetric = """


[assembly: TelemetryGeneration(GenerateDependencyExtension = true)]

namespace Testing;

[Meter("testing-meter")]
public interface ITestMetrics {
	[Counter]
	void Counter(int counterValue, [Tag]int intParam, [Tag]bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicMetric,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}

	[Test]
	public async Task Generate_GivenInterfaceEnableDI_GeneratesMetrics(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicMetric = """


namespace Testing;

[Meter("testing-meter")]
[TelemetryGeneration(GenerateDependencyExtension = true)]
public interface ITestMetrics {
	[Counter]
	void Counter(int counterValue, [Tag]int intParam, [Tag]bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicMetric,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}

	[Test]
	public async Task Generate_GivenDIDisabledAtAssemblyAndInterfaceEnableDI_GeneratesMetrics(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicMetric = """


[assembly: TelemetryGeneration(GenerateDependencyExtension = false)]

namespace Testing;

[Meter("testing-meter")]
[TelemetryGeneration(GenerateDependencyExtension = true)]
public interface ITestMetrics {
	[Counter]
	void Counter(int counterValue, [Tag]int intParam, [Tag]bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicMetric,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}

	[Test]
	public async Task Generate_GivenDIEnabledAtAssemblyAndInterfaceDisabledDI_GeneratesMetrics(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicMetric = """


[assembly: TelemetryGeneration(GenerateDependencyExtension = true)]

namespace Testing;

[Meter("testing-meter")]
[TelemetryGeneration(GenerateDependencyExtension = false)]
public interface ITestMetrics {
	[Counter]
	void Counter(int counterValue, [Tag]int intParam, [Tag]bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicMetric,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}
}
