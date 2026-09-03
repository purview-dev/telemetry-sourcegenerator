namespace Purview.Telemetry.SourceGenerator;

partial class TelemetrySourceGeneratorTests
{
	[Test]
	public async Task Generate_FromREADMESection_GeneratesTelemetry(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicTelemetry = """

using System.Diagnostics;

[ActivitySource]
[Logger]
[Meter]
interface IEntityStoreTelemetry
{
    /// <summary>
    /// Creates and starts an Activity and adds the parameters as Tags and Baggage.
    /// </summary>
    [Activity]
    Activity? GettingEntityFromStore(int entityId, [Baggage]string serviceUrl);

    /// <summary>
    /// Adds an ActivityEvent to the Activity with the parameters as Tags.
    /// </summary>
    [Event]
    void GetDuration(Activity? activity, int durationInMS);

    /// <summary>
    /// Adds the parameters as Baggage to the Activity.
    /// </summary>
    [Context]
    void RetrievedEntity(Activity? activity, float totalValue, int lastUpdatedByUserId);

    /// <summary>
    /// A scoped logging method.
    /// </summary>
    [Log]
    IDisposable AScopedLogEntry(int parentEntityId);

    /// <summary>
    /// Generates a structured log message using an ILogger - defaults to Informational.
    /// </summary>
    [Log]
    void LogMessage(int entityId, string updateState);

    /// <summary>
    /// Generates a structured log message using an ILogger, specifically defined as Informational.
    /// </summary>
    [Info]
    void ExplicitInfoMessage(int entityId, string updateState);

    /// <summary>
    /// Generates a structured log message using an ILogger, specifically defined as Error.
    /// </summary>
    [Error("An explicit error message. The entity Id is {EntityId}, and the error is {Exception}.")]
    void ExplicitErrorMessage(int entityId, Exception exception);

    /// <summary>
    /// Adds 1 to a Counter<T> with the entityId as a Tag.
    /// </summary>
    [AutoCounter]
    void RetrievingEntity(int entityId);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicTelemetry,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

		// Assert
		var query = generationResult.Generated();
		await Assert
			.That(query.HasMethod("GettingEntityFromStore"))
			.IsTrue()
			.Because("the generated class must contain the activity method");
		await Assert
			.That(query.HasMethod("GetDuration"))
			.IsTrue()
			.Because("the generated class must contain the event method");
		await Assert
			.That(query.HasMethod("RetrievedEntity"))
			.IsTrue()
			.Because("the generated class must contain the context method");
		await Assert
			.That(query.HasMethod("AScopedLogEntry"))
			.IsTrue()
			.Because("the generated class must contain the scoped log method");
		await Assert
			.That(query.HasMethod("LogMessage"))
			.IsTrue()
			.Because("the generated class must contain the log method");
		await Assert
			.That(query.HasMethod("ExplicitInfoMessage"))
			.IsTrue()
			.Because("the generated class must contain the info log method");
		await Assert
			.That(query.HasMethod("ExplicitErrorMessage"))
			.IsTrue()
			.Because("the generated class must contain the error log method");
		await Assert
			.That(query.HasMethod("RetrievingEntity"))
			.IsTrue()
			.Because("the generated class must contain the auto-counter method");
	}

	[Test]
	public async Task Generate_FromWikiActivitiesSection_GeneratesTelemetry(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicTelemetry = """

using System.Diagnostics;

[ActivitySource("some-activity")]
interface IActivityTelemetry
{
    [Activity]
    Activity? GettingItemFromCache([Baggage]string key, [Tag]string itemType);

    [Event("cachemiss")]
    void Miss(Activity? activity);

    [Event("cachehit")]
    void Hit(Activity? activity);

    [Event]
    void Error(Activity? activity, Exception ex);

    [Event]
    void Finished(Activity? activity, [Tag]TimeSpan duration);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicTelemetry,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG3021");
	}

	[Test]
	public async Task Generate_FromWikiLoggingSection_GeneratesTelemetry(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicTelemetry =
			@"
using Microsoft.Extensions.Logging;

[Logger]
interface ILoggingTelemetry
{
    [Log]
    IDisposable? ProcessingWorkItem(Guid id);

    [Log(LogLevel.Trace)]
    void ProcessingItemType(ItemTypes itemType);

    [Log(LogLevel.Error)]
    void FailedToProcessWorkItem(Exception ex);

    [Log(LogLevel.Information)]
    void ProcessingComplete(bool success, TimeSpan duration);
}

enum ItemTypes
{
	Unknown,
	File,
	Folder,
	Link
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicTelemetry,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

		// Assert
		var query = generationResult.Generated();
		await Assert
			.That(query.HasMethod("ProcessingWorkItem"))
			.IsTrue()
			.Because("the generated logger must contain the scoped log method");
		await Assert
			.That(query.HasMethod("ProcessingItemType"))
			.IsTrue()
			.Because("the generated logger must contain the item-type log method");
		await Assert
			.That(query.HasMethod("FailedToProcessWorkItem"))
			.IsTrue()
			.Because("the generated logger must contain the error log method");
		await Assert
			.That(query.HasMethod("ProcessingComplete"))
			.IsTrue()
			.Because("the generated logger must contain the completion log method");
	}

	[Test]
	public async Task Generate_FromWikiMetricsSection_GeneratesTelemetry(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicTelemetry =
			@"
using System.Collections.Generic;
using System.Diagnostics.Metrics;

[Meter]
interface IMeterTelemetry
{
    [AutoCounter]
    void AutoCounterMeter([Tag]string someValue);

    [Counter(AutoIncrement = true)]
    void AutoIncrementMeter([Tag]string someValue);

    [Counter]
    void CounterMeter([InstrumentMeasurement]int measurement, [Tag]float someValue);

    [Histogram]
    void HistogramMeter([InstrumentMeasurement]int measurement, [Tag]int someValue, [Tag]bool anotherValue);

    [ObservableCounter]
    void ObservableCounterMeter(Func<float> measurement, [Tag]double someValue);

    [ObservableGauge]
    void ObservableGaugeMeter(Func<Measurement<float>> measurement, [Tag]double someValue);

    [ObservableUpDownCounter]
    void ObservableUpDownCounter(Func<IEnumerable<Measurement<byte>>> measurement, [Tag]double someValue);

    [UpDownCounter]
    void UpDownCounterMeter([InstrumentMeasurement]decimal measurement, [Tag]byte someValue);
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicTelemetry,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

		// Assert
		var query = generationResult.Generated();
		await Assert
			.That(query.HasMethod("AutoCounterMeter"))
			.IsTrue()
			.Because("the generated metrics class must contain the auto-counter method");
		await Assert
			.That(query.HasMethod("AutoIncrementMeter"))
			.IsTrue()
			.Because("the generated metrics class must contain the auto-increment counter method");
		await Assert
			.That(query.HasMethod("CounterMeter"))
			.IsTrue()
			.Because("the generated metrics class must contain the counter method");
		await Assert
			.That(query.HasMethod("HistogramMeter"))
			.IsTrue()
			.Because("the generated metrics class must contain the histogram method");
		await Assert
			.That(query.HasMethod("ObservableCounterMeter"))
			.IsTrue()
			.Because("the generated metrics class must contain the observable counter method");
		await Assert
			.That(query.HasMethod("ObservableGaugeMeter"))
			.IsTrue()
			.Because("the generated metrics class must contain the observable gauge method");
		await Assert
			.That(query.HasMethod("ObservableUpDownCounter"))
			.IsTrue()
			.Because("the generated metrics class must contain the observable up-down counter method");
		await Assert
			.That(query.HasMethod("UpDownCounterMeter"))
			.IsTrue()
			.Because("the generated metrics class must contain the up-down counter method");
	}

	[Test]
	public async Task Generate_FromWikiMultiTargetingSection_GeneratesTelemetry(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicTelemetry = """

using System.Diagnostics;

[ActivitySource("multi-targeting")]
[Logger]
[Meter]
interface IServiceTelemetry
{
    [Activity]
    Activity? StartAnActivity(int tagIntParam, [Baggage]string entityId);

    [Event]
    void AnInterestingEvent(Activity? activity, float aTagValue);

    [Context]
    void InterestingInfo(Activity? activity, float anotherTagValue, int intTagValue);

    [Log]
    void ProcessingEntity(int entityId, string property1);

    [Counter(AutoIncrement = true)]
    void AnAutoIncrement([Tag]int value);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicTelemetry,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

		// Assert
		var query = generationResult.Generated();
		await Assert
			.That(query.HasMethod("StartAnActivity"))
			.IsTrue()
			.Because("the generated class must contain the activity method");
		await Assert
			.That(query.HasMethod("AnInterestingEvent"))
			.IsTrue()
			.Because("the generated class must contain the event method");
		await Assert
			.That(query.HasMethod("InterestingInfo"))
			.IsTrue()
			.Because("the generated class must contain the context method");
		await Assert
			.That(query.HasMethod("ProcessingEntity"))
			.IsTrue()
			.Because("the generated class must contain the log method");
		await Assert
			.That(query.HasMethod("AnAutoIncrement"))
			.IsTrue()
			.Because("the generated class must contain the auto-increment counter method");
	}
}
