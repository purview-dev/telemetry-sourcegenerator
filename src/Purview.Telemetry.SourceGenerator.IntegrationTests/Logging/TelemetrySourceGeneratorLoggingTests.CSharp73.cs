using Microsoft.CodeAnalysis.CSharp;

namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingTests
{
	[Test]
	public async Task Generate_GivenBasicGen_GeneratesLogger_WithCSharp73LanguageVersion(
		CancellationToken cancellationToken
	)
	{
		// Arrange: C# 7.3-compatible interface — no nullable reference annotations,
		// no file-scoped namespace.
		const string basicLogger = @"

namespace Testing {

[Logger]
public interface ITestLogger {
	void Log(string stringParam, int intParam, bool boolParam);
}

}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			languageVersion: LanguageVersion.CSharp7_3,
			cancellationToken: cancellationToken
		);

		// Assert: validationCompilation=true (default) verifies generated code compiles under C# 7.3.
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}
}
