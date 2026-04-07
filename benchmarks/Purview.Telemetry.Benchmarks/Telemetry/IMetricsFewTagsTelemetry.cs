using Purview.Telemetry;

namespace Purview.Telemetry.Benchmarks.Telemetry;

/// <summary>
/// Metrics-only interface with few tags (below the TagList threshold of 4).
/// Methods with fewer than 4 tags pass them directly as KeyValuePair parameters,
/// avoiding a TagList allocation.
/// </summary>
[Meter]
public interface IMetricsFewTagsTelemetry
{
	// 0 tags – no TagList; instrument.Add/Record called with tagList: default
	[Histogram]
	void RecordOperationLatency(long latencyMs);

	// 1 tag – no TagList; passed as a single KeyValuePair
	[AutoCounter]
	void CountOperationByType(string operationType);

	// 3 tags – no TagList; passed as three inline KeyValuePair parameters (one below threshold)
	[Histogram]
	void RecordRequestSize(long sizeBytes, string endpoint, string method, string statusCode);
}
