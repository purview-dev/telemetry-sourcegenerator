namespace Purview.Telemetry.SourceGenerator.Metrics;

partial class TelemetrySourceGeneratorMetricsTests
{
	[Test]
	public async Task Generate_GivenBasicHistogram_GeneratesMetrics(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicMetric = """


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
		var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}
}
