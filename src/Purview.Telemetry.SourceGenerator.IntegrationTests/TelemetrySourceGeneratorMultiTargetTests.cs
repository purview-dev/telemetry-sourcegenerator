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
		await TestHelpers.Verify(generationResult);
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
		await TestHelpers.Verify(generationResult);
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
		await TestHelpers.Verify(generationResult);
	}
}
