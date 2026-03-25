using System.Diagnostics;

namespace Purview.Telemetry.Benchmarks.Manual;

/// <summary>
/// Hand-written equivalent of the generated single-target activity telemetry.
/// This mirrors exactly what the source generator produces, so that benchmarks
/// can measure the generated vs. manual implementations fairly.
/// </summary>
public sealed class ManualActivityTelemetry
{
    static readonly ActivitySource _activitySource = new("benchmark-manual-activity-source");

    public Activity? StartOperation(string operationName, int operationId)
    {
        if (!_activitySource.HasListeners())
        {
            return null;
        }

        var activity = _activitySource.StartActivity(
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

        return activity;
    }

    public void OperationCompleted(Activity? activity, int resultCode, long elapsedMs)
    {
        if (!_activitySource.HasListeners())
        {
            return;
        }

        if (activity != null)
        {
            var tags = new ActivityTagsCollection
            {
                { "result_code", resultCode },
                { "elapsed_ms", elapsedMs },
            };

            activity.AddEvent(new ActivityEvent("OperationCompleted", tags: tags));
        }
    }

    public void OperationFailed(Activity? activity, string errorMessage)
    {
        if (!_activitySource.HasListeners())
        {
            return;
        }

        if (activity != null)
        {
            var tags = new ActivityTagsCollection { { "error_message", errorMessage } };

            activity.AddEvent(new ActivityEvent("OperationFailed", tags: tags));
            activity.SetStatus(ActivityStatusCode.Error, errorMessage);
        }
    }
}
