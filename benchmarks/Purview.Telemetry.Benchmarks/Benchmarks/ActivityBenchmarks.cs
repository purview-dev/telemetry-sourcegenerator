using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Purview.Telemetry.Benchmarks.Manual;
using Purview.Telemetry.Benchmarks.Telemetry;

namespace Purview.Telemetry.Benchmarks.Benchmarks;

/// <summary>
/// Compares Activity telemetry performance between:
/// <list type="bullet">
///   <item>Interface-based (source-generator-generated) vs. manually-written implementations.</item>
///   <item>With an ActivityListener registered (listener present) vs. without (no listener).</item>
/// </list>
/// This highlights the cost of Activity creation and event recording under each scenario,
/// and validates that the generated code has no significant overhead vs. handwritten code.
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
[SimpleJob(RuntimeMoniker.Net47)]
[SimpleJob(RuntimeMoniker.Net48)]
[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class ActivityBenchmarks
{
    ActivityListener? _listener;
    IActivityOnlyTelemetry _generated = default!;
    ManualActivityTelemetry _manual = default!;

    [Params(true, false)]
    public bool HasListener { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _generated = BenchmarkHelpers.CreateGeneratedActivityTelemetry();
        _manual = new ManualActivityTelemetry();

        if (HasListener)
        {
            _listener = BenchmarkHelpers.CreateAllSamplingListener();
            ActivitySource.AddActivityListener(_listener);
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _listener?.Dispose();
        _listener = null;
    }

    [Benchmark(Baseline = true, Description = "Manual: start + complete")]
    public void Manual_StartAndComplete()
    {
        using var activity = _manual.StartOperation("benchmark-op", 42);
        _manual.OperationCompleted(activity, resultCode: 200, elapsedMs: 15);
    }

    [Benchmark(Description = "Generated: start + complete")]
    public void Generated_StartAndComplete()
    {
        using var activity = _generated.StartOperation("benchmark-op", 42);
        _generated.OperationCompleted(activity, resultCode: 200, elapsedMs: 15);
    }

    [Benchmark(Description = "Manual: start + fail")]
    public void Manual_StartAndFail()
    {
        using var activity = _manual.StartOperation("benchmark-op", 42);
        _manual.OperationFailed(activity, "something went wrong");
    }

    [Benchmark(Description = "Generated: start + fail")]
    public void Generated_StartAndFail()
    {
        using var activity = _generated.StartOperation("benchmark-op", 42);
        _generated.OperationFailed(activity, "something went wrong");
    }
}
