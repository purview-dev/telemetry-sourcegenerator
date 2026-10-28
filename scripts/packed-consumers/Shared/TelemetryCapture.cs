using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace PackedTelemetry;

public sealed record CapturedEvent(string Name, IReadOnlyDictionary<string, string?> Tags);

/// <param name="CreatedByFactory">
/// Whether the activity's source was created by an <see cref="ActivitySourceFactory"/> (.NET 11), which records itself
/// as the source's <see cref="ActivitySource.Scope"/>; a static <c>new ActivitySource(...)</c> has no scope.
/// </param>
public sealed record CapturedActivity(
	string Source,
	string Name,
	IReadOnlyDictionary<string, string?> Tags,
	IReadOnlyDictionary<string, string?> Baggage,
	IReadOnlyList<CapturedEvent> Events,
	bool CreatedByFactory
);

public sealed record CapturedMeasurement(
	string Meter,
	string Instrument,
	string? Unit,
	double Value,
	IReadOnlyDictionary<string, string?> Tags
);

public sealed record CapturedLog(
	string Category,
	LogLevel Level,
	string? EventName,
	string Message,
	IReadOnlyDictionary<string, string?> State,
	Exception? Exception
);

/// <summary>
/// An in-process <see cref="ActivityListener"/>, <see cref="MeterListener"/> and <see cref="ILoggerProvider"/> that
/// record everything the generated telemetry emits, so a consumer can assert on it.
/// </summary>
/// <remarks>
/// The logger reports itself enabled from <see cref="MinimumLevel"/> up, but records every <c>Log</c> call it
/// receives whatever the level. Register it with the logging minimum at <see cref="LogLevel.Trace"/>, so the logging
/// infrastructure filters nothing: then a log entry below <see cref="MinimumLevel"/> can only be absent because the
/// generated code asked <c>IsEnabled</c> and skipped it.
/// </remarks>
public sealed class TelemetryCapture : ILoggerProvider
{
	readonly ActivityListener _activityListener;
	readonly MeterListener _meterListener;
	readonly ConcurrentQueue<CapturedActivity> _activities = new();
	readonly ConcurrentQueue<CapturedMeasurement> _measurements = new();
	readonly ConcurrentQueue<CapturedLog> _logs = new();
	int _isEnabledCalls;

	public TelemetryCapture(string[] names, LogLevel minimumLevel = LogLevel.Information)
	{
		MinimumLevel = minimumLevel;

		_activityListener = new()
		{
			ShouldListenTo = source => Array.IndexOf(names, source.Name) >= 0,
			Sample = static (ref _) => ActivitySamplingResult.AllDataAndRecorded,
			SampleUsingParentId = static (ref _) => ActivitySamplingResult.AllDataAndRecorded,
			ActivityStopped = activity =>
				_activities.Enqueue(
					new(
						activity.Source.Name,
						activity.OperationName,
						ToDictionary(activity.TagObjects),
						ToDictionary(activity.Baggage),
						[.. activity.Events.Select(static e => new CapturedEvent(e.Name, ToDictionary(e.Tags)))],
						activity.Source.Scope is ActivitySourceFactory
					)
				),
		};
		ActivitySource.AddActivityListener(_activityListener);

		_meterListener = new()
		{
			InstrumentPublished = (instrument, listener) =>
			{
				if (Array.IndexOf(names, instrument.Meter.Name) >= 0)
					listener.EnableMeasurementEvents(instrument);
			},
		};
		_meterListener.SetMeasurementEventCallback<int>((i, m, t, _) => Record(i, m, t));
		_meterListener.SetMeasurementEventCallback<long>((i, m, t, _) => Record(i, m, t));
		_meterListener.SetMeasurementEventCallback<float>((i, m, t, _) => Record(i, m, t));
		_meterListener.SetMeasurementEventCallback<double>((i, m, t, _) => Record(i, m, t));
		_meterListener.SetMeasurementEventCallback<decimal>((i, m, t, _) => Record(i, (double)m, t));
		_meterListener.Start();
	}

	public LogLevel MinimumLevel { get; }

	public IReadOnlyCollection<CapturedActivity> Activities => _activities;

	public IReadOnlyCollection<CapturedMeasurement> Measurements => _measurements;

	public IReadOnlyCollection<CapturedLog> Logs => _logs;

	/// <summary>
	/// How often a <c>PackedTelemetry.*</c> logger - one the generated code created - was asked whether a level was enabled.
	/// </summary>
	public int IsEnabledCalls => Volatile.Read(ref _isEnabledCalls);

	public ILogger CreateLogger(string categoryName) => new CapturingLogger(this, categoryName);

	public void Dispose()
	{
		_activityListener.Dispose();
		_meterListener.Dispose();
	}

	void Record(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
	{
		Dictionary<string, string?> captured = [];
		foreach (var tag in tags)
			captured[tag.Key] = Format(tag.Value);

		_measurements.Enqueue(new(instrument.Meter.Name, instrument.Name, instrument.Unit, value, captured));
	}

	static Dictionary<string, string?> ToDictionary<T>(IEnumerable<KeyValuePair<string, T>> pairs)
	{
		Dictionary<string, string?> result = [];
		foreach (var pair in pairs)
			result[pair.Key] = Format(pair.Value);

		return result;
	}

	static string? Format(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture);

	sealed class CapturingLogger(TelemetryCapture owner, string category) : ILogger
	{
		public IDisposable? BeginScope<TState>(TState state)
			where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel)
		{
			if (category.StartsWith("PackedTelemetry.", StringComparison.Ordinal))
				Interlocked.Increment(ref owner._isEnabledCalls);

			return logLevel >= owner.MinimumLevel;
		}

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter
		)
		{
			Dictionary<string, string?> captured = [];
			if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
			{
				foreach (var pair in pairs)
					captured[pair.Key] = Format(pair.Value);
			}

			owner._logs.Enqueue(
				new(category, logLevel, eventId.Name, formatter(state, exception), captured, exception)
			);
		}
	}
}
