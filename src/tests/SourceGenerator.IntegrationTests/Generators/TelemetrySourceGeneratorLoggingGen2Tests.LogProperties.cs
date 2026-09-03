using Purview.SourceGeneratorFramework;
using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingGen2Tests
{
	[Test]
	public async Task Generate_GivenMethodWithLogProperty_GeneratesIndividualProperties(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLogger =
			@"
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
		var generationResult = await GenerateAsync(basicLogger, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var loggerClass = query.GetClass("TestLoggerCore", "Testing");
		await Assert
			.That(
				loggerClass.HasMethod(
					query,
					"LogWeather",
					new TypeReference(new TypeIdentity("WeatherForecast", "Testing"))
				)
			)
			.IsTrue()
			.Because("the generated logger must contain the log method with the class-typed parameter");
		await Assert
			.That(generationResult.GetSource("TestLoggerCore.Logging.g.cs"))
			.ContainsGeneratedCode("TemperatureC")
			.Because("the object's individual properties must be expanded into the log state");
	}

	[Test]
	public async Task Generate_GivenMethodWithLogPropertyAndExpandEnumerable_GeneratesDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLogger =
			@"
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
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG2006");
	}

	[Test]
	public async Task Generate_GivenMethodWithExceptionUsedInTemplate_UsesPassInException(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLogger = """

using Microsoft.Extensions.Logging;

namespace Testing;

[Logger(GenerationMode = LoggerGenerationMode.V2)]
public interface ITestLogger
{
	[Log(MessageTemplate = "v = {v} Exception = {ex}")]
	void Log(string v, Exception ex);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicLogger, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var loggerClass = query.GetClass("TestLoggerCore", "Testing");
		await Assert
			.That(
				loggerClass.HasMethod(query, "Log", TypeReference.Create<string>(), TypeReference.Create<Exception>())
			)
			.IsTrue()
			.Because("the generated logger must contain the log method with the exception parameter");
		await Assert
			.That(generationResult.GetSource("TestLoggerCore.Logging.g.cs"))
			.ContainsGeneratedCode("Exception = {ex}")
			.Because("the message template must reference the passed-in exception");
	}

	[Test]
	public async Task Generate_GivenMethodWithLogPropertyOmit_GeneratesIndividualProperties(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLogger =
			@"
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
		var generationResult = await GenerateAsync(basicLogger, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var loggerClass = query.GetClass("TestLoggerCore", "Testing");
		await Assert
			.That(
				loggerClass.HasMethod(
					query,
					"LogWeatherWithOmit",
					new TypeReference(new TypeIdentity("WeatherForecast", "Testing"))
				)
			)
			.IsTrue()
			.Because("the generated logger must contain the log method with the class-typed parameter");
		await Assert
			.That(generationResult.GetSource("TestLoggerCore.Logging.g.cs"))
			.ContainsGeneratedCode("TemperatureC")
			.Because("the object's individual properties must be expanded into the log state");
	}

	[Test]
	public async Task Generate_GivenMethodWithLogPropertySkipNull_GeneratesIndividualProperties(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLogger =
			@"
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
		var generationResult = await GenerateAsync(basicLogger, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var loggerClass = query.GetClass("TestLoggerCore", "Testing");
		await Assert
			.That(
				loggerClass.HasMethod(
					query,
					"LogWeather",
					new TypeReference(new TypeIdentity("WeatherForecast", "Testing"))
				)
			)
			.IsTrue()
			.Because("the generated logger must contain the log method with the class-typed parameter");
		await Assert
			.That(generationResult.GetSource("TestLoggerCore.Logging.g.cs"))
			.ContainsGeneratedCode("TemperatureC")
			.Because("the object's individual properties must be expanded into the log state");
	}

	[Test]
	public async Task Generate_GivenMethodWithLogPropertySkipNullAndOmit_GeneratesIndividualProperties(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLogger =
			@"
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
		var generationResult = await GenerateAsync(basicLogger, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var loggerClass = query.GetClass("TestLoggerCore", "Testing");
		await Assert
			.That(
				loggerClass.HasMethod(
					query,
					"LogWeather",
					new TypeReference(new TypeIdentity("WeatherForecast", "Testing"))
				)
			)
			.IsTrue()
			.Because("the generated logger must contain the log method with the class-typed parameter");
		await Assert
			.That(generationResult.GetSource("TestLoggerCore.Logging.g.cs"))
			.ContainsGeneratedCode("TemperatureC")
			.Because("the object's individual properties must be expanded into the log state");
	}

	[Test]
	public async Task Generate_GivenMethodWithLogPropertyIgnore_GeneratesIndividualProperties(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicLogger =
			@"
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
		var generationResult = await GenerateAsync(basicLogger, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var loggerClass = query.GetClass("TestLoggerCore", "Testing");
		await Assert
			.That(
				loggerClass.HasMethod(
					query,
					"LogWeather",
					new TypeReference(new TypeIdentity("WeatherForecast", "Testing"))
				)
			)
			.IsTrue()
			.Because("the generated logger must contain the log method with the class-typed parameter");
		await Assert
			.That(generationResult.GetSource("TestLoggerCore.Logging.g.cs"))
			.ContainsGeneratedCode("TemperatureC")
			.Because("the object's individual properties must be expanded into the log state");
		await Assert
			.That(generationResult.GetSource("TestLoggerCore.Logging.g.cs"))
			.DoesNotContain("IgnoreMe")
			.Because("the ignored property must be excluded from the expanded log properties");
	}
}
