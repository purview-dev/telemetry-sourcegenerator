namespace Purview.Telemetry.SourceGenerator.Refactorings;

/// <summary>
/// Snapshot tests for <see cref="ConvertMetricsToTelemetryRefactoringProvider"/>.
/// Each test defines a <em>before</em> scenario and the snapshot captures the <em>after</em> output.
/// To regenerate snapshots: run <c>dotnet test</c>; <c>*.received.txt</c> files are auto-accepted.
/// </summary>
public sealed class ConvertMetricsToTelemetryRefactoringProviderSnapshotTests
	: CodeRefactoringTestBase
{
	static readonly ConvertMetricsToTelemetryRefactoringProvider Provider = new();

	// ─────────────────────────────────────────────────────────────────────────
	// Counter
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task Verify_Counter_AutoIncrement_LiteralOne(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$RequestHandler
			{
				readonly Counter<long> _requestCounter;

				public RequestHandler(IMeterFactory meterFactory)
				{
					var meter = meterFactory.Create("RequestHandler");
					_requestCounter = meter.CreateCounter<long>("requests");
				}

				public void Handle(string path)
				{
					_requestCounter.Add(1);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_Counter_WithVariable_NotAutoIncrement(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$BatchProcessor
			{
				readonly Counter<long> _batchCounter;

				public BatchProcessor(IMeterFactory meterFactory)
				{
					var meter = meterFactory.Create("BatchProcessor");
					_batchCounter = meter.CreateCounter<long>("batches-processed");
				}

				public void ProcessBatch(int batchSize)
				{
					_batchCounter.Add(batchSize);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_Counter_WithKeyValuePairTags(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Collections.Generic;
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$ApiMetrics
			{
				readonly Counter<long> _apiCounter;

				public ApiMetrics(IMeterFactory meterFactory)
				{
					var meter = meterFactory.Create("ApiMetrics");
					_apiCounter = meter.CreateCounter<long>("api-calls");
				}

				public void RecordCall(string route, string method)
				{
					_apiCounter.Add(1,
						new KeyValuePair<string, object?>("route", route),
						new KeyValuePair<string, object?>("method", method));
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_Counter_WithTagList(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$SearchMetrics
			{
				readonly Counter<long> _searchCounter;

				public SearchMetrics(IMeterFactory meterFactory)
				{
					var meter = meterFactory.Create("SearchMetrics");
					_searchCounter = meter.CreateCounter<long>("searches");
				}

				public void RecordSearch(string query, string index, string tenant, bool cacheHit)
				{
					var tags = new TagList
					{
						{ "query_type", query },
						{ "index", index },
						{ "tenant", tenant },
						{ "cache_hit", cacheHit }
					};
					_searchCounter.Add(1, tags);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Histogram
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task Verify_Histogram_BasicRecord(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$LatencyTracker
			{
				readonly Histogram<double> _latencyHistogram;

				public LatencyTracker(IMeterFactory meterFactory)
				{
					var meter = meterFactory.Create("LatencyTracker");
					_latencyHistogram = meter.CreateHistogram<double>("request-latency-ms");
				}

				public void Record(double elapsedMs)
				{
					_latencyHistogram.Record(elapsedMs);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_Histogram_WithTag(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Collections.Generic;
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$DatabaseMetrics
			{
				readonly Histogram<double> _queryDuration;

				public DatabaseMetrics(IMeterFactory meterFactory)
				{
					var meter = meterFactory.Create("DatabaseMetrics");
					_queryDuration = meter.CreateHistogram<double>("db-query-duration-ms");
				}

				public void RecordQuery(double durationMs, string operation)
				{
					_queryDuration.Record(durationMs, new KeyValuePair<string, object?>("operation", operation));
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// UpDownCounter
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task Verify_UpDownCounter_Add(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$ConnectionPool
			{
				readonly UpDownCounter<int> _activeConnections;

				public ConnectionPool(IMeterFactory meterFactory)
				{
					var meter = meterFactory.Create("ConnectionPool");
					_activeConnections = meter.CreateUpDownCounter<int>("active-connections");
				}

				public void Acquired() => _activeConnections.Add(1);
				public void Released() => _activeConnections.Add(-1);
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Mixed instruments
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task Verify_Mixed_Counter_And_Histogram(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$HttpServerMetrics
			{
				readonly Counter<long> _requestCount;
				readonly Histogram<double> _requestDuration;

				public HttpServerMetrics(IMeterFactory meterFactory)
				{
					var meter = meterFactory.Create("HttpServer");
					_requestCount = meter.CreateCounter<long>("http-requests-total");
					_requestDuration = meter.CreateHistogram<double>("http-request-duration-ms");
				}

				public void RecordRequest(double durationMs)
				{
					_requestCount.Add(1);
					_requestDuration.Record(durationMs);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_Mixed_AllThreeInstrumentTypes(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$QueueMetrics
			{
				readonly Counter<long> _enqueued;
				readonly Counter<long> _dequeued;
				readonly UpDownCounter<long> _depth;
				readonly Histogram<double> _processingTime;

				public QueueMetrics(IMeterFactory meterFactory)
				{
					var meter = meterFactory.Create("QueueMetrics");
					_enqueued = meter.CreateCounter<long>("messages-enqueued");
					_dequeued = meter.CreateCounter<long>("messages-dequeued");
					_depth = meter.CreateUpDownCounter<long>("queue-depth");
					_processingTime = meter.CreateHistogram<double>("message-processing-ms");
				}

				public void Enqueue()
				{
					_enqueued.Add(1);
					_depth.Add(1);
				}

				public void Dequeue(double processMs)
				{
					_dequeued.Add(1);
					_depth.Add(-1);
					_processingTime.Record(processMs);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Injection styles
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task Verify_PrimaryConstructor_WithMeterFactory(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$StorageMetrics(IMeterFactory meterFactory)
			{
				readonly Counter<long> _writes = meterFactory.Create("StorageMetrics").CreateCounter<long>("writes");
				readonly Counter<long> _reads = meterFactory.Create("StorageMetrics").CreateCounter<long>("reads");

				public void RecordWrite() => _writes.Add(1);
				public void RecordRead() => _reads.Add(1);
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_Counter_IntMeasurementType(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$WorkerMetrics
			{
				readonly Counter<int> _taskCount;

				public WorkerMetrics(IMeterFactory meterFactory)
				{
					var meter = meterFactory.Create("WorkerMetrics");
					_taskCount = meter.CreateCounter<int>("tasks");
				}

				public void RecordTasks(int count)
				{
					_taskCount.Add(count);
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
			using System.Diagnostics.Metrics;

			namespace Testing;

			public class $$RequestMetrics
			{
				readonly Counter<long> _requestCount;

				public RequestMetrics(IMeterFactory meterFactory)
				{
					var meter = meterFactory.Create("RequestMetrics");
					_requestCount = meter.CreateCounter<long>("request.count");
				}

				public void RecordRequest()
				{
					_requestCount.Add(1);
				}
			}

			public class ErrorMetrics
			{
				readonly Counter<long> _errorCount;

				public ErrorMetrics(IMeterFactory meterFactory)
				{
					var meter = meterFactory.Create("ErrorMetrics");
					_errorCount = meter.CreateCounter<long>("error.count");
				}

				public void RecordError()
				{
					_errorCount.Add(1);
				}
			}
			""";

		await VerifyRefactoringAsync(
			code,
			Provider,
			"Purview.Telemetry.ConvertMetricsToTelemetry.Document",
			cancellationToken
		);
	}
}
