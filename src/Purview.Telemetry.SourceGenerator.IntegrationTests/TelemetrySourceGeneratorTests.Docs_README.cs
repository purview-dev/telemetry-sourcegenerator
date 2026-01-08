namespace Purview.Telemetry.SourceGenerator;

partial class TelemetrySourceGeneratorTests
{
	[Test]
	[Arguments(IncludeLoggerTypes.LoggerOnly)]
	[Arguments(IncludeLoggerTypes.Telemetry)]
	public async Task Generate_FromREADMESection_GeneratesTelemetry(IncludeLoggerTypes loggerTypes)
	{
		// Arrange
		const string basicTelemetry =
			"""

using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;
using Purview.Telemetry.Metrics;
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
			disableDependencyInjection: false,
			includeLoggerTypes: loggerTypes
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, parameters: loggerTypes);
	}

	[Test]
	public async Task Generate_FromWikiActivitiesSection_GeneratesTelemetry()
	{
		// Arrange
		const string basicTelemetry =
			"""

using Purview.Telemetry.Activities;
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
			disableDependencyInjection: false
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_FromWikiLoggingSection_GeneratesTelemetry()
	{
		// Arrange
		const string basicTelemetry =
			@"
using Purview.Telemetry.Logging;
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
			disableDependencyInjection: false
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_FromWikiMetricsSection_GeneratesTelemetry()
	{
		// Arrange
		const string basicTelemetry =
			@"
using Purview.Telemetry.Metrics;
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
			disableDependencyInjection: false
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_FromWikiMultiTargetingSection_GeneratesTelemetry()
	{
		// Arrange
		const string basicTelemetry =
			"""

using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;
using Purview.Telemetry.Metrics;
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
			disableDependencyInjection: false
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}
}
