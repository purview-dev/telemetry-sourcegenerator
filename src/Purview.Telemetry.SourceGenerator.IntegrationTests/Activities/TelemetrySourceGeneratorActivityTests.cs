namespace Purview.Telemetry.SourceGenerator.Activities;

public partial class TelemetrySourceGeneratorActivityTests
	: IncrementalSourceGeneratorTestBase<TelemetrySourceGenerator>
{
	[Test]
	[MethodDataSource<TelemetrySourceGeneratorTests>(
		nameof(TelemetrySourceGeneratorTests.BasicGenericParameters)
	)]
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
		var generationResult = await GenerateAsync(
			basicActivity,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			cancellationToken: cancellationToken,
			parameters: parameterType
		);
	}

	[Test]
	[MethodDataSource<TelemetrySourceGeneratorTests>(
		nameof(TelemetrySourceGeneratorTests.GetGenericTypeDefCount)
	)]
	public async Task Generate_GivenInterfaceWithGenerics_RaisesDiagnostics(
		int genericTypeCount,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var genericTypeDef = string.Join(
			", ",
			Enumerable.Range(0, genericTypeCount).Select(i => $"T{i}")
		);
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
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			expectsDiagnostics: true,
			cancellationToken: cancellationToken,
			parameters: genericTypeCount
		);
	}

	[Test]
	[MethodDataSource<TelemetrySourceGeneratorTests>(
		nameof(TelemetrySourceGeneratorTests.GetGenericTypeDefCount)
	)]
	public async Task Generate_GivenMethodWithGenerics_RaisesDiagnostics(
		int genericTypeCount,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var genericTypeDef = string.Join(
			", ",
			Enumerable.Range(0, genericTypeCount).Select(i => $"T{i}")
		);
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
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			expectsDiagnostics: true,
			cancellationToken: cancellationToken,
			parameters: genericTypeCount
		);
	}
}
