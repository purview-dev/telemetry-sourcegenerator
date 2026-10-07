namespace SampleApp.APIService.Services;

interface IWeatherService
{
	Task<ErrorOr<IEnumerable<WeatherForecast>>> GetWeatherForecastsAsync(
		int requestCount,
		CancellationToken cancellationToken = default
	);
}
