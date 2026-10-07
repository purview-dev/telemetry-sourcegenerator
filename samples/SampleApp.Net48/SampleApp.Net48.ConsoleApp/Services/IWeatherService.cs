namespace SampleApp.Net48.ConsoleApp.Services
{
	interface IWeatherService
	{
		IReadOnlyList<WeatherForecast> GetWeatherForecasts(int requestCount);
	}

	sealed class WeatherForecast
	{
		public System.DateTime Date { get; set; }
		public int TemperatureC { get; set; }
		public string Summary { get; set; } = string.Empty;
		public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
	}
}
