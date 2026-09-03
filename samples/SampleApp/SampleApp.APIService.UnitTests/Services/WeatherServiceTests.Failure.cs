using System.Diagnostics;

namespace SampleApp.APIService.Services;

partial class WeatherServiceTests
{
	[Test]
	public async Task GetWeatherForecastsAsync_GivenSimulatedUpstreamFails_CallsFailureActivityEventAndLog(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const int requestCount = 10;
		var telemetry = CreateTelemetry();
		var service = CreateService(telemetry, throwOnRNG: true);

		using Activity activity = new(
			nameof(GetWeatherForecastsAsync_GivenSimulatedUpstreamFails_CallsFailureActivityEventAndLog)
		);

		telemetry.GettingWeatherForecast(Any<string>(), Is(requestCount)).Returns(activity);

		// Act
		var err = await service.GetWeatherForecastsAsync(requestCount, cancellationToken);

		// Assert
		await Assert.That(err.IsError).IsTrue();
		await Assert.That(err.FirstError.Code).IsEqualTo("WeatherForecast.RetrievalFailed");
		await Assert.That(err.FirstError.Type).IsEqualTo(ErrorOr.ErrorType.Failure);

		telemetry.FailedToRetrieveForecast(Is<Activity?>(activity), Any<Exception>()).WasCalled(Times.Once);
	}
}
