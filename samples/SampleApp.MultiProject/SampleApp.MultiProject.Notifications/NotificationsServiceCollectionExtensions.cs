using Microsoft.Extensions.DependencyInjection;

namespace SampleApp.MultiProject.Notifications;

/// <summary>
/// The library owns its own registration, including the generated
/// <c>AddNotificationTelemetry()</c> extension (which is <see langword="internal"/> to this
/// assembly by default).
/// </summary>
public static class NotificationsServiceCollectionExtensions
{
	public static IServiceCollection AddNotifications(this IServiceCollection services) =>
		services.AddNotificationTelemetry().AddSingleton<ReceiptSender>();
}
