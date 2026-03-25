using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Purview.Telemetry.Benchmarks.Manual;

/// <summary>
/// Hand-written equivalent of the generated multi-target telemetry.
/// Combines Activity + ILogger + Metrics to mirror what the source generator produces,
/// enabling fair performance comparisons.
/// <para>
/// Logging uses <c>LoggerMessage.Define&lt;T&gt;</c> static delegates — the same optimised
/// pattern emitted by the v1 source-generator path — so that no eager string allocation
/// occurs on the hot path. This keeps the comparison apples-to-apples with generated code.
/// </para>
/// </summary>
public sealed class ManualMultiTargetTelemetry : IDisposable
{
    static readonly ActivitySource _activitySource = new("benchmark-manual-multi-target-source");

    static readonly Action<ILogger, string, int, Exception?> _startOperationLog =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(1, "StartOperation"),
            "StartOperation: OperationName = {OperationName}, OperationId = {OperationId}"
        );

    static readonly Action<ILogger, int, long, Exception?> _operationCompletedLog =
        LoggerMessage.Define<int, long>(
            LogLevel.Trace,
            new EventId(2, "OperationCompleted"),
            "OperationCompleted: ResultCode = {ResultCode}, ElapsedMs = {ElapsedMs}"
        );

    static readonly Action<ILogger, string, Exception?> _operationFailedLog =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(3, "OperationFailed"),
            "OperationFailed: ErrorMessage = {ErrorMessage}"
        );

    readonly ILogger _logger;
    readonly Meter _meter;
    readonly Counter<int> _startOperationCounter;
    readonly Counter<int> _operationFailedCounter;
    readonly Histogram<long> _latencyHistogram;

    public ManualMultiTargetTelemetry(ILogger logger, IMeterFactory meterFactory)
    {
        _logger = logger;
        _meter = meterFactory.Create(new MeterOptions("Purview.Telemetry.Benchmarks.Manual"));
        _startOperationCounter = _meter.CreateCounter<int>("multi_target.start_operation");
        _operationFailedCounter = _meter.CreateCounter<int>("multi_target.operation_failed");
        _latencyHistogram = _meter.CreateHistogram<long>("multi_target.latency_ms");
    }

    public Activity? StartOperation(string operationName, int operationId)
    {
        // Activity
        Activity? activity = null;

        if (_activitySource.HasListeners())
        {
            activity = _activitySource.StartActivity(
                "StartOperation",
                ActivityKind.Internal,
                parentId: default,
                tags: default,
                links: default,
                startTime: default
            );

            if (activity != null)
            {
                activity.SetTag("operation_name", operationName);
                activity.SetTag("operation_id", operationId);
            }
        }

        // Logging
        if (_logger.IsEnabled(LogLevel.Information))
            _startOperationLog(_logger, operationName, operationId, null);

        // Metrics
        _startOperationCounter.Add(
            1,
            new KeyValuePair<string, object?>("operation_name", operationName),
            new KeyValuePair<string, object?>("operation_id", operationId)
        );

        return activity;
    }

    public void OperationCompleted(Activity? activity, int resultCode, long elapsedMs)
    {
        // Activity
        if (_activitySource.HasListeners() && activity != null)
        {
            var tags = new ActivityTagsCollection
            {
                { "result_code", resultCode },
                { "elapsed_ms", elapsedMs },
            };

            activity.AddEvent(new ActivityEvent("OperationCompleted", tags: tags));
        }

        // Logging
        if (_logger.IsEnabled(LogLevel.Trace))
            _operationCompletedLog(_logger, resultCode, elapsedMs, null);
    }

    public void OperationFailed(Activity? activity, string errorMessage)
    {
        // Activity
        if (_activitySource.HasListeners() && activity != null)
        {
            var tags = new ActivityTagsCollection { { "error_message", errorMessage } };

            activity.AddEvent(new ActivityEvent("OperationFailed", tags: tags));
            activity.SetStatus(ActivityStatusCode.Error, errorMessage);
        }

        // Logging
        if (_logger.IsEnabled(LogLevel.Error))
            _operationFailedLog(_logger, errorMessage, null);

        // Metrics
        _operationFailedCounter.Add(
            1,
            new KeyValuePair<string, object?>("error_message", errorMessage)
        );
    }

    public void RecordLatency(long latencyMs)
    {
        _latencyHistogram.Record(latencyMs);
    }

    public void Dispose() => _meter.Dispose();
}
