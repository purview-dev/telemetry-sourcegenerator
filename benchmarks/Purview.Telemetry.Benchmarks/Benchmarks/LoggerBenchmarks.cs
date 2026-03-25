using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Purview.Telemetry.Benchmarks.Manual;
using Purview.Telemetry.Benchmarks.Telemetry;

namespace Purview.Telemetry.Benchmarks.Benchmarks;

/// <summary>
/// Compares logging performance between the hand-written baseline and source-generated implementations.
/// <list type="bullet">
///   <item>Manual (baseline): hand-written <c>LoggerMessage.Define&lt;T&gt;</c> static delegates —
///   the optimal approach that the source generator replicates.</item>
///   <item>Generated v1: source-generator with <c>GenerationMode = LoggerGenerationMode.V1</c>
///   — emits the same <c>static readonly LoggerMessage.Define&lt;T&gt;</c> delegates as the manual baseline.
///   Any ratio deviation from 1.0× measures generator overhead.</item>
///   <item>Generated v2: source-generator default — emits the state-based
///   <c>LoggerMessageHelper.ThreadLocalState</c> approach, matching the built-in
///   <c>[LoggerMessage]</c> attribute generator.</item>
/// </list>
/// <para>
/// The <see cref="HasLogging"/> parameter controls whether the logger's <c>IsEnabled</c> returns
/// <c>true</c> (full code path exercised) or <c>false</c> (guard short-circuits immediately).
/// </para>
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
[SimpleJob(RuntimeMoniker.Net47)]
[SimpleJob(RuntimeMoniker.Net48)]
[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class LoggerBenchmarks
{
    ILoggerOnlyTelemetry _generatedV2 = default!;
    ILoggerV1OnlyTelemetry _generatedV1 = default!;
    ManualLoggerMessageTelemetry _manual = default!;

    /// <summary>
    /// When <c>true</c>, an always-enabled no-op logger is used so the full logging code path
    /// (<c>IsEnabled</c> check, state population, and <c>ILogger.Log</c> dispatch) is exercised.
    /// When <c>false</c>, <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger{T}"/>
    /// is used so the <c>IsEnabled</c> guard short-circuits immediately.
    /// </summary>
    [Params(true, false)]
    public bool HasLogging { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        (_generatedV2, _generatedV1, _manual) = BenchmarkHelpers.CreateLoggerTelemetry(HasLogging);
    }

    // ---- Single call: Info level ----

    [Benchmark(Baseline = true, Description = "Manual (LoggerMessage.Define) — single Info call")]
    public void Manual_Info() => _manual.OperationStarted("benchmark-op", 42);

    [Benchmark(Description = "Generated v1 (LoggerMessage.Define) — single Info call")]
    public void Generated_V1_Info() => _generatedV1.OperationStarted("benchmark-op", 42);

    [Benchmark(Description = "Generated v2 (ThreadLocalState) — single Info call")]
    public void Generated_V2_Info() => _generatedV2.OperationStarted("benchmark-op", 42);

    // ---- Full lifecycle: Info + Trace + Warning + Error ----

    [Benchmark(Description = "Manual (LoggerMessage.Define) — full lifecycle (4 calls)")]
    public void Manual_FullLifecycle()
    {
        _manual.OperationStarted("benchmark-op", 42);
        _manual.OperationCompleted(resultCode: 200, elapsedMs: 15);
        _manual.HighLatencyDetected("benchmark-op", latencyMs: 500);
        _manual.OperationFailed("something went wrong");
    }

    [Benchmark(Description = "Generated v1 (LoggerMessage.Define) — full lifecycle (4 calls)")]
    public void Generated_V1_FullLifecycle()
    {
        _generatedV1.OperationStarted("benchmark-op", 42);
        _generatedV1.OperationCompleted(resultCode: 200, elapsedMs: 15);
        _generatedV1.HighLatencyDetected("benchmark-op", latencyMs: 500);
        _generatedV1.OperationFailed("something went wrong");
    }

    [Benchmark(Description = "Generated v2 (ThreadLocalState) — full lifecycle (4 calls)")]
    public void Generated_V2_FullLifecycle()
    {
        _generatedV2.OperationStarted("benchmark-op", 42);
        _generatedV2.OperationCompleted(resultCode: 200, elapsedMs: 15);
        _generatedV2.HighLatencyDetected("benchmark-op", latencyMs: 500);
        _generatedV2.OperationFailed("something went wrong");
    }
}
