using System.Collections.Generic;

namespace SampleApp.Net48.ConsoleApp.Services
{
	public interface IWeatherService
	{
		IReadOnlyList<WeatherForecast> GetWeatherForecasts(int requestCount);
	}

	public sealed class WeatherForecast
	{
		public System.DateTime Date { get; set; }
		public int TemperatureC { get; set; }
		public string Summary { get; set; }
		public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
	}
}
