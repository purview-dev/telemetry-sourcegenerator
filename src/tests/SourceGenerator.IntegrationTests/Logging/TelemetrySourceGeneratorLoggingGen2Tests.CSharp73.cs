using Microsoft.CodeAnalysis.CSharp;

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
		var generationResult = await GenerateAsync(
			source,
			languageVersion: LanguageVersion.CSharp7_3,
			cancellationToken: cancellationToken
		);

		// Assert: validationCompilation=true (default) verifies generated code compiles under C# 7.3.
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}
}
