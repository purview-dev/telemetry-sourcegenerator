using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Purview.Telemetry.Benchmarks.Manual;
using Purview.Telemetry.Benchmarks.Telemetry;

namespace Purview.Telemetry.Benchmarks;

/// <summary>
/// Factory helpers for constructing telemetry instances used across benchmarks.
/// </summary>
static class BenchmarkHelpers
{
	/// <summary>
	/// Creates an ActivityListener that samples all activities, simulating a real
	/// observability backend (e.g. OpenTelemetry exporter) being attached.
	/// </summary>
	public static ActivityListener CreateAllSamplingListener()
	{
		return new ActivityListener
		{
			ShouldListenTo = _ => true,
			Sample = static (ref ActivityCreationOptions<ActivityContext> _) =>
				ActivitySamplingResult.AllDataAndRecorded,
			SampleUsingParentId = static (ref ActivityCreationOptions<string> _) =>
				ActivitySamplingResult.AllDataAndRecorded,
		};
	}

	/// <summary>
	/// Creates the source-generator-produced implementation of <see cref="IActivityOnlyTelemetry"/>.
	/// The generated class is <c>ActivityOnlyTelemetryCore</c> in the
	/// <c>Purview.Telemetry.Benchmarks.Telemetry</c> namespace.
	/// </summary>
	public static IActivityOnlyTelemetry CreateGeneratedActivityTelemetry()
	{
		return new ActivityOnlyTelemetryCore();
	}

	/// <summary>
	/// Creates both the source-generator-produced implementation of <see cref="IMultiTargetTelemetry"/>
	/// and the hand-written <see cref="ManualMultiTargetTelemetry"/>, using a shared DI container
	/// so that ILogger and IMeterFactory are wired up identically for a fair comparison.
	/// </summary>
	public static (IMultiTargetTelemetry Generated, ManualMultiTargetTelemetry Manual) CreateMultiTargetTelemetry()
	{
		var logger = CreateEnabledLogger<IMultiTargetTelemetry>();
		var meterFactory = new SimpleMeterFactory();

		var generated = new MultiTargetTelemetryCore(logger, meterFactory);
		var manual = new ManualMultiTargetTelemetry(logger, meterFactory);

		return (generated, manual);
	}

	/// <summary>
	/// Creates both the source-generator-produced metrics-only implementations.
	/// </summary>
	public static (IMetricsFewTagsTelemetry FewTags, IMetricsManyTagsTelemetry ManyTags) CreateMetricsTelemetry()
	{
		var meterFactory = new SimpleMeterFactory();
		var fewTags = new MetricsFewTagsTelemetryCore(meterFactory);
		var manyTags = new MetricsManyTagsTelemetryCore(meterFactory);
		return (fewTags, manyTags);
	}

	/// <summary>
	/// Creates the v2 (state-based ThreadLocalState) and v1 (LoggerMessage.Define) generated logger
	/// implementations alongside the hand-written <see cref="ManualLoggerMessageTelemetry"/> baseline,
	/// all backed by either an always-enabled no-op logger or <see cref="NullLogger{T}"/> (always disabled).
	/// </summary>
	/// <param name="loggingEnabled">
	/// When <c>true</c>, a no-op logger with <see cref="ILogger.IsEnabled"/> always returning
	/// <c>true</c> is used, so the full logging code path is exercised — the <c>IsEnabled</c>
	/// guard passes, state is populated (delegates / ThreadLocalState filled), and
	/// <c>ILogger.Log</c> is called. The formatter lambda is NOT invoked because the logger
	/// is a no-op; no string allocation occurs from v1/v2 generated code or
	/// <see cref="ManualLoggerMessageTelemetry"/>.
	/// When <c>false</c>, <see cref="NullLogger{T}"/> is used, so the <c>IsEnabled</c> guard
	/// short-circuits immediately with no further work.
	/// </param>
	public static (
		ILoggerOnlyTelemetry GeneratedV2,
		ILoggerV1OnlyTelemetry GeneratedV1,
		ManualLoggerMessageTelemetry Manual
	) CreateLoggerTelemetry(bool loggingEnabled)
	{
		ILogger<ILoggerOnlyTelemetry> loggerV2 = loggingEnabled
			? new EnabledNullLogger<ILoggerOnlyTelemetry>()
			: NullLogger<ILoggerOnlyTelemetry>.Instance;

		ILogger<ILoggerV1OnlyTelemetry> loggerV1 = loggingEnabled
			? new EnabledNullLogger<ILoggerV1OnlyTelemetry>()
			: NullLogger<ILoggerV1OnlyTelemetry>.Instance;

		ILogger loggerBase = loggingEnabled
			? new EnabledNullLogger<object>()
			: NullLogger.Instance;

		var generatedV2 = new LoggerOnlyTelemetryCore(loggerV2);
		var generatedV1 = new LoggerV1OnlyTelemetryCore(loggerV1);
		var manual = new ManualLoggerMessageTelemetry(loggerBase);

		return (generatedV2, generatedV1, manual);
	}

