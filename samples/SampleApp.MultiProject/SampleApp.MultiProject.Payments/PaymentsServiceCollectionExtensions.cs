using Microsoft.Extensions.DependencyInjection;
using SampleApp.MultiProject.Notifications;

namespace SampleApp.MultiProject.Payments;

/// <summary>
/// Registers this library and everything it depends on, so the host composes one call per direct
/// reference rather than one per assembly.
/// </summary>
public static class PaymentsServiceCollectionExtensions
{
	public static IServiceCollection AddPayments(this IServiceCollection services) =>
		services.AddNotifications().AddPaymentTelemetry().AddSingleton<PaymentProcessor>();
}
