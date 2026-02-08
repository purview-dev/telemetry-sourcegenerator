using System.Diagnostics;
using Purview.Telemetry;

/* This is the interface from the README.md
 * It demonstrates how you can use Purview's multi-target attributes to
 * generate multiple types of telemetry from a single method.
 */

// Multi-target interface: generates Activities, Logging, AND Metrics from combined methods
[ActivitySource]
[Logger]
[Meter]
interface IEntityStoreTelemetry
{
	// MULTI-TARGET: Creates Activity + Logs Info + Increments Counter - all from one method!
	[Activity]
	[Info]
	[AutoCounter]
	Activity? GettingEntityFromStore(int entityId, [Baggage] string serviceUrl);

	// MULTI-TARGET: Adds ActivityEvent + Logs the duration as Trace.
	[Event]
	[Trace]
	void GetDuration(Activity? activity, int durationInMS);

	// Single-target examples (when you only need one telemetry type):

	// Activity-only: Adds Baggage to the Activity
	[Context]
	void RetrievedEntity(Activity? activity, float totalValue, int lastUpdatedByUserId);

	// Log-only: Structured log message
	[Warning]
	void EntityNotFound(int entityId);

	// Metric-only: Histogram for tracking values
	[Histogram]
	void RecordEntitySize(int sizeInBytes);
}
