using Purview.Telemetry.SourceGenerator.Configuration;

namespace Purview.Telemetry.SourceGenerator;

public class TelemetrySourceGeneratorMultiTargetTests(ITestOutputHelper testOutputHelper)
	: IncrementalSourceGeneratorTestBase<TelemetrySourceGenerator>(testOutputHelper)
{
	[Fact]
	public async Task Generate_GivenBasicMultiTargetMethod_GeneratesCorrectly()
	{
		const string source = """
				using System;
				using Microsoft.Extensions.Logging;
				using System.Diagnostics;

				[assembly: Purview.Telemetry.EnableMultiTargetGeneration]

				namespace Test;

				[Purview.Telemetry.TelemetryGeneration]
				public partial interface ITestService
				{
					[Purview.Telemetry.Telemetry(
						GenerateActivity = true,
						GenerateLogging = true,
						ActivityName = "test_operation",
						LogMessage = "Test operation executed"
					)]
					void TestOperation(string userId, int count);
				}
			""";

		var generationResult = await GenerateAsync(source);
		
		// Write detailed output information if enabled
		if (TestOutputConfiguration.IsOutputEnabled)
		{
			testOutputHelper.WriteLine($"Generated content written to: {TestOutputConfiguration.OutputDirectory}");
			testOutputHelper.WriteLine($"Total generated sources: {generationResult.DriverResult.Results.SelectMany(r => r.GeneratedSources).Count()}");
		}

		await TestHelpers.Verify(generationResult, validationCompilation: false);
	}

	[Fact]
	public async Task Generate_GivenMultiTargetWithExclusions_GeneratesCorrectly()
	{
		const string source = """
				using System;
				using Microsoft.Extensions.Logging;
				using System.Diagnostics;

				[assembly: Purview.Telemetry.EnableMultiTargetGeneration]

				namespace Test;

				[Purview.Telemetry.TelemetryGeneration]
				public partial interface ITestService
				{
					[Purview.Telemetry.Telemetry(
						GenerateActivity = true,
						GenerateLogging = true,
						GenerateMetrics = true
					)]
					void TestOperation(
						string userId,
						[Purview.Telemetry.ExcludeFromActivity] string internalId,
						[Purview.Telemetry.ExcludeFromLogging] int sensitiveCount,
						[Purview.Telemetry.ExcludeFromMetrics] DateTime timestamp
					);
				}
			""";

		var generationResult = await GenerateAsync(source);
		
		// Write detailed output information if enabled
		if (TestOutputConfiguration.IsOutputEnabled)
		{
			testOutputHelper.WriteLine($"Generated content written to: {TestOutputConfiguration.OutputDirectory}");
			testOutputHelper.WriteLine($"Total generated sources: {generationResult.DriverResult.Results.SelectMany(r => r.GeneratedSources).Count()}");
		}

		await TestHelpers.Verify(generationResult, validationCompilation: false);
	}

	[Fact]
	public async Task Generate_GivenMultiTargetWithTagsAndBaggage_GeneratesCorrectly()
	{
		const string source = """
				using System;
				using Microsoft.Extensions.Logging;
				using System.Diagnostics;

				[assembly: Purview.Telemetry.EnableMultiTargetGeneration]

				namespace Test;

				[Purview.Telemetry.TelemetryGeneration]
				public partial interface ITestService
				{
					[Purview.Telemetry.Telemetry(
						GenerateActivity = true,
						GenerateLogging = true
					)]
					void TestOperation(
						[Purview.Telemetry.Tag("user_id")] string userId,
						[Purview.Telemetry.Activities.Baggage("operation_context")] string context,
						int count
					);
				}
			""";

		var generationResult = await GenerateAsync(source);
		
		// Write detailed output information if enabled
		if (TestOutputConfiguration.IsOutputEnabled)
		{
			testOutputHelper.WriteLine($"Generated content written to: {TestOutputConfiguration.OutputDirectory}");
			testOutputHelper.WriteLine($"Total generated sources: {generationResult.DriverResult.Results.SelectMany(r => r.GeneratedSources).Count()}");
		}

		await TestHelpers.Verify(generationResult, validationCompilation: false);
	}

	[Fact]
	public async Task Generate_GivenComplexMultiTargetScenario_OutputsDetailedContent()
	{
		const string inputSource = """
				using System;
				using Microsoft.Extensions.Logging;
				using System.Diagnostics;

				[assembly: Purview.Telemetry.EnableMultiTargetGeneration]

				namespace Test.Complex;

				[Purview.Telemetry.TelemetryGeneration]
				public partial interface IOrderService
				{
					[Purview.Telemetry.Telemetry(
						GenerateActivity = true,
						GenerateLogging = true,
						GenerateMetrics = true,
						ActivityName = "process_order",
						LogMessage = "Processing order {orderId} for customer {customerId}"
					)]
					Task<bool> ProcessOrderAsync(
						[Purview.Telemetry.Tag("order.id")] string orderId,
						[Purview.Telemetry.Tag("customer.id")] int customerId,
						[Purview.Telemetry.Activities.Baggage("correlation.id")] string correlationId,
						[Purview.Telemetry.ExcludeFromMetrics] DateTime timestamp,
						[Purview.Telemetry.ExcludeFromLogging] string internalToken
					);

					[Purview.Telemetry.Telemetry(
						GenerateActivity = true,
						GenerateLogging = true,
						ActivityName = "cancel_order"
					)]
					void CancelOrder(
						[Purview.Telemetry.Tag] string orderId,
						[Purview.Telemetry.ExcludeFromActivity] string reason
					);

					[Purview.Telemetry.Telemetry(
						GenerateMetrics = true
					)]
					void IncrementOrderCount(
						[Purview.Telemetry.Tag("order.type")] string orderType,
						int count = 1
					);
				}
			""";

		var generationResult = await GenerateAsync(inputSource);

		// This test specifically demonstrates output capabilities
		if (TestOutputConfiguration.IsOutputEnabled)
		{
			var outputDir = TestOutputConfiguration.OutputDirectory;
			testOutputHelper.WriteLine($"=== DETAILED GENERATION OUTPUT ===");
			testOutputHelper.WriteLine($"Output Directory: {outputDir}");
			testOutputHelper.WriteLine($"Total Generated Sources: {generationResult.DriverResult.Results.SelectMany(r => r.GeneratedSources).Count()}");
			testOutputHelper.WriteLine($"Diagnostics Count: {generationResult.Diagnostics.Length}");
			testOutputHelper.WriteLine($"Has Errors: {generationResult.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error)}");
			testOutputHelper.WriteLine($"Has Warnings: {generationResult.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Warning)}");

			// List generated files
			testOutputHelper.WriteLine("Generated Files:");
			foreach (var driverResult in generationResult.DriverResult.Results)
			{
				foreach (var generatedSourceFile in driverResult.GeneratedSources)
				{
					testOutputHelper.WriteLine($"  - {generatedSourceFile.HintName}");
				}
			}
		}
		else
		{
			testOutputHelper.WriteLine("Set PURVIEW_TELEMETRY_OUTPUT_GENERATED_FILES=true to see detailed output");
		}

		await TestHelpers.Verify(generationResult, validationCompilation: false);
	}
}
