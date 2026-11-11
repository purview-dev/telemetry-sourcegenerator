namespace Purview.Telemetry.SourceGenerator.Metrics;

/// <summary>
/// The synchronous <c>Gauge&lt;T&gt;</c> instrument (.NET 9) and its <c>InstrumentAdvice&lt;T&gt;</c> histogram
/// bucket boundaries.
/// </summary>
partial class TelemetrySourceGeneratorMetricsTests
{
	[Test]
	public async Task Generate_GivenGauge_GeneratesAGaugeInstrument(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicMetric = """

			using System.Diagnostics.Metrics;
			using System.Collections.Generic;

			namespace Testing;

			[Meter("testing-meter")]
			public interface ITestMetrics
			{
				[Gauge("queue-depth", Unit = "items", Description = "The number of queued items")]
				void QueueDepth(int value, [Tag] string queue);

				[Gauge]
				void Ratio(double value);
			}

			""";

		// Act
		using var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		generationResult.AssertNoCompilationErrors();

		var generated = generationResult.GetSource("TestMetricsCore.Metric.g.cs");

		await Assert
			.That(generated)
			.ContainsGeneratedCode("_queueDepthInstrument = _meter.CreateGauge<int>(name:")
			.Because("a gauge is created with CreateGauge<T>");
		await Assert
			.That(generated)
			.ContainsGeneratedCode("_queueDepthInstrument.Record(value")
			.Because("a gauge records a value, it does not add to one");
		await Assert.That(generated).ContainsGeneratedCode("_ratioInstrument = _meter.CreateGauge<double>(");
		await Assert.That(generationResult).DoesNotHaveDiagnostic("TSG4010");
	}

	[Test]
	public async Task Generate_GivenHistogramBucketBoundaries_GeneratesInstrumentAdvice(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicMetric = """

			using System.Diagnostics.Metrics;

			namespace Testing;

			[Meter("testing-meter")]
			public interface ITestMetrics
			{
				[Histogram("request-duration", HistogramBucketBoundaries = "0.005, 0.01, 0.1, 1")]
				void RequestDuration(double value);
			}

			""";

		// Act
		using var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		generationResult.AssertNoCompilationErrors();

		var generated = generationResult.GetSource("TestMetricsCore.Metric.g.cs");

		await Assert
			.That(generated)
			.ContainsGeneratedCode(
				"advice: new global::System.Diagnostics.Metrics.InstrumentAdvice<double> { HistogramBucketBoundaries = new double[] { 0.005d, 0.01d, 0.1d, 1d } }"
			);
		await Assert.That(generationResult).DoesNotHaveDiagnostic("TSG4011");
		await Assert.That(generationResult).DoesNotHaveDiagnostic("TSG4012");
	}

	[Test]
	public async Task Generate_GivenUnparsableHistogramBucketBoundaries_ReportsTSG4012(
		CancellationToken cancellationToken
	)
	{
		// Arrange: not ascending, so no advice is emitted.
		const string basicMetric = """

			using System.Diagnostics.Metrics;

			namespace Testing;

			[Meter("testing-meter")]
			public interface ITestMetrics
			{
				[Histogram("request-duration", HistogramBucketBoundaries = "1, 0.5")]
				void RequestDuration(double value);
			}

			""";

		// Act
		using var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var generated = generationResult.GetSource("TestMetricsCore.Metric.g.cs");

		await Assert.That(generated).DoesNotContain("InstrumentAdvice");
		await Assert.That(generationResult).HasDiagnostic("TSG4012");
	}
}
