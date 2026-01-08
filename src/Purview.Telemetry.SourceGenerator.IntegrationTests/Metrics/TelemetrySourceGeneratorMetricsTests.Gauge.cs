namespace Purview.Telemetry.SourceGenerator.Metrics;

partial class TelemetrySourceGeneratorMetricsTests
{
	[Test]
	public async Task Generate_GivenBasicObservableGauge_GeneratesMetrics()
	{
		// Arrange
		const string basicMetric =
			"""

using Purview.Telemetry.Metrics;
using System.Diagnostics.Metrics;
using System.Collections.Generic;

namespace Testing;

[Meter("testing-meter")]
public interface ITestMetrics {
	[ObservableGauge]
	void ObservableGauge(Func<int> f, [Tag]int intParam, [Tag]bool boolParam);

	[ObservableGauge(ThrowOnAlreadyInitialized = true)]
	void ObservableGauge2(Func<Measurement<int>> f, [Tag]int intParam, [Tag]bool boolParam);

	[ObservableGauge]
	void ObservableGauge3(Func<IEnumerable<Measurement<int>>> f, [Tag]int intParam, [Tag]bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicMetric);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}
}
