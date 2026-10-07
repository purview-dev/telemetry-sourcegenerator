using Microsoft.Extensions.DependencyInjection;

namespace SampleApp;

/// <summary>
/// The library owns its own registration, including the generated
/// <c>AddWeatherForecastTelemetry()</c> extension, which is <see langword="internal"/> to this
/// assembly. A host calls <see cref="AddWeatherForecastValidation"/> and gets both.
/// </summary>
public static class WeatherForecastServiceCollectionExtensions
{
	public static IServiceCollection AddWeatherForecastValidation(this IServiceCollection services) =>
		services.AddWeatherForecastTelemetry().AddSingleton<WeatherForecastValidator>();
}
