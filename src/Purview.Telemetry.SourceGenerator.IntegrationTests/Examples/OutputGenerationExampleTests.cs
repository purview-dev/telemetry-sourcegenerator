using Purview.Telemetry.SourceGenerator.Configuration;

namespace Purview.Telemetry.SourceGenerator.Examples;

/// <summary>
/// Example test class demonstrating the output generation functionality.
/// Run with PURVIEW_TELEMETRY_OUTPUT_GENERATED_FILES=true to see generated files.
/// </summary>
public class OutputGenerationExampleTests(ITestOutputHelper testOutputHelper)
	: IncrementalSourceGeneratorTestBase<TelemetrySourceGenerator>(testOutputHelper)
{
	[Fact]
	public async Task Example_BasicActivityGeneration_ShowsOutputStructure()
	{
		const string inputSource = """
			using System.Diagnostics;
			using Purview.Telemetry.Activities;

			namespace Example;

			[ActivitySource("example-service")]
			public partial interface IExampleService
			{
				[Activity]
				Activity? StartOperation([Tag] string operationId, [Baggage] string context);

				[Event]
				void LogEvent(Activity? activity, [Tag] string eventType, [Tag] int eventCode);
			}
			""";

		testOutputHelper.WriteLine("=== EXAMPLE: Basic Activity Generation ===");
		testOutputHelper.WriteLine(
			"This example demonstrates how to inspect generated Activity code."
		);

		if (TestOutputConfiguration.IsOutputEnabled)
		{
			testOutputHelper.WriteLine($"? Output generation is ENABLED");
			testOutputHelper.WriteLine(
				$"  Output directory: {TestOutputConfiguration.OutputDirectory}"
			);
			testOutputHelper.WriteLine(
				$"  Look for: Example_BasicActivityGeneration_ShowsOutputStructure/"
			);
		}
		else
		{
			testOutputHelper.WriteLine("? Output generation is DISABLED");
			testOutputHelper.WriteLine(
				"  Set PURVIEW_TELEMETRY_OUTPUT_GENERATED_FILES=true to enable"
			);
		}

		var result = await GenerateAsync(inputSource);

		if (TestOutputConfiguration.IsOutputEnabled)
		{
			var generatedCount = result
				.DriverResult.Results.SelectMany(r => r.GeneratedSources)
				.Count();
			testOutputHelper.WriteLine($"? Generated {generatedCount} source files");

			testOutputHelper.WriteLine("Generated files:");
			foreach (var generatorResult in result.DriverResult.Results)
			{
				foreach (var generatedFile in generatorResult.GeneratedSources)
				{
					testOutputHelper.WriteLine($"  · {generatedFile.HintName}");
				}
			}
		}

		await TestHelpers.Verify(result);
	}

	[Fact]
	public async Task Example_LoggingGeneration_ShowsOutputStructure()
	{
		const string inputSource = """
			using Microsoft.Extensions.Logging;
			using Purview.Telemetry.Logging;

			namespace Example;

			[Logger]
			public partial interface IExampleLogger
			{
				[Log]
				void LogInformation([Tag] string message, [Tag] int code);

				[Warning]
				void LogWarning(string warning, Exception? ex = null);

				[Error]
				IDisposable BeginScope([Tag] string operationId);
			}
			""";

		testOutputHelper.WriteLine("=== EXAMPLE: Logging Generation ===");
		testOutputHelper.WriteLine(
			"This example demonstrates how to inspect generated Logging code."
		);

		var result = await GenerateAsync(inputSource);

		if (TestOutputConfiguration.IsOutputEnabled)
		{
			testOutputHelper.WriteLine($"? Generated content available in test-specific folder");
		}

		await TestHelpers.Verify(result, whenValidatingDiagnosticsIgnoreNonErrors: true);
	}

	[Fact]
	public async Task Example_MetricsGeneration_ShowsOutputStructure()
	{
		const string inputSource = """
			using Purview.Telemetry.Metrics;

			namespace Example;

			[Meter("example-metrics")]
			public partial interface IExampleMetrics
			{
				[Counter]
				void IncrementRequests([Tag] string endpoint, [Tag] string method = "GET");

				[Histogram]
				void RecordDuration([Tag] string operation, double durationMs);

				[UpDownCounter]
				void UpdateActiveConnections(int change);
			}
			""";

		testOutputHelper.WriteLine("=== EXAMPLE: Metrics Generation ===");
		testOutputHelper.WriteLine(
			"This example demonstrates how to inspect generated Metrics code."
		);

		var result = await GenerateAsync(inputSource);
		await TestHelpers.Verify(
			result,
			expectsDiagnostics: true,
			whenValidatingDiagnosticsIgnoreNonErrors: true
		);
	}

	[Fact]
	public async Task Example_ErrorScenario_ShowsDiagnosticsOutput()
	{
		// This example intentionally contains an error to demonstrate diagnostic output
		const string inputSource = """
			using Purview.Telemetry.Activities;

			namespace Example;

			[ActivitySource("example-service")]
			public partial interface IExampleService
			{
				[Activity]
				// This will cause an error - generic methods are not supported
				Activity? GenericMethod<T>(T parameter);
			}
			""";

		testOutputHelper.WriteLine("=== EXAMPLE: Error Scenario ===");
		testOutputHelper.WriteLine(
			"This example demonstrates how diagnostics are captured in output."
		);

		var result = await GenerateAsync(inputSource);

		if (TestOutputConfiguration.IsOutputEnabled)
		{
			if (result.Diagnostics.Length > 0)
			{
				testOutputHelper.WriteLine($"? Captured {result.Diagnostics.Length} diagnostics");
				testOutputHelper.WriteLine(
					"  Check diagnostics.txt in the output folder for details"
				);
			}
		}

		// Validate the presence of expected diagnostics without snapshot comparison
		result
			.Diagnostics.Any(d => d.Id == "TSG1005" && d.Severity == DiagnosticSeverity.Error)
			.ShouldBeTrue();
	}
}
