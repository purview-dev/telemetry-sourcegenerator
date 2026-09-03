using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator.Activities;

public partial class TelemetrySourceGeneratorActivityTests
	: IncrementalSourceGeneratorTestBase<TelemetrySourceGenerator>
{
	[Test]
	[MethodDataSource<TelemetrySourceGeneratorTests>(nameof(TelemetrySourceGeneratorTests.BasicGenericParameters))]
	public async Task Generate_GivenMethodWithBasicGenericParams_GeneratesEntryCorrectly(
		string parameterType,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var basicActivity =
			@$"

namespace Testing;

[ActivitySource]
public interface ITestActivities
{{
	[Activity]
	System.Diagnostics.Activity? Activity({parameterType} paramName);

	[Event]
	void Event(System.Diagnostics.Activity? activity, {parameterType} paramName);

	[Context]
	void Context(System.Diagnostics.Activity? activity, {parameterType} paramName);
}}
";

		// Act
		var generationResult = await GenerateAsync(basicActivity, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var implClass = query.GetClass("TestActivitiesCore", "Testing");
		await Assert
			.That(implClass.HasMethod(query, "Activity"))
			.IsTrue()
			.Because("the generated implementation must contain the activity method");
		await Assert
			.That(implClass.HasMethod(query, "Event"))
			.IsTrue()
			.Because("the generated implementation must contain the event method");
		await Assert
			.That(implClass.HasMethod(query, "Context"))
			.IsTrue()
			.Because("the generated implementation must contain the context method");
	}

	[Test]
	[MethodDataSource<TelemetrySourceGeneratorTests>(nameof(TelemetrySourceGeneratorTests.GetGenericTypeDefCount))]
	public async Task Generate_GivenInterfaceWithGenerics_RaisesDiagnostics(
		int genericTypeCount,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var genericTypeDef = string.Join(", ", Enumerable.Range(0, genericTypeCount).Select(i => $"T{i}"));
		var basicActivity =
			@$"

namespace Testing;

[ActivitySource]
public interface ITestActivities<{genericTypeDef}>
{{
	[Activity]
	System.Diagnostics.Activity? Activity();
}}
";

		// Act
		var generationResult = await GenerateAsync(
			basicActivity,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG1004");
	}

	[Test]
	[MethodDataSource<TelemetrySourceGeneratorTests>(nameof(TelemetrySourceGeneratorTests.GetGenericTypeDefCount))]
	public async Task Generate_GivenMethodWithGenerics_RaisesDiagnostics(
		int genericTypeCount,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var genericTypeDef = string.Join(", ", Enumerable.Range(0, genericTypeCount).Select(i => $"T{i}"));
		var basicActivity =
			@$"

namespace Testing;

[ActivitySource]
public interface ITestActivities
{{
	[Activity]
	System.Diagnostics.Activity? Activity<{genericTypeDef}>();
}}
";

		// Act
		var generationResult = await GenerateAsync(
			basicActivity,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG1005");
	}
}
