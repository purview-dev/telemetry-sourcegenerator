using Microsoft.Extensions.Logging;
using Purview.Telemetry;

namespace Purview.Telemetry.Benchmarks.Telemetry;

/// <summary>
/// Logger-only interface using the default (v2) code path.
/// When <c>Microsoft.Extensions.Logging.LogPropertiesAttribute</c> is available
/// (via <c>Microsoft.Extensions.Telemetry.Abstractions</c>), the source generator emits
/// the new state-based approach: <c>LoggerMessageHelper.ThreadLocalState</c> is populated
/// with structured key-value pairs and passed to <see cref="ILogger.Log{TState}"/>.
/// This mirrors the output of the built-in <c>[LoggerMessage]</c> source generator.
/// </summary>
[Logger]
public interface ILoggerOnlyTelemetry
{
	[Info]
	void OperationStarted(string operationName, int operationId);

	[Trace]
	void OperationCompleted(int resultCode, long elapsedMs);

	[Error]
	void OperationFailed(string errorMessage);

	[Warning]
	void HighLatencyDetected(string operationName, long latencyMs);
}
