using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
		var services = new ServiceCollection();
		services.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace));

		var provider = services.BuildServiceProvider();
		var logger = provider.GetRequiredService<ILogger<IMultiTargetTelemetry>>();
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
}
