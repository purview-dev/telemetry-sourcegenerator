namespace SampleApp.Web.Clients;

/// <summary>
/// Weather forecast data from the API.
/// </summary>
public readonly record struct WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
	public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
