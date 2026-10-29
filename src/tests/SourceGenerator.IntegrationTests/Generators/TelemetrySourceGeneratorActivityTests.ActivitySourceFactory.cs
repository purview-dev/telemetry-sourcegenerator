using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator.Activities;

/// <summary>
/// .NET 11's <c>System.Diagnostics.ActivitySourceFactory</c>, opted into with
/// <c>[assembly: ActivitySourceGeneration(..., UseActivitySourceFactory = true)]</c>. The factory path only runs on the
/// <c>net11.0</c> leg, where the test compilation can resolve the type; the fallback only on the earlier legs.
/// </summary>
partial class TelemetrySourceGeneratorActivityTests
{
	const string FactoryOptIn = """
		[assembly: ActivitySourceGeneration("testing-activity-source", UseActivitySourceFactory = true)]
		""";

	/// <summary>The initialisation method every factory-created source gets, as a code snapshot.</summary>
	const string InitializeActivitySourceSnapshot = """
		private void InitializeActivitySource(global::System.Diagnostics.ActivitySourceFactory activitySourceFactory)
		{
			if (activitySourceFactory == null)
			{
				throw new global::System.ArgumentNullException(nameof(activitySourceFactory));
			}

			_activitySource = activitySourceFactory.Create(new global::System.Diagnostics.ActivitySourceOptions("testing-activity-source"));
		}
		""";

	[Test]
	[RequiresActivitySourceFactory]
	public async Task Generate_GivenUseActivitySourceFactoryOnAnActivityOnlyInterface_CreatesTheSourceFromTheFactory(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = $$"""
			using System.Diagnostics;

			{{FactoryOptIn}}

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
			.ContainsGeneratedCode("private global::System.Diagnostics.ActivitySource _activitySource = default!;")
			.Because("a factory-created source is per instance, not static");
		await Assert
			.That(generated)
			.ContainsGeneratedCode(
				"""
				public TestActivitiesCore(global::System.Diagnostics.ActivitySourceFactory activitySourceFactory)
				{
					InitializeActivitySource(activitySourceFactory);
				}
				"""
			)
			.Because("an Activities-only interface has no other target to own the constructor");
		await Assert.That(generated).ContainsGeneratedCode(InitializeActivitySourceSnapshot);
		await Assert.That(generated).DoesNotContain("static readonly global::System.Diagnostics.ActivitySource");
		await Assert.That(generationResult).DoesNotHaveDiagnostic("TSG3023");
	}

	[Test]
	[RequiresActivitySourceFactory]
	public async Task Generate_GivenUseActivitySourceFactoryWithLogging_TheLoggerConstructorTakesTheFactory(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = $$"""
			using System.Diagnostics;

			{{FactoryOptIn}}

			namespace Testing;

			[ActivitySource]
			[Logger]
			public interface ITestTelemetry
			{
				[Activity]
				[Info]
				Activity? Work([Tag] int id);
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		await Assert
			.That(generationResult.GetSource("TestTelemetryCore.Logging.g.cs"))
			.ContainsGeneratedCode(
				"""
				public TestTelemetryCore(global::Microsoft.Extensions.Logging.ILogger<global::Testing.ITestTelemetry> logger, global::System.Diagnostics.ActivitySourceFactory activitySourceFactory)
				{
					_logger = logger;
					InitializeActivitySource(activitySourceFactory);
				}
				"""
			)
			.Because("Logging owns the constructor of a multi-target interface, so it receives the factory");
		await Assert
			.That(generationResult.GetSource("TestTelemetryCore.Activity.g.cs"))
			.ContainsGeneratedCode(InitializeActivitySourceSnapshot);
	}

	[Test]
	[RequiresActivitySourceFactory]
	public async Task Generate_GivenUseActivitySourceFactoryWithMetrics_TheMetricsConstructorTakesBothFactories(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = $$"""
			using System.Diagnostics;

			{{FactoryOptIn}}

			namespace Testing;

			[ActivitySource]
			[Meter("testing-meter")]
			public interface ITestTelemetry
			{
				[Activity]
				[AutoCounter]
				Activity? Work([Tag] int id);
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		var metrics = generationResult.GetSource("TestTelemetryCore.Metric.g.cs");
		await Assert
			.That(metrics)
			.ContainsGeneratedCode(
				"public TestTelemetryCore(global::System.Diagnostics.Metrics.IMeterFactory meterFactory, global::System.Diagnostics.ActivitySourceFactory activitySourceFactory)"
			)
			.Because("Metrics owns the constructor when there is no Logging target");
		await Assert.That(metrics).ContainsGeneratedCode("InitializeActivitySource(activitySourceFactory);");
	}

	[Test]
	[RequiresActivitySourceFactory]
	public async Task Generate_GivenTheFactoryIsAvailableButNotRequested_KeepsTheStaticSource(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			using System.Diagnostics;

			[assembly: ActivitySourceGeneration("testing-activity-source")]

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
				"private static readonly global::System.Diagnostics.ActivitySource _activitySource = new global::System.Diagnostics.ActivitySource(\"testing-activity-source\");"
			)
			.Because(
				"the factory is opt-in: an existing consumer's constructor must not change when it moves to .NET 11"
			);
		await Assert.That(generated).DoesNotContain("ActivitySourceFactory");
	}

	[Test]
	[RequiresNoActivitySourceFactory]
	public async Task Generate_GivenUseActivitySourceFactoryWithoutTheType_FallsBackToAStaticSourceAndReportsTSG3023(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = $$"""
			using System.Diagnostics;

			{{FactoryOptIn}}

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
				"private static readonly global::System.Diagnostics.ActivitySource _activitySource = new global::System.Diagnostics.ActivitySource(\"testing-activity-source\");"
			);
		await Assert.That(generated).DoesNotContain("ActivitySourceFactory");
		await Assert.That(generationResult).HasDiagnostic("TSG3023");
	}
}
