using Microsoft.Extensions.Logging;

namespace Purview.Telemetry.Benchmarks.Manual;

/// <summary>
/// Hand-written logging using <c>LoggerMessage.Define&lt;T&gt;</c> static pre-compiled delegates —
/// the same pattern that the source generator emits for v1 logging
/// (<c>GenerationMode = LoggerGenerationMode.V1</c>).
/// This baseline demonstrates the ceiling performance of the pattern and verifies
/// that the source generator adds no measurable overhead over a hand-written equivalent.
/// </summary>
public sealed class ManualLoggerMessageTelemetry
{
    static readonly Action<ILogger, string, int, Exception?> _operationStartedAction =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(781377476, "OperationStarted"),
            "OperationStarted: OperationName = {OperationName}, OperationId = {OperationId}"
        );

    static readonly Action<ILogger, int, long, Exception?> _operationCompletedAction =
        LoggerMessage.Define<int, long>(
            LogLevel.Trace,
            new EventId(2040081925, "OperationCompleted"),
            "OperationCompleted: ResultCode = {ResultCode}, ElapsedMs = {ElapsedMs}"
        );

    static readonly Action<ILogger, string, Exception?> _operationFailedAction =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1187282567, "OperationFailed"),
            "OperationFailed: ErrorMessage = {ErrorMessage}"
        );

    static readonly Action<ILogger, string, long, Exception?> _highLatencyDetectedAction =
        LoggerMessage.Define<string, long>(
            LogLevel.Warning,
            new EventId(42, "HighLatencyDetected"),
            "HighLatencyDetected: OperationName = {OperationName}, LatencyMs = {LatencyMs}"
        );

    readonly ILogger _logger;

    public ManualLoggerMessageTelemetry(ILogger logger) => _logger = logger;

    public void OperationStarted(string operationName, int operationId)
    {
        if (!_logger.IsEnabled(LogLevel.Information))
            return;

        _operationStartedAction(_logger, operationName, operationId, null);
    }

    public void OperationCompleted(int resultCode, long elapsedMs)
    {
        if (!_logger.IsEnabled(LogLevel.Trace))
            return;

        _operationCompletedAction(_logger, resultCode, elapsedMs, null);
    }

    public void OperationFailed(string errorMessage)
    {
        if (!_logger.IsEnabled(LogLevel.Error))
            return;

        _operationFailedAction(_logger, errorMessage, null);
    }

    public void HighLatencyDetected(string operationName, long latencyMs)
    {
        if (!_logger.IsEnabled(LogLevel.Warning))
            return;

        _highLatencyDetectedAction(_logger, operationName, latencyMs, null);
    }
}
