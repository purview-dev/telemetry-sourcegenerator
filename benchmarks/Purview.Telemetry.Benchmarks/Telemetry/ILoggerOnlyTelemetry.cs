using Microsoft.Extensions.Logging;
using Purview.Telemetry;

namespace Purview.Telemetry.Benchmarks.Telemetry;

/// <summary>
/// Logger-only interface using the v2 (state-based) code path.
/// <see cref="LoggerAttribute.GenerationMode"/> is <see cref="LoggerGenerationMode.V2"/>, so
/// the source generator emits the state-based approach: <c>LoggerMessageHelper.ThreadLocalState</c>
/// is populated with structured key-value pairs and passed to <see cref="ILogger.Log{TState}"/>.
/// This mirrors the output of the built-in <c>[LoggerMessage]</c> source generator.
/// </summary>
[Logger(GenerationMode = LoggerGenerationMode.V2)]
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
