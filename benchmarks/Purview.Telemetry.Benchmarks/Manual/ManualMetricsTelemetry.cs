using System.Diagnostics.Metrics;
using Purview.Telemetry.Benchmarks.Telemetry;

namespace Purview.Telemetry.Benchmarks.Manual;

/// <summary>
/// Hand-written equivalent of the generated <c>IMetricsBenchmarkTelemetry</c> implementation.
/// Uses the raw .NET metrics API (<see cref="Counter{T}"/>, <see cref="UpDownCounter{T}"/>,
/// <see cref="Histogram{T}"/>) with the same instrument types, names, and tag patterns that the
/// source generator produces, enabling a fair generated-vs-manual comparison.
/// </summary>
/// <remarks>
/// Implements <see cref="IMetricsBenchmarkTelemetry"/> so the benchmark can hold both the generated and the
/// hand-written implementation as the interface and compare like with like: a generated class is resolved from
/// DI as its interface, and a call that crosses an interface boundary cannot be inlined into the caller. Holding
/// this baseline as its concrete type instead would let the JIT inline every one-line body and would attribute
/// the difference to the generator.
/// </remarks>
public sealed class ManualMetricsTelemetry : IMetricsBenchmarkTelemetry, IDisposable
{
	readonly Meter _meter;

	readonly Counter<int> _requestCount;
	readonly Counter<int> _requestCountByType;
	readonly UpDownCounter<int> _activeConnections;
	readonly Histogram<double> _duration;
	readonly Histogram<double> _durationByEndpoint;

	public ManualMetricsTelemetry(IMeterFactory meterFactory)
	{
		// Not ArgumentNullException.ThrowIfNull: the project also targets net48, which lacks it.
		_ = meterFactory ?? throw new ArgumentNullException(nameof(meterFactory));

		_meter = meterFactory.Create(new MeterOptions("benchmark-manual-metrics"));

		_requestCount = _meter.CreateCounter<int>("increment_request_count");
		_requestCountByType = _meter.CreateCounter<int>("count_request_by_type");
		_activeConnections = _meter.CreateUpDownCounter<int>("adjust_active_connections");
		_duration = _meter.CreateHistogram<double>("record_duration");
		_durationByEndpoint = _meter.CreateHistogram<double>("record_duration_by_endpoint");
	}

	public void IncrementRequestCount()
	{
		_requestCount.Add(1);
	}

	public void CountRequestByType(string requestType)
	{
		_requestCountByType.Add(1, new KeyValuePair<string, object?>("request_type", requestType));
	}

	public void AdjustActiveConnections(int delta)
	{
		_activeConnections.Add(delta);
	}

	public void RecordDuration(double durationMs)
	{
		_duration.Record(durationMs);
	}

	public void RecordDurationByEndpoint(double durationMs, string endpoint)
	{
		_durationByEndpoint.Record(durationMs, new KeyValuePair<string, object?>("endpoint", endpoint));
	}

	public void Dispose() => _meter.Dispose();
}
