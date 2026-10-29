using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Purview.Telemetry;

namespace PackedTelemetry.Library;

/// <summary>
/// Telemetry generated in a class library: every consumer references this project, so the packed generator runs in
/// two assemblies of the same application, and the application aggregates this one's names.
/// </summary>
[ActivitySource("packed.inventory")]
[Meter("packed.inventory")]
public interface IInventoryTelemetry
{
	[Activity("stock.reserve")]
	Activity? ReservingStock([Tag] string sku);

	[Counter("stock.reserved")]
	void StockReserved([InstrumentMeasurement] int quantity, [Tag] string sku);
}

/// <summary>Reserves stock, emitting the library's own telemetry.</summary>
public sealed class StockReserver(IInventoryTelemetry telemetry)
{
	public void Reserve(string sku, int quantity)
	{
		using var activity = telemetry.ReservingStock(sku);
		telemetry.StockReserved(quantity, sku);
	}
}

public static class InventoryServiceCollectionExtensions
{
	/// <summary>
	/// The library owns its registration: the generated <c>AddInventoryTelemetry()</c> is internal to it.
	/// </summary>
	public static IServiceCollection AddInventory(this IServiceCollection services) =>
		services.AddInventoryTelemetry().AddSingleton<StockReserver>();
}
