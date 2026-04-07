namespace SampleApp.APIService.Services;

public interface IWeatherService
{
	Task<ErrorOr<IEnumerable<WeatherForecast>>> GetWeatherForecastsAsync(
		int requestCount,
		CancellationToken cancellationToken = default
	);
}
