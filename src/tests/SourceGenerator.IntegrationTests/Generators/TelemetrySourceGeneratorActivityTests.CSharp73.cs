using Purview.SourceGeneratorFramework;

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
		var generationResult = await GenerateAsync(basicActivity, cancellationToken: cancellationToken);

		// Assert: validates the generated code compiles. The source uses C# 7.3-style syntax
		// (block namespaces, no nullable annotations). TSG3022 (non-nullable Activity return
		// type) is a warning, so ignore non-errors.
		await Assert.That(generationResult).HasNoErrorDiagnostics();

		var query = generationResult.Generated();
		var implClass = query.GetClass("TestActivitiesCore", "Testing");
		await Assert
			.That(
				implClass.HasMethod(
					query,
					"Activity",
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the activity method");
		await Assert
			.That(
				implClass.HasMethod(
					query,
					"Event",
					TypeReference.Create<System.Diagnostics.Activity>(),
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the event method");
	}
}
