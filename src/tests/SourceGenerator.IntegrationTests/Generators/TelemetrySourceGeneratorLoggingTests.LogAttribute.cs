using Purview.SourceGeneratorFramework;

namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingTests
{
	[Test]
	[MethodDataSource(nameof(GetEntryNames))]
	public async Task Generate_GivenLogTargetWithEntryName_GenerateLogger(
		string logTargetName,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var basicLogger = $$"""


namespace Testing;

[Logger]
public interface ITestLogger {
	[Log(Name = "{{logTargetName}}")]
	void Log(string stringParam, int intParam, bool boolParam);
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
			.That(generationResult.GetSource("TestLoggerCore.Logging.g.cs"))
			.ContainsGeneratedCode(logTargetName)
			.Because("the generated log entry must carry the configured name");
	}

	[Test]
	[MethodDataSource(nameof(GetPrefixAndEntryNames))]
	public async Task Generate_GivenLogTargetWithPrefixAndEntryName_GenerateLogger(
		string type,
		string logTargetName,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var prefixType = type switch
		{
			"Custom" => type + ", CustomPrefix = \"custom-prefix\"",
			_ => type,
		};

		var basicLogger = $$"""


namespace Testing;

[Logger(PrefixType = LogPrefixType.{{prefixType}})]
public interface ITestLogger {
	[Log(Name = "{{logTargetName}}")]
	void Log(string stringParam, int intParam, bool boolParam);
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
			.That(generationResult.GetSource("TestLoggerCore.Logging.g.cs"))
			.ContainsGeneratedCode(logTargetName)
			.Because("the generated log entry must carry the configured name");
	}

	public static IEnumerable<(string, string)> GetPrefixAndEntryNames()
	{
		List<(string, string)> data = [];

		string[] prefixes = ["Default", "Custom", "Interface", "Class", "TrimmedClassName"];

		foreach (var type in prefixes)
		{
			foreach (var entryName in TestEntryNames)
			{
				data.Add((type, entryName));
			}
		}

		return data;
	}

	public static IEnumerable<string> GetEntryNames()
	{
		List<string> data = [];

		data.AddRange(TestEntryNames);

		return data;
	}

	static readonly string[] TestEntryNames =
	[
		"LogNameSetViaLogTargetAttribute",
		"CustomLogNameSetViaLogTargetAttribute",
		"123",
		"custom-log-entry-name",
	];
}
