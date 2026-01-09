using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SampleApp.APIService.Services;

namespace SampleApp.APIService.Endpoints;

static class WeatherAPI
{
	const int DefaultRequestCount = 5;

	public static IEndpointRouteBuilder MapWeatherAPIv1(this IEndpointRouteBuilder app)
	{
		var api = app.MapGroup("/weatherforecast").WithDisplayName("Weather APIs");

		api.MapGet("/", GetDefaultWeatherRequestAsync)
			.WithDescription($"Gets the weather forecasts, defaults to {DefaultRequestCount}.")
			.WithDisplayName($"{DefaultRequestCount} Weather Forecasts");

		api.MapGet("/{requestCount:int}", GetWeatherRequestAsync)
			.WithDescription("Gets the weather forecasts.")
			.WithDisplayName("Weather Forecasts");

		return api;
	}

	static async Task<
		Results<Ok<WeatherForecast[]>, NoContent, ProblemHttpResult>
	> GetDefaultWeatherRequestAsync(
		[FromServices] IWeatherService weatherService,
		CancellationToken cancellationToken
	)
	{
		try
		{
			var results = await weatherService.GetWeatherForecastsAsync(
				DefaultRequestCount,
				cancellationToken
			);

			return ConvertResults(results);
		}
		catch (Exception ex)
		{
			return TypedResults.Problem(detail: ex.Message, statusCode: 502);
		}
	}

	static async Task<
		Results<Ok<WeatherForecast[]>, NoContent, ProblemHttpResult>
	> GetWeatherRequestAsync(
		int requestCount,
		[FromServices] IWeatherService weatherService,
		CancellationToken cancellationToken
	)
	{
		try
		{
			var results = await weatherService.GetWeatherForecastsAsync(
				requestCount,
				cancellationToken
			);

			return ConvertResults(results);
		}
		catch (Exception ex)
		{
			return TypedResults.Problem(detail: ex.Message, statusCode: 502);
		}
	}

	static Results<Ok<WeatherForecast[]>, NoContent, ProblemHttpResult> ConvertResults(
		ErrorOr<IEnumerable<WeatherForecast>> results
	)
	{
		if (results.IsError)
			return TypedResults.Problem(results.FirstError.ToProblemDetails());

		return results.Value?.Any() == true
			? TypedResults.Ok(results.Value.ToArray())
			: TypedResults.NoContent();
	}
}
