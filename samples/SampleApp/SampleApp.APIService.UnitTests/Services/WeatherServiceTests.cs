namespace SampleApp.APIService.Services;

public partial class WeatherServiceTests
{
	static WeatherService CreateService(Mock<IWeatherServiceTelemetry>? telemetry, bool throwOnRNG = false) =>
		new(
			telemetry: (telemetry ?? Mock.Of<IWeatherServiceTelemetry>()).Object,
			rng: () => throwOnRNG ? 8 : 1 // 8 is out magic eight-ball number - it throws randomly in simulated use.
		);

	static Mock<IWeatherServiceTelemetry> CreateTelemetry() => Mock.Of<IWeatherServiceTelemetry>();
}
