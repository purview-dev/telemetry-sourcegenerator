namespace Purview.Telemetry.SourceGenerator.Refactorings;

/// <summary>
/// Snapshot tests for <see cref="ConvertAllTelemetryToInterfaceRefactoringProvider"/>.
/// Each test defines a <em>before</em> scenario and the snapshot captures the <em>after</em> output.
/// To regenerate snapshots: run <c>dotnet test</c>; <c>*.received.txt</c> files are auto-accepted.
/// </summary>
public sealed class ConvertAllTelemetryToInterfaceRefactoringProviderSnapshotTests : CodeRefactoringTestBase
{
	static readonly ConvertAllTelemetryToInterfaceRefactoringProvider Provider = new();

	// ─────────────────────────────────────────────────────────────────────────
	// All three telemetry types
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task Verify_AllThreeTelemetryTypes(CancellationToken cancellationToken)
	{
		const string code = """
			using System;
			using System.Diagnostics;
			using System.Diagnostics.Metrics;
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$OrderService
			{
				readonly ILogger<OrderService> _logger;
				static readonly ActivitySource _activitySource = new("OrderService");
				readonly Counter<long> _orderCounter;

				public OrderService(ILogger<OrderService> logger, IMeterFactory meterFactory)
				{
					_logger = logger;
					var meter = meterFactory.Create("OrderService");
					_orderCounter = meter.CreateCounter<long>("orders-processed");
				}

				public void PlaceOrder(string orderId, decimal amount)
				{
					using var activity = _activitySource.StartActivity("PlaceOrder", ActivityKind.Internal);
					_logger.LogInformation("Placing order {OrderId} for {Amount}", orderId, amount);
					_orderCounter.Add(1);
				}

				public void CancelOrder(string orderId, Exception ex)
				{
					_logger.LogError(ex, "Order {OrderId} cancelled", orderId);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Single-type subsets (only one telemetry type present)
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task Verify_LoggerOnly(CancellationToken cancellationToken)
	{
		const string code = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$AuditService
			{
				readonly ILogger<AuditService> _logger;

				public AuditService(ILogger<AuditService> logger) => _logger = logger;

				public void LogAction(string userId, string action)
				{
					_logger.LogInformation("User {UserId} performed {Action}", userId, action);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_ActivitySourceOnly(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics;

			namespace Testing;

			public class $$QueryExecutor
			{
				static readonly ActivitySource _activitySource = new("QueryExecutor");

				public void Execute(string sql)
				{
					using var activity = _activitySource.StartActivity("ExecuteQuery", ActivityKind.Internal);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_MetricsOnly(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$ThroughputMonitor
			{
				readonly Counter<long> _events;
				readonly Histogram<double> _latency;

				public ThroughputMonitor(IMeterFactory meterFactory)
				{
					var meter = meterFactory.Create("ThroughputMonitor");
					_events = meter.CreateCounter<long>("events-processed");
					_latency = meter.CreateHistogram<double>("processing-latency-ms");
				}

				public void RecordEvent(double latencyMs)
				{
					_events.Add(1);
					_latency.Record(latencyMs);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Two-type pairs
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task Verify_LoggerAndMetrics(CancellationToken cancellationToken)
	{
		const string code = """
			using System;
			using System.Diagnostics.Metrics;
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$PaymentProcessor
			{
				readonly ILogger<PaymentProcessor> _logger;
				readonly Counter<long> _paymentCount;
				readonly Histogram<double> _paymentAmount;

				public PaymentProcessor(ILogger<PaymentProcessor> logger, IMeterFactory meterFactory)
				{
					_logger = logger;
					var meter = meterFactory.Create("PaymentProcessor");
					_paymentCount = meter.CreateCounter<long>("payments-processed");
					_paymentAmount = meter.CreateHistogram<double>("payment-amount-usd");
				}

				public void Process(string paymentId, decimal amount)
				{
					_logger.LogInformation("Processing payment {PaymentId} for {Amount}", paymentId, amount);
					_paymentCount.Add(1);
					_paymentAmount.Record((double)amount);
				}

				public void Fail(string paymentId, Exception ex)
				{
					_logger.LogError(ex, "Payment {PaymentId} failed", paymentId);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_LoggerAndActivitySource(CancellationToken cancellationToken)
	{
		const string code = """
			using System;
			using System.Diagnostics;
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$BackgroundJob
			{
				readonly ILogger<BackgroundJob> _logger;
				static readonly ActivitySource _activitySource = new("BackgroundJob");

				public BackgroundJob(ILogger<BackgroundJob> logger) => _logger = logger;

				public void Execute(string jobId)
				{
					using var activity = _activitySource.StartActivity("Execute", ActivityKind.Internal);
					_logger.LogInformation("Executing job {JobId}", jobId);
				}

				public void Failed(string jobId, Exception ex)
				{
					_logger.LogError(ex, "Job {JobId} failed", jobId);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_ActivitySourceAndMetrics(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics;
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$CachingLayer
			{
				static readonly ActivitySource _activitySource = new("CachingLayer");
				readonly Counter<long> _hits;
				readonly Counter<long> _misses;

				public CachingLayer(IMeterFactory meterFactory)
				{
					var meter = meterFactory.Create("CachingLayer");
					_hits = meter.CreateCounter<long>("cache-hits");
					_misses = meter.CreateCounter<long>("cache-misses");
				}

				public void Hit(string key)
				{
					using var activity = _activitySource.StartActivity("CacheHit");
					_hits.Add(1);
				}

				public void Miss(string key)
				{
					using var activity = _activitySource.StartActivity("CacheMiss");
					_misses.Add(1);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Realistic full-service scenarios
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task Verify_ComplexService_PrimaryConstructor(CancellationToken cancellationToken)
	{
		const string code = """
			using System;
			using System.Diagnostics;
			using System.Diagnostics.Metrics;
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$ProductCatalogService(
				ILogger<ProductCatalogService> logger,
				IMeterFactory meterFactory)
			{
				static readonly ActivitySource _activitySource = new("ProductCatalogService");
				readonly Counter<long> _searches = meterFactory.Create("ProductCatalogService").CreateCounter<long>("catalog-searches");
				readonly Histogram<double> _searchLatency = meterFactory.Create("ProductCatalogService").CreateHistogram<double>("search-latency-ms");

				public void Search(string query, int pageSize)
				{
					using var activity = _activitySource.StartActivity("Search", ActivityKind.Internal);
					logger.LogDebug("Searching catalog: {Query} (page={PageSize})", query, pageSize);
					_searches.Add(1);
				}

				public void RecordSearchLatency(double latencyMs, string query)
				{
					_searchLatency.Record(latencyMs);
					logger.LogInformation("Search completed in {LatencyMs}ms for {Query}", latencyMs, query);
				}

				public void IndexProduct(string productId)
				{
					using var activity = _activitySource.StartActivity("IndexProduct");
					logger.LogInformation("Indexing product {ProductId}", productId);
				}

				public void IndexFailed(string productId, Exception ex)
				{
					logger.LogError(ex, "Failed to index product {ProductId}", productId);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Document scope
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task Verify_DocumentScope_TwoClassesInSameFile(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics;
			using System.Diagnostics.Metrics;
			using Microsoft.Extensions.Logging;

			namespace Testing;

			public class $$WeatherService
			{
				readonly ILogger<WeatherService> _logger;
				readonly ActivitySource _activitySource = new("WeatherService");
				readonly Counter<long> _requestCount;

				public WeatherService(ILogger<WeatherService> logger, IMeterFactory meterFactory)
				{
					_logger = logger;
					_requestCount = meterFactory.Create("WeatherService").CreateCounter<long>("requests");
				}

				public void GetWeather(string city)
				{
					using var activity = _activitySource.StartActivity("GetWeather");
					_logger.LogInformation("Fetching weather for {City}", city);
					_requestCount.Add(1);
				}
			}

			public class OrderService
			{
				readonly ILogger<OrderService> _logger;
				readonly Counter<long> _orderCount;

				public OrderService(ILogger<OrderService> logger, IMeterFactory meterFactory)
				{
					_logger = logger;
					_orderCount = meterFactory.Create("OrderService").CreateCounter<long>("orders");
				}

				public void PlaceOrder(int orderId)
				{
					_logger.LogInformation("Order {OrderId} placed", orderId);
					_orderCount.Add(1);
				}
			}
			""";

		await VerifyRefactoringAsync(
			code,
			Provider,
			"Purview.Telemetry.ConvertAllTelemetryToInterface.Document",
			cancellationToken
		);
	}
}
