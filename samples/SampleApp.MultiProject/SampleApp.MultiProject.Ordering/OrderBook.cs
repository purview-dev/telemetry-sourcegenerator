using SampleApp.MultiProject.Payments;

namespace SampleApp.MultiProject.Ordering;

/// <summary>Places an order, charging it through the payments library.</summary>
public sealed class OrderBook(IOrderingTelemetry telemetry, PaymentProcessor paymentProcessor)
{
	public void PlaceOrder(string orderId, decimal total)
	{
		using var activity = telemetry.PlacingOrder(orderId);

		paymentProcessor.Charge(orderId, total);

		telemetry.OrdersPlaced();
		telemetry.OrderPlaced(orderId, total);
	}
}
