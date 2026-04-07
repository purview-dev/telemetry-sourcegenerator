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
		var generationResult = await GenerateAsync(
			basicLogger,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			cancellationToken: cancellationToken,
			parameters: logTargetName
		);
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
		var generationResult = await GenerateAsync(
			basicLogger,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			cancellationToken: cancellationToken,
			parameters: [prefixType, logTargetName]
		);
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
