using System.Diagnostics;
using Purview.Telemetry;

namespace Purview.Telemetry.Benchmarks.Telemetry;

/// <summary>
/// Multi-target interface equivalent to <see cref="IMultiTargetTelemetry"/> but using the
/// v1 logging code path (<see cref="LoggerAttribute.GenerationMode"/> = <see cref="LoggerGenerationMode.V1"/>).
/// Activity + Logging (direct ILogger.Log) + Metrics in combined methods.
/// Used to compare v1 vs v2 logging overhead within the multi-target scenario.
/// </summary>
[ActivitySource("benchmark-multi-target-v1-source")]
[Logger(GenerationMode = LoggerGenerationMode.V1)]
[Meter]
public interface IMultiTargetV1Telemetry
{
	[Activity]
	[Info]
	[AutoCounter]
	Activity? StartOperation(string operationName, int operationId);

	[Event]
	[Trace]
	void OperationCompleted(Activity? activity, int resultCode, long elapsedMs);

	[Event(ActivityStatusCode.Error)]
	[Error]
	[AutoCounter]
	void OperationFailed(Activity? activity, string errorMessage);

	[Histogram]
	void RecordLatency(long latencyMs);
}
