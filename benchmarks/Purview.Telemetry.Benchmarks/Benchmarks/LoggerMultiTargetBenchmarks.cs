using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Purview.Telemetry.Benchmarks.Manual;
using Purview.Telemetry.Benchmarks.Telemetry;

namespace Purview.Telemetry.Benchmarks.Benchmarks;

/// <summary>
/// Compares multi-target telemetry performance (Activity + Logging + Metrics) between
/// the hand-written manual baseline and source-generated implementations (v1 and v2),
/// and shows the marginal cost of combining all three telemetry types versus logging alone.
/// <list type="bullet">
///   <item>Baseline: manual multi-target (hand-written, <c>LoggerMessage.Define</c> pattern).</item>
///   <item>Generated v1: same <c>LoggerMessage.Define</c> logging pattern as the manual baseline.</item>
///   <item>Generated v2: state-based <c>ThreadLocalState</c> logging within the multi-target stack.</item>
///   <item>Single-target logger-only (v1, v2) shows the cost of logging alone, so the overhead
///   of adding Activity and Metrics can be read directly from the ratios.</item>
/// </list>
/// <para>
/// Logging is always enabled (full code path exercised).
/// The <see cref="HasListener"/> parameter controls whether Activity sampling is active.
/// </para>
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
[SimpleJob(RuntimeMoniker.Net47)]
[SimpleJob(RuntimeMoniker.Net48)]
[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class LoggerMultiTargetBenchmarks
{
	ActivityListener? _listener;

	ILoggerOnlyTelemetry _singleTargetV2 = default!;
	ILoggerV1OnlyTelemetry _singleTargetV1 = default!;
	IMultiTargetTelemetry _multiTargetV2 = default!;
	IMultiTargetV1Telemetry _multiTargetV1 = default!;
	ManualMultiTargetTelemetry _multiTargetManual = default!;

	/// <summary>
	/// When <c>true</c>, an <see cref="ActivityListener"/> sampling all activities is
	/// registered, so the Activity creation and event recording code paths are fully exercised.
	/// When <c>false</c>, no listener is present and the <c>HasListeners()</c> guard
	/// short-circuits Activity creation immediately.
	/// </summary>
	[Params(true, false)]
	public bool HasListener { get; set; }

	[GlobalSetup]
	public void Setup()
	{
		(_singleTargetV2, _singleTargetV1, _) = BenchmarkHelpers.CreateLoggerTelemetry(loggingEnabled: true);

		(_multiTargetV2, _multiTargetV1, _multiTargetManual) = BenchmarkHelpers.CreateMultiTargetTelemetryAll();

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

	// ---- Multi-target: start + complete ----

	[Benchmark(Baseline = true, Description = "Multi-target (manual): start + complete")]
	public void Manual_StartAndComplete()
	{
		using var activity = _multiTargetManual.StartOperation("op", 1);
		_multiTargetManual.OperationCompleted(activity, resultCode: 0, elapsedMs: 10);
	}

	[Benchmark(Description = "Multi-target (generated v1): start + complete")]
	public void Generated_V1_StartAndComplete()
	{
		using var activity = _multiTargetV1.StartOperation("op", 1);
		_multiTargetV1.OperationCompleted(activity, resultCode: 0, elapsedMs: 10);
	}

	[Benchmark(Description = "Multi-target (generated v2): start + complete")]
	public void Generated_V2_StartAndComplete()
	{
		using var activity = _multiTargetV2.StartOperation("op", 1);
		_multiTargetV2.OperationCompleted(activity, resultCode: 0, elapsedMs: 10);
	}

	// ---- Multi-target: full lifecycle (start + complete + record latency) ----

	[Benchmark(Description = "Multi-target (manual): full lifecycle")]
	public void Manual_FullLifecycle()
	{
		using var activity = _multiTargetManual.StartOperation("op", 1);
		_multiTargetManual.OperationCompleted(activity, resultCode: 0, elapsedMs: 10);
		_multiTargetManual.RecordLatency(10);
	}

	[Benchmark(Description = "Multi-target (generated v1): full lifecycle")]
	public void Generated_V1_FullLifecycle()
	{
		using var activity = _multiTargetV1.StartOperation("op", 1);
		_multiTargetV1.OperationCompleted(activity, resultCode: 0, elapsedMs: 10);
		_multiTargetV1.RecordLatency(10);
	}

	[Benchmark(Description = "Multi-target (generated v2): full lifecycle")]
	public void Generated_V2_FullLifecycle()
	{
		using var activity = _multiTargetV2.StartOperation("op", 1);
		_multiTargetV2.OperationCompleted(activity, resultCode: 0, elapsedMs: 10);
		_multiTargetV2.RecordLatency(10);
	}

	// ---- Single-target (logger only): reference for the marginal cost of Activity + Metrics ----

	[Benchmark(Description = "Single-target (generated v1): full lifecycle")]
	public void Generated_V1_SingleTarget_FullLifecycle()
	{
		_singleTargetV1.OperationStarted("op", 1);
		_singleTargetV1.OperationCompleted(resultCode: 200, elapsedMs: 10);
		_singleTargetV1.HighLatencyDetected("op", latencyMs: 100);
		_singleTargetV1.OperationFailed("err");
	}

	[Benchmark(Description = "Single-target (generated v2): full lifecycle")]
	public void Generated_V2_SingleTarget_FullLifecycle()
	{
		_singleTargetV2.OperationStarted("op", 1);
		_singleTargetV2.OperationCompleted(resultCode: 200, elapsedMs: 10);
		_singleTargetV2.HighLatencyDetected("op", latencyMs: 100);
		_singleTargetV2.OperationFailed("err");
	}
}
