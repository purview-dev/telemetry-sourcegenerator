using System.Diagnostics;
using Purview.Telemetry;

// ************************************************
// This is the sample from the README.md file.
// ************************************************

namespace SampleApp.APIService.Services;

[ActivitySource]
[Logger]
[Meter]
interface IEntityStoreTelemetry
{
	/// <summary>
	/// Creates and starts an Activity and adds the parameters as Tags and Baggage.
	/// </summary>
	[Activity]
	Activity? GettingEntityFromStore(int entityId, [Baggage] string serviceUrl);

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
	/// Generates a structured log message using an ILogger - defaults to Informational.
	/// </summary>
	[Log]
	void ProcessingEntity(int entityId, string updateState);

	/// <summary>
	/// Generates a structured log message using an ILogger, specifically defined as Informational.
	/// </summary>
	[Info]
	void ProcessingAnotherEntity(int entityId, string updateState);

	/// <summary>
	/// Adds 1 to a Counter{T} with the entityId as a Tag.
	/// </summary>
	[AutoCounter]
	void RetrievingEntity(int entityId);
}
