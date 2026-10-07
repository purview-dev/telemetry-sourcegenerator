using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SampleApp.MultiProject.Observability;

/// <summary>
/// Stands in for an Aspire <c>ServiceDefaults</c> project. Every other project in this solution
/// references this one, so its own reference graph contains none of them — it can never discover
/// their telemetry names for itself, no matter what the generator records. The host project, which
/// does reference them, aggregates the names and passes them in here.
/// </summary>
/// <remarks>
/// A real <c>ServiceDefaults</c> would hand the same two arrays to <c>AddSource(...)</c> and
/// <c>AddMeter(...)</c> on the OpenTelemetry tracing and metrics builders. This sample subscribes
/// with <see cref="ActivityListener"/> and <see cref="MeterListener"/> instead, so it stays free of
/// exporter configuration and prints what it receives.
/// </remarks>
public static class TelemetryDefaults
{
	/// <summary>
	/// Subscribes to exactly the supplied activity sources and meters, writing everything they
	/// produce to the console. Dispose the result to unsubscribe.
	/// </summary>
	public static IDisposable Listen(string[] activitySourceNames, string[] meterNames)
	{
		ArgumentNullException.ThrowIfNull(activitySourceNames);
		ArgumentNullException.ThrowIfNull(meterNames);

		ActivityListener activityListener = new()
		{
			ShouldListenTo = source => Array.IndexOf(activitySourceNames, source.Name) >= 0,
			Sample = static (ref _) => ActivitySamplingResult.AllDataAndRecorded,
			SampleUsingParentId = static (ref _) => ActivitySamplingResult.AllDataAndRecorded,
			ActivityStarted = static activity =>
				Console.WriteLine($"    [trace  ] start {activity.Source.Name}/{activity.DisplayName}"),
			ActivityStopped = static activity =>
				Console.WriteLine(
					$"    [trace  ] stop  {activity.Source.Name}/{activity.DisplayName} ({activity.Duration.TotalMilliseconds:F1} ms)"
				),
		};
		ActivitySource.AddActivityListener(activityListener);

		MeterListener meterListener = new()
		{
			InstrumentPublished = (instrument, listener) =>
			{
				if (Array.IndexOf(meterNames, instrument.Meter.Name) >= 0)
					listener.EnableMeasurementEvents(instrument);
			},
		};

		meterListener.SetMeasurementEventCallback<int>(
			static (instrument, measurement, _, _) => WriteMeasurement(instrument, measurement)
		);
		meterListener.SetMeasurementEventCallback<long>(
			static (instrument, measurement, _, _) => WriteMeasurement(instrument, measurement)
		);
		meterListener.SetMeasurementEventCallback<double>(
			static (instrument, measurement, _, _) => WriteMeasurement(instrument, measurement)
		);
		meterListener.Start();

		return new Subscription(activityListener, meterListener);
	}

	static void WriteMeasurement(Instrument instrument, object measurement) =>
		Console.WriteLine($"    [metric ] {instrument.Meter.Name}/{instrument.Name} = {measurement}");

	sealed class Subscription(ActivityListener activityListener, MeterListener meterListener) : IDisposable
	{
		public void Dispose()
		{
			activityListener.Dispose();
			meterListener.Dispose();
		}
	}
}
