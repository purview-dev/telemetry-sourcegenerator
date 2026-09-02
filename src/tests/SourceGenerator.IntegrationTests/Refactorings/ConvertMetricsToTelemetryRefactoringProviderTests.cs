using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator.Refactorings;

[SkipOnNetFramework]
public sealed class ConvertMetricsToTelemetryRefactoringProviderTests : CodeRefactoringTestBase
{
	static readonly ConvertMetricsToTelemetryRefactoringProvider Provider = new();

	// ─────────────────────────────────────────────────────────────────────────
	// No-op cases
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task ComputeRefactorings_GivenClassWithoutMetrics_ReturnsNoActions(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$OrderService
			{
				public void DoWork() { }
			}
			""";

		var actions = await GetRefactoringActionsAsync(code, Provider, cancellationToken: cancellationToken);

		await Assert.That(actions).IsEmpty();
	}

	[Test]
	public async Task ComputeRefactorings_GivenMetricsFieldButNoAddCalls_ReturnsNoActions(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$OrderService
			{
				readonly Counter<int> _orderCounter;

				public void DoWork() { }
			}
			""";

		var actions = await GetRefactoringActionsAsync(code, Provider, cancellationToken: cancellationToken);

		await Assert.That(actions).IsEmpty();
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Action detection
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task ComputeRefactorings_GivenClassWithCounter_ReturnsAction(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$WeatherService
			{
				readonly Counter<int> _requestCounter;

				public WeatherService(Meter meter)
				{
					_requestCounter = meter.CreateCounter<int>("requests");
				}

				public void GetWeather(string city)
				{
					_requestCounter.Add(1);
				}
			}
			""";

		var actions = await GetRefactoringActionsAsync(code, Provider, cancellationToken: cancellationToken);

		await Assert.That(actions).IsNotEmpty();
		await Assert.That(actions[0].Title).IsEqualTo("Convert Metrics to IWeatherServiceMetrics");
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Interface generation — Counter / AutoCounter
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task ApplyRefactoring_GivenCounterAddWithLiteralOne_GeneratesAutoCounterInterface(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$RequestService
			{
				readonly Counter<int> _requestCounter;

				public RequestService(Meter meter)
				{
					_requestCounter = meter.CreateCounter<int>("requests");
				}

				public void HandleRequest()
				{
					_requestCounter.Add(1);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, Provider, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("[Meter]");
		await Assert.That(result).Contains("IRequestServiceMetrics");
		await Assert.That(result).Contains("[AutoCounter]");
		await Assert.That(result).Contains("using Purview.Telemetry;");
	}

	[Test]
	public async Task ApplyRefactoring_GivenCounterAddWithVariable_GeneratesCounterInterface(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$BatchService
			{
				readonly Counter<int> _batchCounter;

				public BatchService(Meter meter)
				{
					_batchCounter = meter.CreateCounter<int>("batches");
				}

				public void ProcessBatch(int count)
				{
					_batchCounter.Add(count);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, Provider, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("[Meter]");
		await Assert.That(result).Contains("IBatchServiceMetrics");
		await Assert.That(result).Contains("[Counter]");
		await Assert.That(result).Contains("int value");
	}

	[Test]
	public async Task ApplyRefactoring_GivenHistogramRecord_GeneratesHistogramInterface(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$TimingService
			{
				readonly Histogram<double> _durationHistogram;

				public TimingService(Meter meter)
				{
					_durationHistogram = meter.CreateHistogram<double>("duration");
				}

				public void RecordDuration(double ms)
				{
					_durationHistogram.Record(ms);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, Provider, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("[Histogram]");
		await Assert.That(result).Contains("ITimingServiceMetrics");
		await Assert.That(result).Contains("double value");
	}

	[Test]
	public async Task ApplyRefactoring_GivenUpDownCounter_GeneratesUpDownCounterInterface(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$QueueService
			{
				readonly UpDownCounter<int> _queueDepth;

				public QueueService(Meter meter)
				{
					_queueDepth = meter.CreateUpDownCounter<int>("queue-depth");
				}

				public void Enqueue() => _queueDepth.Add(1);
				public void Dequeue() => _queueDepth.Add(-1);
			}
			""";

		var result = await ApplyRefactoringAsync(code, Provider, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("[UpDownCounter]");
		await Assert.That(result).Contains("IQueueServiceMetrics");
	}

	[Test]
	public async Task ApplyRefactoring_GivenMetricsField_ReplacesFieldTypeWithInterface(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$OrderService
			{
				readonly Counter<int> _orderCounter;

				public OrderService(Meter meter)
				{
					_orderCounter = meter.CreateCounter<int>("orders");
				}

				public void PlaceOrder()
				{
					_orderCounter.Add(1);
				}
			}
			""";

		var result = await ApplyRefactoringAsync(code, Provider, cancellationToken: cancellationToken);

		await Assert.That(result).IsNotNull();
		await Assert.That(result).Contains("IOrderServiceMetrics _orderCounter");
		await Assert.That(result).DoesNotContain("Counter<int> _orderCounter");
	}
}
