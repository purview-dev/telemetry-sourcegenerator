namespace SampleApp.APIService.Services;

partial class WeatherServiceTests
{
	[Test]
	[Arguments(5)]
	[Arguments(10)]
	[Arguments(20)]
	public async Task GetWeatherForecastsAsync_GivenRequestCountIsWithinRange_CallsGettingUpstreamTelemetry(
		int requestCount,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var telemetry = CreateTelemetry();
		var service = CreateService(telemetry);

		// Act
		await service.GetWeatherForecastsAsync(requestCount, cancellationToken);

		// Assert
		telemetry.GettingWeatherForecast(Any<string>(), Is(requestCount)).WasCalled(Times.Once);
	}
}
