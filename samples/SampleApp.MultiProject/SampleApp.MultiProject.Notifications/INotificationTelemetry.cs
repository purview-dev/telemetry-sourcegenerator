using System.Diagnostics;
using Purview.Telemetry;

namespace SampleApp.MultiProject.Notifications;

/// <summary>
/// Tracing only, with an explicit source name — this library wants nothing to do with logging or
/// metrics. Nothing here registers the name anywhere: the generator records
/// <c>multiproject-notifications</c> in this assembly's metadata and the host picks it up.
/// </summary>
/// <remarks>
/// Only <c>SampleApp.MultiProject.Payments</c> references this project, so the host sees it purely
/// as a transitive reference — the aggregate still includes it.
/// </remarks>
[ActivitySource("multiproject-notifications")]
public interface INotificationTelemetry
{
	[Activity]
	Activity? SendingReceipt([Tag] string orderId);
}
