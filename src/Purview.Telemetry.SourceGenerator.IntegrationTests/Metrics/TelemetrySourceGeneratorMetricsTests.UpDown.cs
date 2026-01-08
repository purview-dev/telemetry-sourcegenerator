namespace Purview.Telemetry.SourceGenerator.Metrics;

partial class TelemetrySourceGeneratorMetricsTests
{
	[Test]
	public async Task Generate_GivenBasicUpDown_GeneratesMetrics()
	{
		// Arrange
		const string basicMetric =
			"""

using Purview.Telemetry.Metrics;

namespace Testing;

[Meter("testing-meter")]
public interface ITestMetrics {
	[UpDownCounter]
	void UpDown(int counterValue, [Tag]int intParam, [Tag]bool boolParam);

	[UpDownCounter]
	void UpDown2([InstrumentMeasurement]int counterValue, [Tag]int intParam, [Tag]bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicMetric);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenBasicObservableUpDown_GeneratesMetrics()
	{
		// Arrange
		const string basicMetric =
			"""

using Purview.Telemetry.Metrics;
using System.Diagnostics.Metrics;
using System.Collections.Generic;

namespace Testing;

[Meter("testing-observable-meter")]
public interface ITestMetrics {
	[ObservableUpDownCounter]
	void ObservableUpDown(Func<int> f, [Tag]int intParam, [Tag]bool boolParam);

	[ObservableUpDownCounter(ThrowOnAlreadyInitialized = true)]
	void ObservableUpDown2(Func<Measurement<int>> f, [Tag]int intParam, [Tag]bool boolParam);

	[ObservableUpDownCounter]
	void ObservableUpDown3(Func<IEnumerable<Measurement<int>>> f, [Tag]int intParam, [Tag]bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicMetric);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}
}
