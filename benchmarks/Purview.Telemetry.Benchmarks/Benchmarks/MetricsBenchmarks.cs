using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Purview.Telemetry.Benchmarks.Manual;
using Purview.Telemetry.Benchmarks.Telemetry;

namespace Purview.Telemetry.Benchmarks.Benchmarks;

/// <summary>
/// Compares metrics performance between source-generator-produced and hand-written implementations
/// for the core synchronous instrument types: <c>Counter</c>, <c>UpDownCounter</c>, and <c>Histogram</c>.
/// <para>
/// Each benchmark pair exercises a single instrument call, with the manual implementation as the baseline.
/// The goal is to verify that the generated code adds no measurable overhead vs. best-practice hand-written code.
/// </para>
/// <para>
/// Observable instruments (<c>ObservableCounter</c>, <c>ObservableGauge</c>,
/// <c>ObservableUpDownCounter</c>) are not benchmarked here: they are registered once via a callback
/// and observed by the metrics pipeline — there is no per-operation hot path to compare.
/// </para>
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
[SimpleJob(RuntimeMoniker.Net47)]
[SimpleJob(RuntimeMoniker.Net48)]
[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class MetricsBenchmarks
{
    IMetricsBenchmarkTelemetry _generated = default!;
    ManualMetricsTelemetry _manual = default!;

    [GlobalSetup]
    public void Setup()
    {
        (_generated, _manual) = BenchmarkHelpers.CreateMetricsBenchmarkTelemetry();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _manual.Dispose();
    }

    // --- AutoCounter: 0 tags ---

    [Benchmark(Baseline = true, Description = "Manual: auto-counter (0 tags)")]
    public void Manual_AutoCounter_NoTags()
    {
        _manual.IncrementRequestCount();
    }

    [Benchmark(Description = "Generated: auto-counter (0 tags)")]
    public void Generated_AutoCounter_NoTags()
    {
        _generated.IncrementRequestCount();
    }

    // --- AutoCounter: 1 tag ---

    [Benchmark(Description = "Manual: auto-counter (1 tag)")]
    public void Manual_AutoCounter_OneTag()
    {
        _manual.CountRequestByType("read");
    }

    [Benchmark(Description = "Generated: auto-counter (1 tag)")]
    public void Generated_AutoCounter_OneTag()
    {
        _generated.CountRequestByType("read");
    }

    // --- UpDownCounter ---

    [Benchmark(Description = "Manual: up-down counter")]
    public void Manual_UpDownCounter()
    {
        _manual.AdjustActiveConnections(1);
    }

    [Benchmark(Description = "Generated: up-down counter")]
    public void Generated_UpDownCounter()
    {
        _generated.AdjustActiveConnections(1);
    }

    // --- Histogram: 0 tags ---

    [Benchmark(Description = "Manual: histogram (0 tags)")]
    public void Manual_Histogram_NoTags()
    {
        _manual.RecordDuration(42.0);
    }

    [Benchmark(Description = "Generated: histogram (0 tags)")]
    public void Generated_Histogram_NoTags()
    {
        _generated.RecordDuration(42.0);
    }

    // --- Histogram: 1 tag ---

    [Benchmark(Description = "Manual: histogram (1 tag)")]
    public void Manual_Histogram_OneTag()
    {
        _manual.RecordDurationByEndpoint(42.0, "/api/data");
    }

    [Benchmark(Description = "Generated: histogram (1 tag)")]
    public void Generated_Histogram_OneTag()
    {
        _generated.RecordDurationByEndpoint(42.0, "/api/data");
    }
}
