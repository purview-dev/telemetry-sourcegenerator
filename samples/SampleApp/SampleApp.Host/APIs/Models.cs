using Microsoft.AspNetCore.Mvc;
using SampleApp.Host.Services;

namespace SampleApp.Host.APIs;

record DefaultWeatherRequest(
	[FromServices] IWeatherService WeatherService,
	CancellationToken Token
);

record WeatherRequest(
	int RequestCount,
	[FromServices] IWeatherService WeatherService,
	CancellationToken Token
);
