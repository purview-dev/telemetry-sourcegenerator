namespace SampleApp.MultiProject.Notifications;

/// <summary>Sends the receipt for a completed order.</summary>
public sealed class ReceiptSender(INotificationTelemetry telemetry)
{
	public void SendReceipt(string orderId)
	{
		using var activity = telemetry.SendingReceipt(orderId);

		// A real implementation would hand the receipt to an email or push provider here.
	}
}
