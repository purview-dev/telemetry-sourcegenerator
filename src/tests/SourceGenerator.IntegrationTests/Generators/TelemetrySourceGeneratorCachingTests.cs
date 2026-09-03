using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator.Generators;

/// <summary>
/// Verifies the incremental pipeline caches across unchanged runs and invalidates only the
/// affected target when an interface is edited. Runs through the framework's incremental harness
/// (<c>GenerateIncrementalAsync</c>) and asserts on the generator's tracking-named stages, per the
/// <c>source-generator-testing</c> guidance.
/// </summary>
public class TelemetrySourceGeneratorCachingTests : IncrementalSourceGeneratorTestBase<TelemetrySourceGenerator>
{
	const string ActivityInterface = """
		using Purview.Telemetry;

		namespace Testing;

		[ActivitySource("testing-activity-source")]
		public interface ITestActivities
		{
			[Activity]
			System.Diagnostics.Activity? Activity([Tag]string stringParam);
		}
		""";

	const string LoggerInterface = """
		using Microsoft.Extensions.Logging;
		using Purview.Telemetry;

		namespace Testing;

		[Logger]
		public interface ITestLogger
		{
			void Log([Tag]string stringParam);
		}
		""";

	const string UnrelatedType = """
		namespace Testing;

		public class Unrelated { public int Value { get; set; } }
		""";

	static readonly TelemetrySourceGeneratorTestOptions Options = new();

	/// <summary>
	/// The framework-named pipeline stages this generator participates in. The generator overrides the
	/// <c>ForAttributeWithMetadataName</c> tracking names (<c>TelemetrySourceGenerator_*</c>); the
	/// remaining stages come from the framework's <c>GenerationContextValueProvider</c>
	/// (<c>GetGenerationContext_*</c>). The generator passes a <see langword="null"/> disable-property
	/// name, so no <c>GetMSBuildPropertyValue_*</c> stages exist and a property-only invalidation test
	/// is not applicable.
	/// </summary>
	static bool IsFrameworkStage(string trackingName) =>
		trackingName.StartsWith("TelemetrySourceGenerator_", StringComparison.Ordinal)
		|| trackingName.StartsWith("GetGenerationContext_", StringComparison.Ordinal);

	static ImmutableArray<IncrementalStepRunReason> GetFrameworkReasons(IncrementalCacheRun run) =>
		[
			.. run
				.Steps.Where(static kvp => IsFrameworkStage(kvp.Key))
				.SelectMany(static kvp => kvp.Value)
				.SelectMany(static step => step.Outputs)
				.Select(static output => output.Reason),
		];

	static ImmutableArray<IncrementalStepRunReason> GetStageReasons(IncrementalCacheRun run, string trackingName)
	{
		return
		[
			.. run
				.Steps.Where(kvp => kvp.Key == trackingName)
				.SelectMany(kvp => kvp.Value)
				.SelectMany(static step => step.Outputs)
				.Select(static output => output.Reason),
		];
	}

	static ImmutableDictionary<string, string> GetGeneratedSources(GeneratorRunResult result)
	{
		var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
		foreach (var generated in result.GeneratedSources)
		{
			if (builder.ContainsKey(generated.HintName))
				continue;

			builder.Add(generated.HintName, generated.SourceText.ToString());
		}

		return builder.ToImmutable();
	}

	[Test]
	public async Task Generate_UnchangedCompilation_OutputCached(CancellationToken cancellationToken)
	{
		var result = await GenerateIncrementalAsync([ActivityInterface, LoggerInterface], Options, cancellationToken);

		await Assert
			.That(GetFrameworkReasons(result.Runs[0]).All(static reason => reason == IncrementalStepRunReason.New))
			.IsTrue()
			.Because("the first run must produce every output");
		await Assert
			.That(
				GetFrameworkReasons(result.Runs[1])
					.All(static reason =>
						reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged
					)
			)
			.IsTrue()
			.Because("an unchanged compilation must not re-run any target");
	}

	[Test]
	public async Task Generate_UnchangedCompilation_OutputIsStable(CancellationToken cancellationToken)
	{
		var result = await GenerateIncrementalAsync([ActivityInterface, LoggerInterface], Options, cancellationToken);

		var first = GetGeneratedSources(result.Runs[0].RunResult);
		var second = GetGeneratedSources(result.Runs[1].RunResult);

		await Assert.That(second.Count).IsEqualTo(first.Count);
		foreach (var source in first)
			await Assert.That(second[source.Key]).IsEqualTo(source.Value);
	}

