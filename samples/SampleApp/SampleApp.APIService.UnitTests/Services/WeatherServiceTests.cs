namespace SampleApp.APIService.Services;

public partial class WeatherServiceTests
{
	static WeatherService CreateService(IWeatherServiceTelemetry? telemetry, bool throwOnRNG = false) =>
		new(
			telemetry: telemetry ?? CreateTelemetry(),
			rng: () => throwOnRNG ? 8 : 1 // 8 is out magic eight-ball number - it throws randomly in simulated use.
		);

	static IWeatherServiceTelemetry CreateTelemetry() => Substitute.For<IWeatherServiceTelemetry>();
}
