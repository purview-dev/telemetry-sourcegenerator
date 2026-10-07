namespace SampleApp.APIService.Services;

public partial class WeatherServiceTests
{
	static WeatherService CreateService(
		Mock<IWeatherServiceTelemetry>? telemetry,
		bool throwOnRNG = false,
		Mock<IWeatherForecastTelemetry>? forecastTelemetry = null
	) =>
		new(
			telemetry: (telemetry ?? Mock.Of<IWeatherServiceTelemetry>()).Object,
			// The shared library's validator, with its own (shared-library) telemetry mocked.
			validator: new((forecastTelemetry ?? Mock.Of<IWeatherForecastTelemetry>()).Object),
			rng: () => throwOnRNG ? 8 : 1 // 8 is out magic eight-ball number - it throws randomly in simulated use.
		);

	static Mock<IWeatherServiceTelemetry> CreateTelemetry() => Mock.Of<IWeatherServiceTelemetry>();

	static Mock<IWeatherForecastTelemetry> CreateForecastTelemetry() => Mock.Of<IWeatherForecastTelemetry>();
}
