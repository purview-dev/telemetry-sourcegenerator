namespace SampleApp.APIService.Services;

partial class WeatherServiceTests
{
	[Test]
	[Arguments(1)]
	[Arguments(4)]
	[Arguments(21)]
	[Arguments(221)]
	public async Task GetWeatherForecastsAsync_GivenRequestCountIsOutOfRange_ThrowsAndLogs(
		int requestCount,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var telemetry = CreateTelemetry();
		var service = CreateService(telemetry);

		// Act & Assert
		await Assert
			.That(async () =>
				await service.GetWeatherForecastsAsync(requestCount, cancellationToken)
			)
			.Throws<ArgumentOutOfRangeException>();

		telemetry.Received(1).RequestedCountIsTooSmall(Arg.Is(requestCount));
	}
}
