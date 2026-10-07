using System.Diagnostics;
using Purview.Telemetry;

namespace SampleApp.MultiProject.Payments;

/// <summary>
/// Tracing and metrics with explicit names, and no logging. Both <c>multiproject-payments</c> and
/// the <c>multiproject.payments</c> meter are recorded in this assembly's metadata for the host to
/// aggregate.
/// </summary>
[ActivitySource("multiproject-payments")]
[Meter("multiproject.payments")]
public interface IPaymentTelemetry
{
	[Activity]
	Activity? ChargingCard([Tag] string orderId, [Tag] decimal amount);

	[Counter]
	void PaymentsProcessed([InstrumentMeasurement] int count, [Tag] string currency);

	[Histogram]
	void PaymentDuration([InstrumentMeasurement] double milliseconds);
}
