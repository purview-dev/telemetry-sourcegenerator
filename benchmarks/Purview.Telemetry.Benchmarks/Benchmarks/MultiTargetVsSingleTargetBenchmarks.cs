using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Purview.Telemetry.Benchmarks.Manual;
using Purview.Telemetry.Benchmarks.Telemetry;

namespace Purview.Telemetry.Benchmarks.Benchmarks;

/// <summary>
/// Compares performance between:
/// <list type="bullet">
///   <item>Single-target telemetry (Activity only, one telemetry type per method call).</item>
///   <item>Multi-target telemetry (Activity + Logging + Metrics generated from a single method call).</item>
/// </list>
/// The multi-target scenario is a key feature of the source generator: one interface method
/// generates all three telemetry types simultaneously, reducing boilerplate.
/// This benchmark shows the cost of that combined approach compared to individual calls.
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
[SimpleJob(RuntimeMoniker.Net47)]
[SimpleJob(RuntimeMoniker.Net48)]
[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class MultiTargetVsSingleTargetBenchmarks
{
    ActivityListener? _listener;

    IActivityOnlyTelemetry _singleTarget = default!;
    IMultiTargetTelemetry _multiTargetGenerated = default!;
    ManualMultiTargetTelemetry _multiTargetManual = default!;

    [Params(true, false)]
    public bool HasListener { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _singleTarget = BenchmarkHelpers.CreateGeneratedActivityTelemetry();
        (_multiTargetGenerated, _multiTargetManual) = BenchmarkHelpers.CreateMultiTargetTelemetry();

        if (HasListener)
        {
            _listener = BenchmarkHelpers.CreateAllSamplingListener();
            ActivitySource.AddActivityListener(_listener);
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _multiTargetManual.Dispose();
        _listener?.Dispose();
        _listener = null;
    }

    // ---- Start + complete (no extra metric recording) ----

    [Benchmark(Baseline = true, Description = "Single-target (generated): start + complete")]
    public void SingleTarget_Generated()
    {
        using var activity = _singleTarget.StartOperation("op", 1);
        _singleTarget.OperationCompleted(activity, resultCode: 0, elapsedMs: 10);
    }

    [Benchmark(Description = "Multi-target (generated): start + complete")]
    public void MultiTarget_Generated()
    {
        using var activity = _multiTargetGenerated.StartOperation("op", 1);
        _multiTargetGenerated.OperationCompleted(activity, resultCode: 0, elapsedMs: 10);
    }

    [Benchmark(Description = "Multi-target (manual): start + complete")]
    public void MultiTarget_Manual()
    {
        using var activity = _multiTargetManual.StartOperation("op", 1);
        _multiTargetManual.OperationCompleted(activity, resultCode: 0, elapsedMs: 10);
    }

    // ---- Full lifecycle (start + complete + record latency metric) ----
    // Single-target does not expose a latency metric; only multi-target does.
    // These benchmarks show the true cost of the combined telemetry pattern.

    [Benchmark(Description = "Multi-target (generated): start + complete + record latency")]
    public void MultiTarget_Generated_FullLifecycle()
    {
        using var activity = _multiTargetGenerated.StartOperation("op", 1);
        _multiTargetGenerated.OperationCompleted(activity, resultCode: 0, elapsedMs: 10);
        _multiTargetGenerated.RecordLatency(10);
    }

    [Benchmark(Description = "Multi-target (manual): start + complete + record latency")]
    public void MultiTarget_Manual_FullLifecycle()
    {
        using var activity = _multiTargetManual.StartOperation("op", 1);
        _multiTargetManual.OperationCompleted(activity, resultCode: 0, elapsedMs: 10);
        _multiTargetManual.RecordLatency(10);
    }
}
