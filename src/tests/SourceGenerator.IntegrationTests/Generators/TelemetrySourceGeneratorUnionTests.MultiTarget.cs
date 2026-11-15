using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator.Generators;

/// <summary>
/// A union-typed parameter on an interface that registers more than one target (Activities, Logging, Metrics).
/// </summary>
/// <remarks>
/// The generated class is a <c>partial</c> across the target files (<c>*.Activity.g.cs</c>, <c>*.Logging.g.cs</c>,
/// <c>*.Metric.g.cs</c>), so a helper declared by two of them is <c>CS0111</c> for the consumer. The accessor is a
/// shared member like the DI extension, the class attributes and the constructor, and follows the same ownership rule:
/// exactly one target (Activities &gt; Logging &gt; Metrics) declares it, once per union type.
/// <para>
/// The regression these tests pin shipped in 6.0.0: each target emitted the accessor into its own file from its own
/// method list, so an interface with two targets declared the same <c>UnwrapUnion_*</c> twice.
/// </para>
/// </remarks>
public partial class TelemetrySourceGeneratorUnionTests
{
	/// <summary>A second union, so two unions on one interface can be counted independently.</summary>
	const string SecondNonBoxingUnion = """

		namespace Testing
		{
			public record class Bird(string Name);
			public record class Fish(string Name);

			[System.Runtime.CompilerServices.Union]
			public struct Animal
			{
				readonly object? _value;

				public Animal(Bird value) => _value = value;

				public Animal(Fish value) => _value = value;

				public object? Value => _value;

				public bool HasValue => _value is not null;

				public bool TryGetValue(out Bird value)
				{
					if (_value is Bird bird)
					{
						value = bird;
						return true;
					}

					value = null!;
					return false;
				}

				public bool TryGetValue(out Fish value)
				{
					if (_value is Fish fish)
					{
						value = fish;
						return true;
					}

					value = null!;
					return false;
				}
			}
		}

		""";

	/// <summary>
	/// The reporter's shape: <c>[ActivitySource]</c> and <c>[Logger]</c> on one interface, and a union-typed parameter
	/// on the one method that targets both. <c>connectionId</c> is excluded from Activities, as in the report, which is
	/// the shape that also proves a parameter-level <c>[ExcludeTargets]</c> does not move the accessor.
	/// </summary>
	const string ConnectorTelemetry = """

		namespace Testing
		{
			public sealed record ProviderConnectionId(string Value);

			[ActivitySource("testing")]
			[Logger]
			public interface IConnectorTelemetry
			{
				[Activity]
				System.Diagnostics.Activity? Retrieving(ProviderConnectionId connectionId);

				[Event(System.Diagnostics.ActivityStatusCode.Error)]
				[Warning]
				void InvalidConnection(
					System.Diagnostics.Activity? activity,
					[ExcludeTargets(Targets.Activities)] ProviderConnectionId connectionId,
					Pet error);
			}
		}

		""";

	/// <summary>The reporter's second interface: one union, on the method that records an event and logs.</summary>
	const string InventoryTelemetry = """

		namespace Testing
		{
			public sealed record ReconciliationError(string Code);

			[Logger]
			[ActivitySource("testing")]
			public interface IRepositoryInventoryStoreTelemetry
			{
				[Activity]
				[Info("Reconciling {RepositoryCount} repositories for {ProviderConnectionId}.")]
				System.Diagnostics.Activity? ReconcilingRepositories(int repositoryCount, string providerConnectionId);

				[Warning]
				[Event]
				void ReconciliationRejected(System.Diagnostics.Activity? activity, ReconciliationError error);
			}
		}

		""";

	const string ActivityAndLogging = """

		namespace Testing
		{
			[ActivitySource("testing")]
			[Logger]
			public interface IShape
			{
				[Activity]
				[Info]
				System.Diagnostics.Activity? DoWork([Tag] Pet pet);
			}
		}

		""";

