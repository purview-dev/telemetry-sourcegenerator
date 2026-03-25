using Purview.Telemetry;

namespace Purview.Telemetry.Benchmarks.Telemetry;

/// <summary>
/// Metrics-only interface with many tags (at or above the TagList threshold of 4).
/// Methods with 4+ tags use a <see cref="System.Diagnostics.TagList"/> (a stack-allocated struct)
/// to batch the tags before recording, which avoids per-call heap allocation but requires
/// struct setup overhead.
/// </summary>
[Meter]
public interface IMetricsManyTagsTelemetry
{
    // 4 tags – exactly at the TagList threshold
    [AutoCounter]
    void CountOperationWithFourTags(string endpoint, string method, string status, string region);

    // 5 tags – above the TagList threshold
    [AutoCounter]
    void CountOperationWithFiveTags(
        string endpoint,
        string method,
        string status,
        string region,
        string environment
    );

    // 6 tags – well above the TagList threshold (measurement + 5 tag parameters)
    [Histogram]
    void RecordRequestDuration(
        long durationMs,
        string endpoint,
        string method,
        string status,
        string region,
        string environment
    );
}
