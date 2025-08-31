namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingGen2Tests
{
	[Fact]
	public async Task Generate_GivenMethodWithLogProperty_GeneratesIndividualProperties()
	{
		// Arrange
		const string basicLogger =
			@"
using Purview.Telemetry.Logging;
using Microsoft.Extensions.Logging;

namespace Testing;

[Logger]
public interface ITestLogger
{
	void LogWeather([LogProperties]WeatherForecast weather);
}

public class WeatherForecast
{
	public DateTime Date { get; set; }
	public int TemperatureC { get; set; }
	public string Summary { get; set; }
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			includeLoggerTypes: IncludeLoggerTypes.Telemetry
		);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Fact]
	public async Task Generate_GivenMethodWithLogPropertyAndExpandEnumerable_GeneratesDiagnostic()
	{
		// Arrange
		const string basicLogger =
			@"
using Purview.Telemetry.Logging;
using Microsoft.Extensions.Logging;

namespace Testing;

[Logger]
public interface ITestLogger
{
	void LogWeather([LogProperties][ExpandEnumerable]WeatherForecast[] weather);
}

public class WeatherForecast
{
	public DateTime Date { get; set; }
	public int TemperatureC { get; set; }
	public string Summary { get; set; }
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			includeLoggerTypes: IncludeLoggerTypes.Telemetry
		);

		// Assert
		await TestHelpers.Verify(generationResult, expectsDiagnostics: true);
	}

	[Fact]
	public async Task Generate_GivenMethodWithExceptionUsedInTemplate_UsesPassInException()
	{
		// Arrange
		const string basicLogger =
			@"
using Purview.Telemetry.Logging;
using Microsoft.Extensions.Logging;

namespace Testing;

[Logger]
public interface ITestLogger
{
	[Log(MessageTemplate = ""v = {v} Exception = {ex}"")]
	void Log(string v, Exception ex);
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			includeLoggerTypes: IncludeLoggerTypes.Telemetry
		);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Fact]
	public async Task Generate_GivenMethodWithLogPropertyOmit_GeneratesIndividualProperties()
	{
		// Arrange
		const string basicLogger =
			@"
using Purview.Telemetry.Logging;
using Microsoft.Extensions.Logging;

namespace Testing;

[Logger]
public interface ITestLogger
{
	void LogWeatherWithOmit([LogProperties(OmitReferenceName = true)]WeatherForecast weather);
}

public class WeatherForecast
{
	public DateTime Date { get; set; }
	public int TemperatureC { get; set; }
	public string Summary { get; set; }
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			includeLoggerTypes: IncludeLoggerTypes.Telemetry
		);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Fact]
	public async Task Generate_GivenMethodWithLogPropertySkipNull_GeneratesIndividualProperties()
	{
		// Arrange
		const string basicLogger =
			@"
using Purview.Telemetry.Logging;
using Microsoft.Extensions.Logging;

namespace Testing;

[Logger]
public interface ITestLogger
{
	void LogWeather([LogProperties(SkipNullProperties = true)]WeatherForecast weather);
}

public class WeatherForecast
{
	public DateTime Date { get; set; }
	public int TemperatureC { get; set; }
	public string Summary { get; set; }
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			includeLoggerTypes: IncludeLoggerTypes.Telemetry
		);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Fact]
	public async Task Generate_GivenMethodWithLogPropertySkipNullAndOmit_GeneratesIndividualProperties()
	{
		// Arrange
		const string basicLogger =
			@"
using Purview.Telemetry.Logging;
using Microsoft.Extensions.Logging;

namespace Testing;

[Logger]
public interface ITestLogger
{
	void LogWeather([LogProperties(SkipNullProperties = true, OmitReferenceName = true)]WeatherForecast weather);
}

public class WeatherForecast
{
	public DateTime Date { get; set; }
	public int TemperatureC { get; set; }
	public string Summary { get; set; }
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			includeLoggerTypes: IncludeLoggerTypes.Telemetry
		);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Fact]
	public async Task Generate_GivenMethodWithLogPropertyIgnore_GeneratesIndividualProperties()
	{
		// Arrange
		const string basicLogger =
			@"
using Purview.Telemetry.Logging;
using Microsoft.Extensions.Logging;

namespace Testing;

[Logger]
public interface ITestLogger
{
	void LogWeather([LogProperties]WeatherForecast weather);
}

public class WeatherForecast
{
	public DateTime Date { get; set; }
	public int TemperatureC { get; set; }
	public string Summary { get; set; }

	[LogPropertyIgnore]
	public string IgnoreMe { get; set; }
}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			includeLoggerTypes: IncludeLoggerTypes.Telemetry
		);

		// Assert
		await TestHelpers.Verify(generationResult);
	}
}
