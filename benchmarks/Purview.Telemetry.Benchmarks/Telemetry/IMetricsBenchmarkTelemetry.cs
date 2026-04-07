using Purview.Telemetry;

namespace Purview.Telemetry.Benchmarks.Telemetry;

/// <summary>
/// Metrics-only interface covering the core synchronous instrument types:
/// <see cref="System.Diagnostics.Metrics.Counter{T}"/>,
/// <see cref="System.Diagnostics.Metrics.UpDownCounter{T}"/>, and
/// <see cref="System.Diagnostics.Metrics.Histogram{T}"/>.
/// <para>
/// Each method is benchmarked against its hand-written equivalent in
/// <see cref="Manual.ManualMetricsTelemetry"/> to verify that the source-generated
/// implementation adds no measurable overhead.
/// </para>
/// </summary>
[Meter]
public interface IMetricsBenchmarkTelemetry
{
	/// <summary>Auto-incrementing counter — no tags.</summary>
	[AutoCounter]
	void IncrementRequestCount();

	/// <summary>Auto-incrementing counter — 1 tag.</summary>
	[AutoCounter]
	void CountRequestByType(string requestType);

	/// <summary>UpDownCounter — accepts a signed delta, no tags.</summary>
	[UpDownCounter]
	void AdjustActiveConnections(int delta);

	/// <summary>Histogram — records a duration measurement, no tags.</summary>
	[Histogram]
	void RecordDuration(double durationMs);

	/// <summary>Histogram — records a duration measurement with 1 tag.</summary>
	[Histogram]
	void RecordDurationByEndpoint(double durationMs, string endpoint);
}
