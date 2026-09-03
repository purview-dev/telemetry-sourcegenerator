namespace Purview.Telemetry.SourceGenerator.Metrics;

partial class TelemetrySourceGeneratorMetricsTests
{
	[Test]
	public async Task Generate_GivenBasicObservableGauge_GeneratesMetrics(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicMetric = """

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
		var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod(query, "ObservableGauge"))
			.IsTrue()
			.Because("the generated metrics class must contain the observable gauge method");
		await Assert
			.That(metricsClass.HasMethod(query, "ObservableGauge2"))
			.IsTrue()
			.Because("the generated metrics class must contain the second observable gauge method");
		await Assert
			.That(metricsClass.HasMethod(query, "ObservableGauge3"))
			.IsTrue()
			.Because("the generated metrics class must contain the third observable gauge method");
	}
}