	[Test]
	public async Task Generate_UnrelatedChange_OutputStaysCached(CancellationToken cancellationToken)
	{
		var inputs = new[]
		{
			new IncrementalRunInput([ActivityInterface, LoggerInterface], []),
			new IncrementalRunInput([ActivityInterface, LoggerInterface, UnrelatedType], []),
		};
		var result = await GenerateIncrementalAsync(inputs, Options, cancellationToken);

		var reasons = GetFrameworkReasons(result.Runs[1]);
		await Assert.That(reasons).IsNotEmpty();
		await Assert
			.That(
				reasons.All(static reason =>
					reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged
				)
			)
			.IsTrue()
			.Because("an unrelated edit must not invalidate the generated output");
	}

	[Test]
	public async Task Generate_UnrelatedChange_AllOutputUnchanged(CancellationToken cancellationToken)
	{
		var inputs = new[]
		{
			new IncrementalRunInput([ActivityInterface, LoggerInterface], []),
			new IncrementalRunInput([ActivityInterface, LoggerInterface, UnrelatedType], []),
		};
		var result = await GenerateIncrementalAsync(inputs, Options, cancellationToken);

		var first = GetGeneratedSources(result.Runs[0].RunResult);
		var second = GetGeneratedSources(result.Runs[1].RunResult);

		await Assert.That(second.Count).IsEqualTo(first.Count);
		foreach (var source in first)
			await Assert.That(second[source.Key]).IsEqualTo(source.Value);
	}

	[Test]
	public async Task Generate_ActivityEdit_OutputReRuns(CancellationToken cancellationToken)
	{
		var edited = ActivityInterface.ReplaceOrdinal("Activity([Tag]", "Activity2([Tag]");
		var inputs = new[] { new IncrementalRunInput([ActivityInterface], []), new IncrementalRunInput([edited], []) };
		var result = await GenerateIncrementalAsync(inputs, Options, cancellationToken);

		var reasons = GetStageReasons(result.Runs[1], "TelemetrySourceGenerator_Activities");
		await Assert
			.That(reasons.Any(static reason => reason == IncrementalStepRunReason.Modified))
			.IsTrue()
			.Because("editing the interface must re-run generation");
	}

	[Test]
	public async Task Generate_LoggerEdit_OnlyLoggerOutputChanges(CancellationToken cancellationToken)
	{
		var editedLogger = LoggerInterface.ReplaceOrdinal("void Log(", "void Log2(");
		var inputs = new[]
		{
			new IncrementalRunInput([ActivityInterface, LoggerInterface], []),
			new IncrementalRunInput([ActivityInterface, editedLogger], []),
		};
		var result = await GenerateIncrementalAsync(inputs, Options, cancellationToken);

		var loggerReasons = GetStageReasons(result.Runs[1], "TelemetrySourceGenerator_Logging");
		await Assert
			.That(loggerReasons.Any(static reason => reason == IncrementalStepRunReason.Modified))
			.IsTrue()
			.Because("editing the logger interface must re-run the logging stage");

		var activityReasons = GetStageReasons(result.Runs[1], "TelemetrySourceGenerator_Activities");
		await Assert
			.That(
				activityReasons.All(static reason =>
					reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged
				)
			)
			.IsTrue()
			.Because("editing the logger must not invalidate the activity stage");

		var first = GetGeneratedSources(result.Runs[0].RunResult);
		var second = GetGeneratedSources(result.Runs[1].RunResult);

		foreach (var source in first)
		{
			var hintName = source.Key;
			if (
				!hintName.EndsWith(".Activity.g.cs", StringComparison.Ordinal)
				&& !hintName.EndsWith(".Logging.g.cs", StringComparison.Ordinal)
			)
				continue;

			var changed = !second[hintName].Equals(source.Value, StringComparison.Ordinal);
			if (hintName.EndsWith(".Activity.g.cs", StringComparison.Ordinal))
				await Assert
					.That(changed)
					.IsFalse()
					.Because($"editing the logger must not change the activity output '{hintName}'");
			else
				await Assert
					.That(changed)
					.IsTrue()
					.Because($"editing the logger must change the logger output '{hintName}'");
		}
	}
}
