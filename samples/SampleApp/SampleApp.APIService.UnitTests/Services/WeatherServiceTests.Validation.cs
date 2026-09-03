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

		// Act
		var err = await service.GetWeatherForecastsAsync(requestCount, cancellationToken);

		// Assert
		await Assert.That(err.IsError).IsTrue();
		await Assert.That(err.FirstError.Code).IsEqualTo("RequestCount.Invalid");
		await Assert.That(err.FirstError.Type).IsEqualTo(ErrorOr.ErrorType.Validation);

		telemetry.RequestedCountIsOutOfRange(Is(requestCount)).WasCalled(Times.Once);
	}
}
