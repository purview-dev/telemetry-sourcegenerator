using Microsoft.Extensions.Logging;
using Purview.Telemetry;

namespace Purview.Telemetry.Benchmarks.Telemetry;

/// <summary>
/// Logger-only interface using the v1 (classic) code path:
/// <see cref="LoggerAttribute.GenerationMode"/> is <see cref="LoggerGenerationMode.V1"/>, so
/// the source generator emits <c>static readonly LoggerMessage.Define&lt;T&gt;</c> fields
/// and invokes them via pre-compiled delegates — the classic approach equivalent to
/// hand-writing <c>LoggerMessage.Define&lt;T1, T2&gt;(...)</c>.
/// </summary>
[Logger(GenerationMode = LoggerGenerationMode.V1)]
public interface ILoggerV1OnlyTelemetry
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
