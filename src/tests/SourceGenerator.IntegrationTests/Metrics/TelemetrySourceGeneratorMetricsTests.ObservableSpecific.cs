namespace Purview.Telemetry.SourceGenerator.Metrics;

partial class TelemetrySourceGeneratorMetricsTests
{
	[Test]
	public async Task Generate_GivenObservablesReturnVoid_GeneratesMetrics(CancellationToken cancellationToken)
	{
		// Arrange - v4.0: Observable instruments must return void and accept Func<T> parameter
		const string basicMetric = """


namespace Testing;

[Meter("testing-meter")]
public interface ITestMetrics
{
	[ObservableCounter]
	void Counter(Func<int> counterValue, [Tag]int intParam, [Tag]bool boolParam);

	[ObservableGauge]
	void Gauge(Func<int> gaugeValue, [Tag]int intParam, [Tag]bool boolParam);

	[ObservableUpDownCounter]
	void UpDown(Func<int> upDownValue, [Tag]int intParam, [Tag]bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}

	[Test]
	public async Task Generate_GivenObservablesWithThrowsOnAlreadyInitialized_GeneratesMetrics(
		CancellationToken cancellationToken
	)
	{
		// Arrange - v4.0: Observable instruments must return void
		const string basicMetric = """


namespace Testing;

[Meter("testing-meter")]
public interface ITestMetrics {
	[ObservableCounter(ThrowOnAlreadyInitialized = true)]
	void Counter(Func<int> counterValue, [Tag]int intParam, [Tag]bool boolParam);

	[ObservableGauge(ThrowOnAlreadyInitialized = true)]
	void Gauge(Func<int> gaugeValue, [Tag]int intParam, [Tag]bool boolParam);

	[ObservableUpDownCounter(ThrowOnAlreadyInitialized = true)]
	void UpDown(Func<int> upDownValue, [Tag]int intParam, [Tag]bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}
}