	const string EventAndWarning = """

		namespace Testing
		{
			[ActivitySource("testing")]
			[Logger]
			public interface IShape
			{
				[Event("shape.failed")]
				[Warning]
				void Failed(System.Diagnostics.Activity? activity, Pet error);
			}
		}

		""";

	const string ActivityLoggingAndMetrics = """

		namespace Testing
		{
			[ActivitySource("testing")]
			[Logger]
			[Meter("testing-meter")]
			public interface IShape
			{
				[Activity]
				[Info]
				[Counter("shape.counted")]
				System.Diagnostics.Activity? DoWork([Tag] Pet pet, int value);
			}
		}

		""";

	const string ExcludedFromActivities = """

		namespace Testing
		{
			[ActivitySource("testing")]
			[Logger]
			public interface IShape
			{
				[Event("shape.failed")]
				[Warning]
				void Failed(System.Diagnostics.Activity? activity, [ExcludeTargets(Targets.Activities)] Pet error);
			}
		}

		""";

	const string TwoMethodsSameUnion = """

		namespace Testing
		{
			[ActivitySource("testing")]
			[Logger]
			public interface IShape
			{
				[Activity]
				System.Diagnostics.Activity? DoWork([Tag] Pet pet);

				[Info]
				void WorkLogged(Pet pet);
			}
		}

		""";

	const string TwoMethodsTwoUnions = """

		namespace Testing
		{
			[ActivitySource("testing")]
			[Logger]
			public interface IShape
			{
				[Activity]
				System.Diagnostics.Activity? DoWork([Tag] Pet pet);

				[Warning]
				void WorkFailed(Animal animal);
			}
		}

		""";

	/// <summary>
	/// A union that has no <c>TryGetValue</c>, so the accessor reads <c>Value</c>. A <c>union</c> declaration has this
	/// shape; the test declarations are hand-written because the test legs predate the <c>union</c> keyword.
	/// </summary>
	const string ValueOnlyUnion = """

		namespace Testing
		{
			[System.Runtime.CompilerServices.Union]
			public struct Reason
			{
				readonly object? _value;

				public Reason(Cat value) => _value = value;

				public Reason(Dog value) => _value = value;

				public object? Value => _value;
			}
		}

		""";

	/// <summary>The six emission-matrix shapes, each expected to declare its union accessor the stated number of times.</summary>
	public static IEnumerable<(string Name, string Source, int Expected)> UnionAccessorShapes =>
		[
			("ActivityAndLogging", NonBoxingUnion + ActivityAndLogging, 1),
			("EventAndWarning", NonBoxingUnion + EventAndWarning, 1),
			("ActivityLoggingAndMetrics", NonBoxingUnion + ActivityLoggingAndMetrics, 1),
			("ExcludedFromActivities", NonBoxingUnion + ExcludedFromActivities, 1),
			("TwoMethodsSameUnion", NonBoxingUnion + TwoMethodsSameUnion, 1),
			("TwoMethodsTwoUnions", NonBoxingUnion + SecondNonBoxingUnion + TwoMethodsTwoUnions, 2),
		];

	[Test]
	public async Task Generate_GivenMultiTargetInterfaceWithUnionParameter_EmitsCompilableCode(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		TelemetrySourceGeneratorTestOptions options = new()
		{
			// The diagnostics are asserted here rather than thrown, so a failure names the exact id and message.
			ThrowOnGenerationException = false,
			CompileToAssembly = true,
		};

		// Act
		using var generationResult = await GenerateAsync(
			NonBoxingUnion + ConnectorTelemetry + InventoryTelemetry,
			options,
			cancellationToken
		);

		// Assert
		var compilationErrors = generationResult
			.CompilationResult.Compilation.GetDiagnostics(cancellationToken)
			.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
			.ToList();
		var emitErrors = generationResult
			.CompilationResult.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
			.ToList();

		var errors = compilationErrors
			.Concat(emitErrors)
			.Select(static diagnostic =>
				$"{diagnostic.Id}: {diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)}"
			)
			.Distinct(StringComparer.Ordinal)
			.ToArray();

		await Assert
			.That(string.Join(Environment.NewLine, errors))
			.IsEmpty()
			.Because("one interface must declare a union accessor exactly once, whatever targets consume it");
	}

