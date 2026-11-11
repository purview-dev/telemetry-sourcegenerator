using System.Globalization;
using System.Text;

namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingGen2Tests
{
	const string TransitiveLoggerSource = """
		#pragma warning disable EXTEXP0003
		using Microsoft.Extensions.Logging;

		namespace Testing;

		[Logger]
		public interface ITestLogger
		{
			void LogWeather([LogProperties(Transitive = true)]WeatherForecast weather);
		}

		public class WeatherForecast
		{
			public int TemperatureC { get; set; }
			public Location Location { get; set; }
			public Coordinates Coordinates { get; set; }
		}

		public class Location
		{
			public string City { get; set; }
			public string Country { get; set; }
		}

		public struct Coordinates
		{
			public double Lat { get; set; }
			public double Lng { get; set; }
		}
		""";

	[Test]
	public async Task Generate_GivenTransitiveLogProperty_FlattensNestedObject(CancellationToken cancellationToken)
	{
		// Arrange / Act
		using var generationResult = await GenerateAsync(TransitiveLoggerSource, cancellationToken: cancellationToken);

		// Assert
		var source = generationResult.GetSource("TestLoggerCore.Logging.g.cs");
		await Assert
			.That(source)
			.Contains("weather?.Location?.City")
			.Because("a nested reference-typed property is reached through a second null-conditional access");
		await Assert
			.That(source)
			.Contains("\"weather.Location.City\"")
			.Because("a transitive tag is named after the whole dotted path beneath the parameter");
		await Assert
			.That(source)
			.Contains("\"weather.TemperatureC\"")
			.Because("the parameter's own properties are still flattened once");
	}

	[Test]
	public async Task Generate_GivenTransitiveLogPropertyWithStructProperty_DoesNotEmitConditionalAccess(
		CancellationToken cancellationToken
	)
	{
		// Arrange / Act
		using var generationResult = await GenerateAsync(TransitiveLoggerSource, cancellationToken: cancellationToken);

		// Assert: `weather?.Coordinates?.Lat` does not compile - a non-nullable struct cannot take a `?`.
		var source = generationResult.GetSource("TestLoggerCore.Logging.g.cs");
		await Assert
			.That(source)
			.Contains("weather?.Coordinates.Lat")
			.Because("the conditional access belongs to the hop into the struct, not out of it");
		await Assert
			.That(source)
			.DoesNotContain("weather?.Coordinates?.Lat")
			.Because("a null-conditional access cannot be applied to a non-nullable value type");
	}

	[Test]
	public async Task Generate_GivenTransitiveLogProperty_AppliesSkipNullPropertiesAtEveryLevel(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			#pragma warning disable EXTEXP0003
			using Microsoft.Extensions.Logging;

			namespace Testing;

			[Logger]
			public interface ITestLogger
			{
				void LogWeather(
					[LogProperties(Transitive = true, SkipNullProperties = true)]WeatherForecast weather
				);
			}

			public class WeatherForecast
			{
				public Location Location { get; set; }
			}

			public class Location
			{
				public string City { get; set; }
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert: the nested nullable leaf is guarded, not written unconditionally.
		var generated = generationResult.GetSource("TestLoggerCore.Logging.g.cs");
		await Assert
			.That(generated)
			.Contains("weather?.Location?.City")
			.Because("the nested property is still read through the chain");
		await Assert
			.That(generated)
			.Contains("!= null")
			.Because("the nested nullable value is guarded by SkipNullProperties");
	}

	[Test]
	public async Task Generate_GivenTransitiveLogPropertyWithIgnoredNestedProperty_OmitsIt(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			#pragma warning disable EXTEXP0003
			using Microsoft.Extensions.Logging;

			namespace Testing;

			[Logger]
			public interface ITestLogger
			{
				void LogWeather([LogProperties(Transitive = true)]WeatherForecast weather);
			}

			public class WeatherForecast
			{
				public Location Location { get; set; }
			}

			public class Location
			{
				public string City { get; set; }

				[LogPropertyIgnore]
				public string Secret { get; set; }
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		var generated = generationResult.GetSource("TestLoggerCore.Logging.g.cs");
		await Assert
			.That(generated)
			.Contains("\"weather.Location.City\"")
			.Because("a nested property without the ignore attribute is flattened");
		await Assert
			.That(generated)
			.DoesNotContain("Secret")
			.Because("LogPropertyIgnore applies at every level, not only to the parameter's own properties");
	}

	[Test]
	public async Task Generate_GivenSelfReferencingLogPropertyType_StopsAtTheCycle(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			#pragma warning disable EXTEXP0003
			using Microsoft.Extensions.Logging;

			namespace Testing;

			[Logger]
			public interface ITestLogger
			{
				void LogNode([LogProperties(Transitive = true)]Node node);
			}

			public class Node
			{
				public string Name { get; set; }
				public Node Next { get; set; }
			}
			""";

		// Act: a cycle that recursed would never terminate.
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		var generated = generationResult.GetSource("TestLoggerCore.Logging.g.cs");
		await Assert.That(generated).Contains("\"node.Name\"").Because("the first level is flattened");
		await Assert.That(generated).Contains("\"node.Next\"").Because("the self-reference is logged as one tag");
		await Assert
			.That(generated)
			.DoesNotContain("\"node.Next.Next\"")
			.Because("the repeated type is not walked again");
	}

	[Test]
	public async Task Generate_GivenDeeplyNestedLogPropertyType_StopsAtTheDepthLimit(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const int depth = 40;
		StringBuilder builder = new();
		builder.AppendLine("#pragma warning disable EXTEXP0003");
		builder.AppendLine("using Microsoft.Extensions.Logging;");
		builder.AppendLine("namespace Testing;");
		builder.AppendLine("[Logger]");
		builder.AppendLine("public interface ITestLogger");
		builder.AppendLine("{");
		builder.AppendLine("	void LogDeep([LogProperties(Transitive = true)]Level0 value);");
		builder.AppendLine("}");

		for (var i = 0; i < depth; i++)
		{
			builder.AppendLine(
				string.Format(
					CultureInfo.InvariantCulture,
					"public class Level{0} {{ public Level{1} Next {{ get; set; }} }}",
					i,
					i + 1
				)
			);
		}

		builder.AppendLine(
			string.Format(
				CultureInfo.InvariantCulture,
				"public class Level{0} {{ public int Terminal {{ get; set; }} }}",
				depth
			)
		);

		// Act: 40 levels is deeper than MaxTransitiveDepth, which must bound the walk rather than overflow.
		using var generationResult = await GenerateAsync(builder.ToString(), cancellationToken: cancellationToken);

		// Assert
		var generated = generationResult.GetSource("TestLoggerCore.Logging.g.cs");
		await Assert.That(generated).Contains("\"value.Next.Next\"").Because("the walk continues past the first level");
		await Assert
			.That(generated)
			.DoesNotContain("Terminal")
			.Because("the chain stops at the depth limit instead of reaching the bottom of the graph");
	}
}
