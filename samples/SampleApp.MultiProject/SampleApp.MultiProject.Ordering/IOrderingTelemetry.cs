using System.Diagnostics;
using Purview.Telemetry;

namespace SampleApp.MultiProject.Ordering;

/// <summary>
/// All three targets from one interface, using the default names — which resolve to this assembly's
/// name, <c>SampleApp.MultiProject.Ordering</c>, for both the activity source and the meter. The
/// host's aggregate therefore mixes assembly-derived names with the explicit ones chosen by the
/// payments and notifications libraries.
/// </summary>
[ActivitySource]
[Logger]
[Meter]
public interface IOrderingTelemetry
{
	[Activity]
	Activity? PlacingOrder([Tag] string orderId);

	[Info]
	void OrderPlaced(string orderId, decimal total);

	[AutoCounter]
	void OrdersPlaced();
}