	[Test]
	public async Task Generate_GivenMultiTargetInterfaceWithUnionParameter_DeclaresTheAccessorOnce(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		// Act
		using var generationResult = await GenerateAsync(
			NonBoxingUnion + ConnectorTelemetry,
			cancellationToken: cancellationToken
		);

		// Assert
		generationResult.AssertNoCompilationErrors();

		var counts = CountAccessorDeclarations(generationResult, "UnwrapUnion_Testing_Pet");

		await Assert
			.That(counts.Values.Sum())
			.IsEqualTo(1)
			.Because($"the accessor must be declared once, but was declared as {Describe(counts)}");
	}

	[Test]
	[MethodDataSource(nameof(UnionAccessorShapes))]
	public async Task Generate_GivenUnionParameter_DeclaresTheAccessorTheExpectedNumberOfTimes(
		string name,
		string source,
		int expected
	)
	{
		// Arrange
		// Act
		using var generationResult = await GenerateAsync(source);

		// Assert
		generationResult.AssertNoCompilationErrors();

		var counts = CountAccessorDeclarations(generationResult, "UnwrapUnion_Testing_");
		var total = counts.Values.Sum();

		await Assert
			.That(total)
			.IsEqualTo(expected)
			.Because($"{name} must declare {expected} accessor(s), but declared {Describe(counts)}");
	}

	[Test]
	public async Task Generate_GivenMultiTargetInterfaceWithUnionParameter_UnwrapsTheValueAtEachCallSite(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		// Act
		using var generationResult = await GenerateAsync(
			NonBoxingUnion + ConnectorTelemetry,
			cancellationToken: cancellationToken
		);

		// Assert
		generationResult.AssertNoCompilationErrors();

		var activity = generationResult.GetSource("Activity.g.cs", HintNameMatchMode.Suffix);
		var logging = generationResult.GetSource("Logging.g.cs", HintNameMatchMode.Suffix);

		await Assert
			.That(activity)
			.Contains("tagsCollectionInvalidConnection.Add(\"error\", UnwrapUnion_Testing_Pet(error))")
			.Because("the activity event records the union's case value, not the union struct");
		await Assert
			.That(logging)
			.Contains("UnwrapUnion_Testing_Pet(error)")
			.Because("the log entry carries the union's case value");
		await Assert
			.That(activity)
			.Contains("TryGetValue(out global::Testing.Cat caseValue0)")
			.Because("the owning target declares the accessor, reading the union through its non-boxing overloads");
	}

	[Test]
	public async Task Generate_GivenMultiTargetInterfaceWithValueOnlyUnion_ReadsTheValueProperty(
		CancellationToken cancellationToken
	)
	{
		// Arrange: the union on the method that records an event and logs, so both targets see it.
		var source =
			NonBoxingUnion
			+ ValueOnlyUnion
			+ """

				namespace Testing
				{
					[ActivitySource("testing")]
					[Logger]
					public interface IShape
					{
						[Event("shape.failed")]
						[Warning]
						void Failed(System.Diagnostics.Activity? activity, Reason reason);
					}
				}

				""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		generationResult.AssertNoCompilationErrors();

		var declaring = generationResult.GetSource("Activity.g.cs", HintNameMatchMode.Suffix);

		await Assert.That(declaring).Contains("return value.Value;");
		await Assert
			.That(CountAccessorDeclarations(generationResult, "UnwrapUnion_Testing_Reason").Values.Sum())
			.IsEqualTo(1);
	}

	[Test]
	public async Task Generate_GivenLoggingAndMetricsInterfaceWithUnionOnTheMetricMethod_DeclaresTheAccessorOnce(
		CancellationToken cancellationToken
	)
	{
		// Arrange: the canonical target (Logging) does not itself see the metric method's parameters, so the accessor
		// list has to come from the interface rather than from the owning target's own method records.
		const string source =
			NonBoxingUnion
			+ """

				namespace Testing
				{
					[Logger]
					[Meter("testing-meter")]
					public interface IShape
					{
						[Info]
						void WorkLogged(string message);

						[Counter("shape.counted")]
						void WorkCounted(int value, [Tag] Pet pet);
					}
				}

				""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		generationResult.AssertNoCompilationErrors();

		var counts = CountAccessorDeclarations(generationResult, "UnwrapUnion_Testing_Pet");

		await Assert
			.That(counts.Values.Sum())
			.IsEqualTo(1)
			.Because($"the accessor must be declared once, but was declared as {Describe(counts)}");
		await Assert
			.That(string.Join(", ", counts.Keys))
			.EndsWith("Logging.g.cs")
			.Because("Logging is the canonical target when the interface registers no Activities");
	}

