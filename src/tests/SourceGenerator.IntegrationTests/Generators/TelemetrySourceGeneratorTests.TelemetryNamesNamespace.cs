using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator;

partial class TelemetrySourceGeneratorTests
{
	[Test]
	public async Task Generate_GivenTelemetryNamesNamespace_GeneratesAllTypesInThatNamespace(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			using Purview.Telemetry;

			[assembly: TelemetryGeneration(GenerateDependencyExtension = true, TelemetryNamesNamespace = "Custom.Telemetry")]

			namespace Testing;

			[ActivitySource("testing-activity-source")]
			public interface ITestActivities {
				[Activity]
				System.Diagnostics.Activity? Activity(string? parentId);
			}

			[Meter("testing-meter")]
			public interface ITestMetrics {
				[Counter]
				void Counter([InstrumentMeasurement]int value);
			}

			[Logger]
			public interface ITestLogger {
				void Log(int value);
			}
			""";

		// Act
		var generationResult = await GenerateAsync(
			source,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);

		await Assert
			.That(generationResult.GetSource("TestActivitiesCore.Activity.g.cs"))
			.ContainsGeneratedCode("namespace Custom.Telemetry");
		await Assert
			.That(generationResult.GetSource("TestMetricsCore.Metric.g.cs"))
			.ContainsGeneratedCode("namespace Custom.Telemetry");
		await Assert
			.That(generationResult.GetSource("TestLoggerCore.Logging.g.cs"))
			.ContainsGeneratedCode("namespace Custom.Telemetry");

		await Assert
			.That(generationResult.GetSource("TestActivitiesCoreDIExtension.DependencyInjection.g.cs"))
			.ContainsGeneratedCode("namespace Custom.Telemetry");

		await Assert
			.That(generationResult.GetSource("TelemetryNames.g.cs"))
			.ContainsGeneratedCode("namespace Custom.Telemetry");
	}

	[Test]
	public async Task Generate_GivenNoTelemetryNamesNamespace_UsesInterfaceNamespaces(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			using Purview.Telemetry;

			[assembly: TelemetryGeneration(GenerateDependencyExtension = true)]

			namespace Testing;

			[Logger]
			public interface ITestLogger {
				void Log(int value);
			}
			""";

		// Act
		var generationResult = await GenerateAsync(
			source,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);

		await Assert
			.That(generationResult.GetSource("TestLoggerCore.Logging.g.cs"))
			.ContainsGeneratedCode("namespace Testing");
	}
}
