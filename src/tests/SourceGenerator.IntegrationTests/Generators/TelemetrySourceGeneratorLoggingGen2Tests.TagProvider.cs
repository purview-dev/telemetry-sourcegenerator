namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingGen2Tests
{
	const string TagProviderLoggerSource = """
		using System.Collections.Generic;
		using Microsoft.Extensions.Logging;

		namespace Testing;

		[Logger]
		public interface ITestLogger
		{
			void LogWeather([TagProvider(typeof(WeatherTagProvider), "GetTags")]WeatherForecast weather);
		}

		public class WeatherForecast
		{
			public string Summary { get; set; }
		}

		public static class WeatherTagProvider
		{
			public static IEnumerable<KeyValuePair<string, object?>> GetTags(WeatherForecast weather) =>
				new[] { new KeyValuePair<string, object?>("Summary", weather.Summary) };
		}
		""";

	[Test]
	public async Task Generate_GivenTagProvider_EmitsProviderCallIntoLogState(CancellationToken cancellationToken)
	{
		// Arrange / Act
		using var generationResult = await GenerateAsync(TagProviderLoggerSource, cancellationToken: cancellationToken);

		// Assert
		var source = generationResult.GetSource("TestLoggerCore.Logging.g.cs");
		await Assert
			.That(source)
			.Contains("global::Testing.WeatherTagProvider.GetTags")
			.Because("the resolved provider method is called with the annotated parameter");
		await Assert
			.That(source)
			.Contains("\"weather.\" + tag.Key")
			.Because("each contributed key is prefixed with the parameter name by default");
		await Assert.That(source).Contains(".AddTag(").Because("the pairs are added to the log state as extra tags");
	}

	[Test]
	public async Task Generate_GivenTagProviderWithOmitReferenceName_EmitsUnprefixedKeys(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			using System.Collections.Generic;
			using Microsoft.Extensions.Logging;

			namespace Testing;

			[Logger]
			public interface ITestLogger
			{
				void LogWeather(
					[TagProvider(typeof(WeatherTagProvider), "GetTags", OmitReferenceName = true)]WeatherForecast weather
				);
			}

			public class WeatherForecast
			{
				public string Summary { get; set; }
			}

			public static class WeatherTagProvider
			{
				public static IEnumerable<KeyValuePair<string, object?>> GetTags(WeatherForecast weather) =>
					new[] { new KeyValuePair<string, object?>("Summary", weather.Summary) };
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		var generated = generationResult.GetSource("TestLoggerCore.Logging.g.cs");
		await Assert
			.That(generated)
			.Contains("state.AddTag(tag.Key, tag.Value)")
			.Because("OmitReferenceName leaves the contributed key untouched");
		await Assert
			.That(generated)
			.DoesNotContain("\"weather.\" + tag.Key")
			.Because("the parameter name is not prefixed onto a provider key");
	}

	[Test]
	public async Task Generate_GivenTagProviderWithStringValues_AcceptsThePairs(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using System.Collections.Generic;
			using Microsoft.Extensions.Logging;

			namespace Testing;

			[Logger]
			public interface ITestLogger
			{
				void LogWeather([TagProvider(typeof(WeatherTagProvider), "GetTags")]WeatherForecast weather);
			}

			public class WeatherForecast
			{
				public string Summary { get; set; }
			}

			public static class WeatherTagProvider
			{
				public static IEnumerable<KeyValuePair<string, string>> GetTags(WeatherForecast weather) =>
					new[] { new KeyValuePair<string, string>("Summary", weather.Summary) };
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		await Assert.That(generationResult).HasNoDiagnostics();
		await Assert
			.That(generationResult.GetSource("TestLoggerCore.Logging.g.cs"))
			.Contains("global::Testing.WeatherTagProvider.GetTags")
			.Because("the string-valued pair shape the .NET generator also accepts is usable");
	}

	[Test]
	public async Task Generate_GivenTagProviderOnV1Method_UpgradesToTheStatePath(CancellationToken cancellationToken)
	{
		// Arrange: LoggerMessage.Define carries no tag bag, so a forced v1 cannot honour a provider.
		const string source = """
			using System.Collections.Generic;
			using Microsoft.Extensions.Logging;

			namespace Testing;

			[Logger(GenerationMode = LoggerGenerationMode.V1)]
			public interface ITestLogger
			{
				void LogWeather([TagProvider(typeof(WeatherTagProvider), "GetTags")]WeatherForecast weather);
			}

			public class WeatherForecast
			{
				public string Summary { get; set; }
			}

			public static class WeatherTagProvider
			{
				public static IEnumerable<KeyValuePair<string, object?>> GetTags(WeatherForecast weather) =>
					new[] { new KeyValuePair<string, object?>("Summary", weather.Summary) };
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		var generated = generationResult.GetSource("TestLoggerCore.Logging.g.cs");
		await Assert
			.That(generated)
			.Contains("global::Testing.WeatherTagProvider.GetTags")
			.Because("the provider is honoured even when v1 was requested");
		await Assert
			.That(generated)
			.DoesNotContain("LoggerMessage.Define")
			.Because("v1 cannot carry the provider's tags, so the state path is used instead");
	}

	[Test]
	public async Task Generate_GivenTagProviderWithUnknownMethod_RaisesTSG2025(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using System.Collections.Generic;
			using Microsoft.Extensions.Logging;

			namespace Testing;

			[Logger]
			public interface ITestLogger
			{
				void LogWeather([TagProvider(typeof(WeatherTagProvider), "DoesNotExist")]WeatherForecast weather);
			}

			public class WeatherForecast
			{
				public string Summary { get; set; }
			}

			public static class WeatherTagProvider
			{
				public static IEnumerable<KeyValuePair<string, object?>> GetTags(WeatherForecast weather) =>
					new[] { new KeyValuePair<string, object?>("Summary", weather.Summary) };
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG2025");
	}

	[Test]
	public async Task Generate_GivenTagProviderWithWrongReturnType_RaisesTSG2026(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			[Logger]
			public interface ITestLogger
			{
				void LogWeather([TagProvider(typeof(WeatherTagProvider), "GetTags")]WeatherForecast weather);
			}

			public class WeatherForecast
			{
				public string Summary { get; set; }
			}

			public static class WeatherTagProvider
			{
				public static void GetTags(WeatherForecast weather) { }
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG2026");
	}

	[Test]
	public async Task Generate_GivenTagProviderOnActivityMethod_RaisesTSG2027(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using System.Collections.Generic;
			using System.Diagnostics;
			using Microsoft.Extensions.Logging;

			namespace Testing;

			[ActivitySource("testing-activity-source")]
			public interface ITestActivities
			{
				[Activity]
				Activity? Start([TagProvider(typeof(WeatherTagProvider), "GetTags")]WeatherForecast weather);
			}

			public class WeatherForecast
			{
				public string Summary { get; set; }
			}

			public static class WeatherTagProvider
			{
				public static IEnumerable<KeyValuePair<string, object?>> GetTags(WeatherForecast weather) =>
					new[] { new KeyValuePair<string, object?>("Summary", weather.Summary) };
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG2027");
	}
}
