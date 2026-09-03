using Purview.SourceGeneratorFramework;

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
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(
				metricsClass.HasMethod(
					query,
					"Histogram",
					TypeReference.Create<int>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated metrics class must contain the histogram method");
		await Assert
			.That(
				metricsClass.HasMethod(
					query,
					"Histogram1",
					TypeReference.Create<int>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated metrics class must contain the second histogram method");
	}
}
