using Microsoft.Extensions.DependencyInjection;
using SampleApp.MultiProject.Payments;

namespace SampleApp.MultiProject.Ordering;

/// <summary>
/// Registers this library and everything it depends on. The host calls only this, even though four
/// assemblies' worth of telemetry end up registered.
/// </summary>
public static class OrderingServiceCollectionExtensions
{
	public static IServiceCollection AddOrdering(this IServiceCollection services) =>
		services.AddPayments().AddOrderingTelemetry().AddSingleton<OrderBook>();
}
