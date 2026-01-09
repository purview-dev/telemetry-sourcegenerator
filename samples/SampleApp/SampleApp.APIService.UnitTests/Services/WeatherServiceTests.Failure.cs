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

		// Act & Assert
		var ex = await Assert
			.That(async () =>
				await service.GetWeatherForecastsAsync(requestCount, cancellationToken)
			)
			.ThrowsExactly<Exception>();

		telemetry.Received(1).FailedToRetrieveForecast(Arg.Is(activity), Arg.Is(ex!));
	}
}
