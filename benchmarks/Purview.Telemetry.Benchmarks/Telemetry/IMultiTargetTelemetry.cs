using System.Diagnostics;
using Purview.Telemetry;

namespace Purview.Telemetry.Benchmarks.Telemetry;

/// <summary>
/// Multi-target interface: Activity + Logging + Metrics in combined methods.
/// Used to benchmark multi-target generation overhead vs. single-target.
/// </summary>
[ActivitySource("benchmark-multi-target-source")]
[Logger]
[Meter]
public interface IMultiTargetTelemetry
{
	// MULTI-TARGET: Activity + Info log + AutoCounter
	[Activity]
	[Info]
	[AutoCounter]
	Activity? StartOperation(string operationName, int operationId);

	// MULTI-TARGET: ActivityEvent + Trace log
	[Event]
	[Trace]
	void OperationCompleted(Activity? activity, int resultCode, long elapsedMs);

	// MULTI-TARGET: ActivityEvent + Error log + AutoCounter
	[Event(ActivityStatusCode.Error)]
	[Error]
	[AutoCounter]
	void OperationFailed(Activity? activity, string errorMessage);

	// SINGLE-TARGET: Histogram only
	[Histogram]
	void RecordLatency(long latencyMs);
}
