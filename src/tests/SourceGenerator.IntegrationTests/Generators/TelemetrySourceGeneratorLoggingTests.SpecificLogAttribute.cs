using Purview.SourceGeneratorFramework;

namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingTests
{
	[Test]
	[MethodDataSource(nameof(SpecificLogAttributeTypes))]
	public async Task Generate_GivenInterfaceWithSpecificLogAttribute_GenerateLoggerWithThatLevel(
		string attribute,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var basicLogger =
			@$"

namespace Testing;

[Logger]
public interface ITestLogger
{{
	[{attribute}]
	void Log(string stringParam, int intParam, bool boolParam);
}}
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
					"Log",
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated logger must contain the log method with its parameter signature");
	}

	[Test]
	[MethodDataSource(nameof(SpecificLogAttributeTypes))]
	public async Task Generate_GivenInterfaceWithSpecificTypesAndSpecificParameters_GenerateLoggerWithThatLevelAndParameter(
		string attribute,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var basicLogger = $$"""


namespace Testing;

[Logger]
public interface ITestLogger
{
	[{{attribute}}]
	void Log(string stringParam, int intParam, bool boolParam);

	[{{attribute}}(eventId: 100)]
	void Log_EventId_1(string stringParam, int intParam, bool boolParam);

	[{{attribute}}(100)]
	void Log_EventId_3(string stringParam, int intParam, bool boolParam);

	[{{attribute}}(messageTemplate: "template")]
	void Log_MessageTemplate_1(string stringParam, int intParam, bool boolParam);

	[{{attribute}}(MessageTemplate = "template")]
	void Log_MessageTemplate_2(string stringParam, int intParam, bool boolParam);

	[{{attribute}}("template")]
	void Log_MessageTemplate_3(string stringParam, int intParam, bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicLogger, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var loggerClass = query.GetClass("TestLoggerCore", "Testing");
		await Assert
			.That(
				loggerClass.HasMethod(
					query,
					"Log",
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated logger must contain the log method");
		await Assert
			.That(
				loggerClass.HasMethod(
					query,
					"Log_EventId_1",
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated logger must contain the event-id log method");
		await Assert
			.That(
				loggerClass.HasMethod(
					query,
					"Log_EventId_3",
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated logger must contain the positional-event-id log method");
		await Assert
			.That(
				loggerClass.HasMethod(
					query,
					"Log_MessageTemplate_1",
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated logger must contain the first message-template log method");
		await Assert
			.That(
				loggerClass.HasMethod(
					query,
					"Log_MessageTemplate_2",
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated logger must contain the second message-template log method");
		await Assert
			.That(
				loggerClass.HasMethod(
					query,
					"Log_MessageTemplate_3",
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated logger must contain the positional message-template log method");
	}

	public static IEnumerable<string> SpecificLogAttributeTypes
	{
		get
		{
			List<string> data = [];

			data.Add("Trace");
			data.Add("Debug");
			data.Add("Info");
			data.Add("Warning");
			data.Add("Error");
			data.Add("Critical");

			return data;
		}
	}
}
