using Microsoft.Extensions.Logging;

namespace Purview.Telemetry.Benchmarks.Manual;

/// <summary>
/// Hand-written logging using direct <see cref="ILogger.Log"/> calls with inline state and
/// a formatter lambda — the traditional approach before <c>LoggerMessage.Define</c>.
/// This is the baseline for comparing against source-generated v1 and v2 logging code.
/// </summary>
public sealed class ManualLoggerTelemetry
{
    readonly ILogger _logger;

    public ManualLoggerTelemetry(ILogger logger) => _logger = logger;

    public void OperationStarted(string operationName, int operationId)
    {
        if (!_logger.IsEnabled(LogLevel.Information))
            return;

        _logger.Log(
            LogLevel.Information,
            new EventId(781377476, "OperationStarted"),
            $"OperationStarted: OperationName = {operationName}, OperationId = {operationId}",
            null,
            static (s, _) => s
        );
    }

    public void OperationCompleted(int resultCode, long elapsedMs)
    {
        if (!_logger.IsEnabled(LogLevel.Trace))
            return;

        _logger.Log(
            LogLevel.Trace,
            new EventId(2040081925, "OperationCompleted"),
            $"OperationCompleted: ResultCode = {resultCode}, ElapsedMs = {elapsedMs}",
            null,
            static (s, _) => s
        );
    }

    public void OperationFailed(string errorMessage)
    {
        if (!_logger.IsEnabled(LogLevel.Error))
            return;

        _logger.Log(
            LogLevel.Error,
            new EventId(1187282567, "OperationFailed"),
            $"OperationFailed: ErrorMessage = {errorMessage}",
            null,
            static (s, _) => s
        );
    }

    public void HighLatencyDetected(string operationName, long latencyMs)
    {
        if (!_logger.IsEnabled(LogLevel.Warning))
            return;

        _logger.Log(
            LogLevel.Warning,
            new EventId(42, "HighLatencyDetected"),
            $"HighLatencyDetected: OperationName = {operationName}, LatencyMs = {latencyMs}",
            null,
            static (s, _) => s
        );
    }
}
