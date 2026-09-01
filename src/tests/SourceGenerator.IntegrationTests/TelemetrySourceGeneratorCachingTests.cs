using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Purview.Telemetry.SourceGenerator;

/// <summary>
/// Verifies the per-target incremental pipeline: an unrelated edit must leave all generated
/// output unchanged, and editing one target's interface must change only that target's output.
/// </summary>
public class TelemetrySourceGeneratorCachingTests
{
	const string ActivityInterface = """
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
			.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
			.Select(path => MetadataReference.CreateFromFile(path))
			.ToList();

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
		CSharpGeneratorDriver.Create(generators: [new TelemetrySourceGenerator().AsSourceGenerator()]);

	static ImmutableDictionary<string, string> GetGeneratedSources(GeneratorDriverRunResult result)
	{
		var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
		foreach (var generated in result.Results.SelectMany(static r => r.GeneratedSources))
		{
			var hintName = generated.HintName;
			if (builder.ContainsKey(hintName))
				continue;

			builder.Add(hintName, generated.SourceText.ToString());
		}

		return builder.ToImmutable();
	}

	[Test]
	public async Task Generate_UnchangedCompilation_OutputIsStable(CancellationToken cancellationToken)
	{
		var compilation = CreateCompilation(ActivityInterface, LoggerInterface);
		var driver = CreateDriver();

		var first = driver
			.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cancellationToken)
			.GetRunResult();
		var firstSources = GetGeneratedSources(first);

		var second = driver
			.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cancellationToken)
			.GetRunResult();
		var secondSources = GetGeneratedSources(second);

		await Assert.That(secondSources.Count).IsEqualTo(firstSources.Count);
		foreach (var (hintName, text) in firstSources)
			await Assert.That(secondSources[hintName]).IsEqualTo(text);
	}

	[Test]
	public async Task Generate_UnrelatedChange_AllOutputUnchanged(CancellationToken cancellationToken)
	{
		var compilation = CreateCompilation(ActivityInterface, LoggerInterface);
		var driver = CreateDriver();
		var first = driver
			.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cancellationToken)
			.GetRunResult();
		var firstSources = GetGeneratedSources(first);

		var changed = CreateCompilation(ActivityInterface, LoggerInterface, UnrelatedType);
		var run = driver.RunGeneratorsAndUpdateCompilation(changed, out _, out _, cancellationToken).GetRunResult();
		var runSources = GetGeneratedSources(run);

		await Assert.That(runSources.Count).IsEqualTo(firstSources.Count);
		foreach (var (hintName, text) in firstSources)
			await Assert.That(runSources[hintName]).IsEqualTo(text);
	}

	[Test]
	public async Task Generate_LoggerEdit_OnlyLoggerOutputChanges(CancellationToken cancellationToken)
	{
		var compilation = CreateCompilation(ActivityInterface, LoggerInterface);
		var driver = CreateDriver();
		var first = driver
			.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cancellationToken)
			.GetRunResult();
		var firstSources = GetGeneratedSources(first);

		// Edit only the logger interface.
		var editedLogger = LoggerInterface.Replace("void Log(", "void Log2(", StringComparison.Ordinal);
		var edited = CreateCompilation(ActivityInterface, editedLogger);
		var run = driver.RunGeneratorsAndUpdateCompilation(edited, out _, out _, cancellationToken).GetRunResult();
		var runSources = GetGeneratedSources(run);

		await Assert.That(runSources.Count).IsEqualTo(firstSources.Count);
		foreach (var (hintName, text) in firstSources)
		{
			if (
				!hintName.EndsWith(".Activity.g.cs", StringComparison.Ordinal)
				&& !hintName.EndsWith(".Logging.g.cs", StringComparison.Ordinal)
			)
				continue;

			var changed = !runSources[hintName].Equals(text, StringComparison.Ordinal);
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
