using PackedTelemetry.Library;

namespace PackedTelemetry;

/// <summary>
/// The service every consumer calls: one order placed through the generated telemetry and the library's.
/// </summary>
public sealed class OrderWorkflow(IOrderTelemetry telemetry, StockReserver stock)
{
	public const string Sku = "SKU-1";

	public void Place(string orderId, string customerId, double total)
	{
		telemetry.OrdersInFlight(1);
		using (var activity = telemetry.PlacingOrder(orderId, customerId))
		{
			telemetry.OrderValidated(activity, 3);
			stock.Reserve(Sku, 2);
			telemetry.OrderTotal(total, "GBP");
			telemetry.OrderShipped(orderId, ["dhl", "ups"]);

			try
			{
				throw new InvalidOperationException("card declined");
			}
			catch (InvalidOperationException exception)
			{
				telemetry.OrderFailed(activity, exception, orderId);
			}
		}
		telemetry.OrdersInFlight(-1);
	}
}
