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
			nameof(
				GetWeatherForecastsAsync_GivenSimulatedUpstreamFails_CallsFailureActivityEventAndLog
			)
		);

		telemetry.GettingWeatherForecast(Arg.Any<string>(), requestCount).Returns(activity);

		// Act
		var err = await service.GetWeatherForecastsAsync(requestCount, cancellationToken);

		// Assert
		await Assert.That(err.IsError).IsTrue();
		await Assert.That(err.FirstError.Code).IsEqualTo("WeatherForecast.RetrievalFailed");
		await Assert.That(err.FirstError.Type).IsEqualTo(ErrorOr.ErrorType.Failure);

		telemetry.Received(1).FailedToRetrieveForecast(Arg.Is(activity), Arg.Any<Exception>());
	}
}
