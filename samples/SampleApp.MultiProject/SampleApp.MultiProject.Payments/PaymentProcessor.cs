using System.Diagnostics;
using SampleApp.MultiProject.Notifications;

namespace SampleApp.MultiProject.Payments;

/// <summary>Charges the card for an order, then asks the notifications library for a receipt.</summary>
public sealed class PaymentProcessor(IPaymentTelemetry telemetry, ReceiptSender receiptSender)
{
	public void Charge(string orderId, decimal amount)
	{
		using var activity = telemetry.ChargingCard(orderId, amount);

		var timestamp = Stopwatch.GetTimestamp();

		// A real implementation would call the payment provider here.
		receiptSender.SendReceipt(orderId);

		telemetry.PaymentDuration(Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds);
		telemetry.PaymentsProcessed(1, "GBP");
	}
}
