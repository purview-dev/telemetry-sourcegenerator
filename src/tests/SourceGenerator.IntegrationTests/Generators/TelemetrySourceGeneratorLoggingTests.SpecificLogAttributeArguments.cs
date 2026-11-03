using System.Globalization;
using Purview.Telemetry.SourceGenerator.Helpers;

namespace Purview.Telemetry.SourceGenerator.Logging;

/// <summary>
/// The level-specific attributes ([Trace], [Debug], [Info], [Warning], [Error], [Critical]) share the
/// <c>LogAttributeData</c> model with [Log]. <c>LogAttributeDataModelTests</c> proves the models read every
/// argument; these prove the V1 and V2 emitters write what was read, not just that the method exists.
/// </summary>
partial class TelemetrySourceGeneratorLoggingTests
{
	[Test]
	[MethodDataSource(nameof(SpecificLogAttributeTypesAndGenerationModes))]
	public async Task Generate_GivenSpecificLogAttributeWithPositionalTemplate_EmitsTemplate(
		string attribute,
		string generationMode,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var basicLogger = $$"""

namespace Testing;

[Logger(GenerationMode = LoggerGenerationMode.{{generationMode}})]
public interface ITestLogger
{
	[{{attribute}}("Custom {Value} message")]
	void Log(int value);
}

""";

		// Act
		using var generationResult = await GenerateAsync(basicLogger, cancellationToken: cancellationToken);

		// Assert
		var loggerClass = GetLoggerClassSource(generationResult);
		await Assert.That(loggerClass).Contains("\"Custom {Value} message\"");
		await Assert.That(loggerClass).DoesNotContain("Log: Value = {Value}");
		await Assert
			.That(loggerClass)
			.Contains(SharedHelpers.GetNonRandomizedHashCode("Log").ToString(CultureInfo.InvariantCulture))
			.Because("an omitted event id falls back to the hashed method name, not 0");
	}

	[Test]
	[MethodDataSource(nameof(SpecificLogAttributeTypesAndGenerationModes))]
	public async Task Generate_GivenSpecificLogAttributeWithEventIdConstructor_EmitsEventIdTemplateAndName(
		string attribute,
		string generationMode,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var basicLogger = $$"""

namespace Testing;

[Logger(GenerationMode = LoggerGenerationMode.{{generationMode}})]
public interface ITestLogger
{
	[{{attribute}}(77, "EventId {Value} message", "RenamedEntry")]
	void Log(int value);
}

""";

		// Act
		using var generationResult = await GenerateAsync(basicLogger, cancellationToken: cancellationToken);

		// Assert
		var loggerClass = GetLoggerClassSource(generationResult);
		await Assert.That(loggerClass).Contains("\"EventId {Value} message\"");
		await Assert.That(loggerClass).Contains("(77, ");
		await Assert.That(loggerClass).Contains("RenamedEntry");
	}

	[Test]
	[MethodDataSource(nameof(SpecificLogAttributeTypes))]
	public async Task Generate_GivenSpecificLogAttributeForcingV1OnV2Interface_EmitsLoggerMessageDefine(
		string attribute,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var basicLogger = $$"""

namespace Testing;

[Logger(GenerationMode = LoggerGenerationMode.V2)]
public interface ITestLogger
{
	[{{attribute}}(GenerationMode = LoggerGenerationMode.V1)]
	void Log(int value);
}

""";

		// Act
		using var generationResult = await GenerateAsync(basicLogger, cancellationToken: cancellationToken);

		// Assert
		var loggerClass = GetLoggerClassSource(generationResult);
		await Assert
			.That(loggerClass)
			.Contains("LoggerMessage.Define")
			.Because("GenerationMode on the attribute overrides the interface's V2 mode");
	}

	[Test]
	[MethodDataSource(nameof(SpecificLogAttributeTypes))]
	public async Task Generate_GivenV2SpecificLogAttributeWithInterfacePrefix_EmitsPrefixedEventName(
		string attribute,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var basicLogger = $$"""

namespace Testing;

[Logger(GenerationMode = LoggerGenerationMode.V2, PrefixType = LogPrefixType.Interface)]
public interface ITestLogger
{
	[{{attribute}}]
	void Log(int value);
}

""";

		// Act
		using var generationResult = await GenerateAsync(basicLogger, cancellationToken: cancellationToken);

		// Assert
		var loggerClass = GetLoggerClassSource(generationResult);
		await Assert
			.That(loggerClass)
			.Contains("\"ITestLogger.Log\"")
			.Because("V2 must name the event like V1 does, not collapse the prefix through nameof");
	}

	static string GetLoggerClassSource(DriverRunResult generationResult) =>
		generationResult.Generated().GetClass("TestLoggerCore", "Testing").Node.ToFullString();

	public static IEnumerable<(string, string)> SpecificLogAttributeTypesAndGenerationModes
	{
		get
		{
			List<(string, string)> data = [];

			foreach (var attribute in SpecificLogAttributeTypes)
			{
				data.Add((attribute, "V1"));
				data.Add((attribute, "V2"));
			}

			return data;
		}
	}
}