	[Test]
	public async Task Generate_GivenSingleTargetInterfaceWithUnionParameter_KeepsTheAccessorInTheTargetFile(
		CancellationToken cancellationToken
	)
	{
		// Arrange: a single-target interface has one generated file, so its accessor placement and shape are unchanged
		// by the multi-target fix. This pins both.
		const string source =
			NonBoxingUnion
			+ """

				namespace Testing
				{
					[Logger]
					public interface ITestLogger
					{
						[Info("Pet {Pet}")]
						void LogPet(Pet pet);
					}
				}

				""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		generationResult.AssertNoCompilationErrors();

		var counts = CountAccessorDeclarations(generationResult, "UnwrapUnion_Testing_Pet");

		await Assert.That(counts.Values.Sum()).IsEqualTo(1);
		await Assert.That(string.Join(", ", counts.Keys)).EndsWith("Logging.g.cs");

		var logging = generationResult.GetSource("Logging.g.cs", HintNameMatchMode.Suffix);

		await Assert
			.That(logging)
			.Contains("private static object? UnwrapUnion_Testing_Pet(global::Testing.Pet value)");
		await Assert.That(logging).Contains("[global::System.Runtime.CompilerServices.MethodImpl(");
		await Assert.That(logging).Contains("return null;");
	}

	[Test]
	public async Task Generate_GivenMultiTargetInterfaceWithUnionParameter_ReportsNoAnalyzerDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		// Act
		using var generationResult = await GenerateAsync(
			NonBoxingUnion + ConnectorTelemetry + InventoryTelemetry,
			cancellationToken: cancellationToken
		);

		// Assert
		var diagnostics = (generationResult.AnalyzerResult?.Diagnostics ?? [])
			.Where(static diagnostic => diagnostic.Id.StartsWith("TSG", StringComparison.Ordinal))
			.Select(static diagnostic => diagnostic.Id)
			.Distinct(StringComparer.Ordinal)
			.ToArray();

		await Assert
			.That(string.Join(", ", diagnostics))
			.IsEmpty()
			.Because("the union accessor fix must not need a new consumer-facing diagnostic");
	}

	/// <summary>
	/// Counts the declarations of an accessor across every generated source, keyed by hint name. Call sites are not
	/// counted, so a helper that is referenced from another target's file does not inflate the total.
	/// </summary>
	static Dictionary<string, int> CountAccessorDeclarations(DriverRunResult result, string prefix)
	{
		Dictionary<string, int> counts = [with(StringComparer.Ordinal)];

		foreach (var source in result.DriverResult.Results.SelectMany(static generator => generator.GeneratedSources))
		{
			var root = CSharpSyntaxTree.ParseText(source.SourceText.ToString()).GetRoot();
			var count = root.DescendantNodes()
				.OfType<MethodDeclarationSyntax>()
				.Count(method => method.Identifier.ValueText.StartsWith(prefix, StringComparison.Ordinal));

			if (count > 0)
				counts[source.HintName] = count;
		}

		return counts;
	}

	static string Describe(Dictionary<string, int> counts) =>
		counts.Count == 0
			? "no accessor at all"
			: string.Join(", ", counts.Select(static pair => $"{pair.Key}={pair.Value}"));
}
