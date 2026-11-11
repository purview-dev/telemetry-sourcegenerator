using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator.Activities;

/// <summary>
/// <c>Version</c> and <c>TelemetrySchemaUrl</c> on <c>[assembly: ActivitySourceGeneration(...)]</c>, which are
/// passed to <c>System.Diagnostics.ActivitySourceOptions</c> for a factory-created source.
/// </summary>
partial class TelemetrySourceGeneratorActivityTests
{
	[Test]
	[RequiresActivitySourceFactory]
	public async Task Generate_GivenVersionAndTelemetrySchemaUrl_PassesThemToTheFactoryOptions(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			using System.Diagnostics;

			[assembly: ActivitySourceGeneration(
				"testing-activity-source",
				UseActivitySourceFactory = true,
				Version = "1.2.3",
				TelemetrySchemaUrl = "https://purview.dev/schemas/telemetry/1.0")]

			namespace Testing;

			[ActivitySource]
			public interface ITestActivities
			{
				[Activity]
				Activity? Work([Tag] int id);
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		var generated = generationResult.GetSource("TestActivitiesCore.Activity.g.cs");

		await Assert
			.That(generated)
			.ContainsGeneratedCode(
				"""
				_activitySource = activitySourceFactory.Create(new global::System.Diagnostics.ActivitySourceOptions("testing-activity-source")
				{
					Version = "1.2.3",
					TelemetrySchemaUrl = "https://purview.dev/schemas/telemetry/1.0",
				});
				"""
			)
			.Because("both options belong to the factory's ActivitySourceOptions, not to the source name");
		await Assert.That(generationResult).DoesNotHaveDiagnostic("TSG3023");
		await Assert.That(generationResult).DoesNotHaveDiagnostic("TSG3024");
	}

	[Test]
	[RequiresActivitySourceFactory]
	public async Task Generate_GivenVersionOnly_PassesJustTheVersion(CancellationToken cancellationToken)
	{
		// Arrange
		const string source = """
			using System.Diagnostics;

			[assembly: ActivitySourceGeneration("testing-activity-source", UseActivitySourceFactory = true, Version = "2.0.0")]

			namespace Testing;

			[ActivitySource]
			public interface ITestActivities
			{
				[Activity]
				Activity? Work([Tag] int id);
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		var generated = generationResult.GetSource("TestActivitiesCore.Activity.g.cs");

		await Assert.That(generated).ContainsGeneratedCode("Version = \"2.0.0\",");
		await Assert.That(generated).DoesNotContain("TelemetrySchemaUrl =");
	}

	[Test]
	[RequiresActivitySourceFactory]
	public async Task Generate_GivenOptionsWithoutTheFactory_ReportsTSG3024AndKeepsTheStaticSource(
		CancellationToken cancellationToken
	)
	{
		// Arrange: without UseActivitySourceFactory there is no ActivitySourceOptions to pass them to.
		const string source = """
			using System.Diagnostics;

			[assembly: ActivitySourceGeneration(
				"testing-activity-source",
				Version = "1.2.3",
				TelemetrySchemaUrl = "https://purview.dev/schemas/telemetry/1.0")]

			namespace Testing;

			[ActivitySource]
			public interface ITestActivities
			{
				[Activity]
				Activity? Work([Tag] int id);
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		var generated = generationResult.GetSource("TestActivitiesCore.Activity.g.cs");

		await Assert
			.That(generated)
			.Contains("static readonly global::System.Diagnostics.ActivitySource")
			.Because("the static source is kept when the factory is not opted into");
		await Assert.That(generationResult).HasDiagnostic("TSG3024");
	}
}
