using Purview.SourceGeneratorFramework;

namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingGen2Tests
{
	[Test]
	public async Task Generate_GivenBasicGen_GeneratesLogger_WithCSharp73LanguageVersion(
		CancellationToken cancellationToken
	)
	{
		// Arrange: C# 7.3-compatible interface — no nullable reference annotations,
		// no file-scoped namespace.
		// Uses LoggerGenerationMode.V2 to exercise the LoggerGenTargetClassEmitter which
		// emits KeyValuePair<string, object?> structs that must become KeyValuePair<string, object>
		// under C# 7.3.
		const string source =
			@"
namespace Testing {

[Logger(GenerationMode = LoggerGenerationMode.V2)]
public interface ITestLogger {
	void Log(string stringParam, int intParam, bool boolParam);
}

}
";

		// Act
		var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert: GenerateAsync's EnsureValid (default) verifies generated code compiles under C# 7.3.
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
}