	/// <summary>
	/// Creates the v2 and v1 multi-target (Activity + Logging + Metrics) generated implementations
	/// alongside the hand-written <see cref="ManualMultiTargetTelemetry"/>.
	/// Logging is always enabled (full code path exercised); activity sampling is controlled
	/// separately via the <c>HasListener</c> benchmark parameter.
	/// </summary>
	public static (
		IMultiTargetTelemetry GeneratedV2,
		IMultiTargetV1Telemetry GeneratedV1,
		ManualMultiTargetTelemetry Manual
	) CreateMultiTargetTelemetryAll()
	{
		var loggerV2 = CreateEnabledLogger<IMultiTargetTelemetry>();
		var loggerV1 = CreateEnabledLogger<IMultiTargetV1Telemetry>();
		var loggerManual = CreateEnabledLogger<ManualMultiTargetTelemetry>();
		var meterFactory = new SimpleMeterFactory();

		var generatedV2 = new MultiTargetTelemetryCore(loggerV2, meterFactory);
		var generatedV1 = new MultiTargetV1TelemetryCore(loggerV1, meterFactory);
		var manual = new ManualMultiTargetTelemetry(loggerManual, meterFactory);

		return (generatedV2, generatedV1, manual);
	}

	/// <summary>
	/// Creates an <see cref="ILogger{T}"/> whose <see cref="ILogger.IsEnabled"/> always
	/// returns <c>true</c> (for all levels except <see cref="LogLevel.None"/>), but whose
	/// <see cref="ILogger.Log{TState}"/> is a no-op. This ensures the full logging code
	/// path — including message formatting — is exercised during benchmarks without
	/// incurring I/O or allocation costs from a real sink.
	/// </summary>
	static ILogger<T> CreateEnabledLogger<T>() => new EnabledNullLogger<T>();

	/// <summary>
	/// Minimal <see cref="IMeterFactory"/> that creates real <see cref="Meter"/> instances
	/// without requiring the full Microsoft.Extensions.Diagnostics DI pipeline.
	/// Each <see cref="Meter"/> is tracked and disposed when the factory itself is disposed.
	/// </summary>
	sealed class SimpleMeterFactory : IMeterFactory
	{
		readonly List<Meter> _meters = [];

		public Meter Create(MeterOptions options)
		{
			var meter = new Meter(options.Name, options.Version, options.Tags, scope: null);
			_meters.Add(meter);
			return meter;
		}

		public void Dispose()
		{
			foreach (var meter in _meters)
				meter.Dispose();
			_meters.Clear();
		}
	}

	/// <summary>
	/// A no-op <see cref="ILogger{T}"/> whose <see cref="IsEnabled"/> always returns
	/// <c>true</c> (except for <see cref="LogLevel.None"/>). Used in benchmarks to exercise
	/// the full logging code path — including <c>IsEnabled</c> checks, delegate invocation,
	/// and message formatting — without any actual I/O.
	/// </summary>
	sealed class EnabledNullLogger<T> : ILogger<T>
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
			=> NullScope.Instance;

		public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter
		)
		{
			// No-op: the state is received (delegates/ThreadLocalState already populated by caller)
			// but the formatter is intentionally not invoked — no string allocation occurs here.
		}

		sealed class NullScope : IDisposable
		{
			public static readonly NullScope Instance = new();
			public void Dispose() { }
		}
	}
}
