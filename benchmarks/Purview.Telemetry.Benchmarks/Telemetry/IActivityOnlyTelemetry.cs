using System.Diagnostics;
using Purview.Telemetry;

namespace Purview.Telemetry.Benchmarks.Telemetry;

/// <summary>
/// Single-target interface: only Activity telemetry.
/// Used to benchmark Activity-only generation vs. multi-target.
/// </summary>
[ActivitySource("benchmark-activity-source")]
public interface IActivityOnlyTelemetry
{
	[Activity]
	Activity? StartOperation(string operationName, int operationId);

	[Event]
	void OperationCompleted(Activity? activity, int resultCode, long elapsedMs);

	[Event]
	void OperationFailed(Activity? activity, string errorMessage);
}
