using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator;

/// <summary>
/// Verifies the incremental pipeline caches across unchanged runs and invalidates only
/// the affected target when an interface is edited.
/// </summary>
public class TelemetrySourceGeneratorCachingTests
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

	static ImmutableArray<MetadataReference> References { get; } = BuildReferences();

	static ImmutableArray<MetadataReference> BuildReferences()
	{
		var trusted = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "")
			.Split([Path.PathSeparator], StringSplitOptions.RemoveEmptyEntries)
			.Select(path => MetadataReference.CreateFromFile(path))
			.ToList();

		// .NET Framework does not populate TRUSTED_PLATFORM_ASSEMBLIES.
		if (trusted.Count == 0)
		{
			trusted.AddRange(
				AppDomain
					.CurrentDomain.GetAssemblies()
					.Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
					.Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
			);
		}

		foreach (
			var type in new[]
			{
				typeof(Activity),
				typeof(Meter),
				typeof(IServiceCollection),
				typeof(LogLevel),
				typeof(LogPropertiesAttribute),
			}
		)
		{
			trusted.Add(MetadataReference.CreateFromFile(type.Assembly.Location));
		}

		return [.. trusted];
	}

	static CSharpCompilation CreateCompilation(params string[] sources)
	{
		var trees = sources.Select(source => CSharpSyntaxTree.ParseText(source));
		return CSharpCompilation.Create(
			"CachingTest",
			trees,
			References,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);
	}

	static CSharpGeneratorDriver CreateDriver() =>
		CSharpGeneratorDriver.Create(
			generators: [new TelemetrySourceGenerator().AsSourceGenerator()],
			driverOptions: new GeneratorDriverOptions(
				IncrementalGeneratorOutputKind.None,
				trackIncrementalGeneratorSteps: true
			)
		);

	static ImmutableArray<IncrementalStepRunReason> GetSourceOutputReasons(GeneratorDriverRunResult result)
	{
		return
		[
			.. result
				.Results.SelectMany(static r => r.TrackedSteps)
				.Where(static kvp => kvp.Key == "SourceOutput")
				.SelectMany(static kvp => kvp.Value)
				.SelectMany(static runStep => runStep.Outputs)
				.Select(static o => o.Reason),
		];
	}

	[Test]
	public async Task Generate_UnchangedCompilation_OutputCached(CancellationToken cancellationToken)
	{
		var compilation = CreateCompilation(ActivityInterface);
		GeneratorDriver driver = CreateDriver();

		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cancellationToken);
		var first = driver.GetRunResult();
		await Assert
			.That(GetSourceOutputReasons(first).All(static r => r == IncrementalStepRunReason.New))
			.IsTrue()
			.Because("the first run must produce every output");

		// Same compilation, same driver: every output must be cached/unchanged.
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cancellationToken);
		var second = driver.GetRunResult();
		var reasons = GetSourceOutputReasons(second);
		await Assert.That(reasons).IsNotEmpty();
		await Assert
			.That(
				reasons.All(static reason =>
					reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged
				)
			)
			.IsTrue()
			.Because("an unchanged compilation must not re-run any target");
	}

	[Test]
	public async Task Generate_UnrelatedChange_OutputStaysCached(CancellationToken cancellationToken)
	{
		var compilation = CreateCompilation(ActivityInterface, LoggerInterface);
		GeneratorDriver driver = CreateDriver();
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cancellationToken);

		// Adding an unrelated type must not invalidate any output.
		var changed = CreateCompilation(ActivityInterface, LoggerInterface, UnrelatedType);
		driver = driver.RunGeneratorsAndUpdateCompilation(changed, out _, out _, cancellationToken);
		var reasons = GetSourceOutputReasons(driver.GetRunResult());
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
	public async Task Generate_ActivityEdit_OutputReRuns(CancellationToken cancellationToken)
	{
		var compilation = CreateCompilation(ActivityInterface);
		GeneratorDriver driver = CreateDriver();
		driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cancellationToken);

		// Edit the activity interface source (change the method name so the output changes).
		var edited = CreateCompilation(ActivityInterface.ReplaceOrdinal("Activity([Tag]", "Activity2([Tag]"));
		driver = driver.RunGeneratorsAndUpdateCompilation(edited, out _, out _, cancellationToken);
		var reasons = GetSourceOutputReasons(driver.GetRunResult());
		await Assert
			.That(reasons.Any(static reason => reason == IncrementalStepRunReason.Modified))
			.IsTrue()
			.Because("editing the interface must re-run generation");
	}
}
