using Microsoft.CodeAnalysis.CSharp;

namespace Purview.Telemetry.SourceGenerator.Activities;

partial class TelemetrySourceGeneratorActivityTests
{
	[Test]
	public async Task Generate_GivenBasicGen_GeneratesActivity_WithCSharp73LanguageVersion(
		CancellationToken cancellationToken
	)
	{
		// Arrange: C# 7.3-compatible interface — no nullable reference annotations,
		// no file-scoped namespace.
		const string basicActivity =
			@"

namespace Testing {

[ActivitySource(""testing-activity-source"")]
public interface ITestActivities {
	[Activity]
	System.Diagnostics.Activity Activity([Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Event]
	void Event(System.Diagnostics.Activity activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);
}

}
";

		// Act
		var generationResult = await GenerateAsync(
			basicActivity,
			languageVersion: LanguageVersion.CSharp7_3,
			cancellationToken: cancellationToken
		);

		// Assert: validationCompilation=true (default) verifies generated code compiles under C# 7.3.
		// TSG3022 (non-nullable Activity return type) is a warning — suppress non-errors since C# 7.3
		// users cannot use 'Activity?' anyway.
		await TestHelpers.VerifyAsync(
			generationResult,
			whenValidatingDiagnosticsIgnoreNonErrors: true,
			cancellationToken: cancellationToken
		);
	}
}
