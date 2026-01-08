namespace Purview.Telemetry.SourceGenerator.Metrics;

partial class TelemetrySourceGeneratorMetricsTests
{
	[Test]
	public async Task Generate_GivenBasicHistogram_GeneratesMetrics()
	{
		// Arrange
		const string basicMetric =
			"""

using Purview.Telemetry.Metrics;

namespace Testing;

[Meter("testing-meter")]
public interface ITestMetrics {
	[Histogram]
	void Histogram(int counterValue, [Tag]int intParam, [Tag]bool boolParam);

	[Histogram]
	void Histogram1([InstrumentMeasurement]int counterValue, [Tag]int intParam, [Tag]bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicMetric);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}
}
