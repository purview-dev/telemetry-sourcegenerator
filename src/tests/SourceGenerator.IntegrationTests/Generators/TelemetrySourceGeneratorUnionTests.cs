using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator.Generators;

/// <summary>
/// C# 15 union parameters. A union is never recorded as itself: the generated code unwraps it to its case
/// value, through the non-boxing <c>TryGetValue</c>/<c>HasValue</c> pattern when the union declares one.
/// </summary>
/// <remarks>
/// The unions in these sources are hand-written rather than declared with the <c>union</c> keyword, so the
/// tests stay valid on every leg (net48 and net10.0 have no <c>UnionAttribute</c>). Detection is by the
/// attribute's name, so a compiler-generated union and a hand-written one take the same path; what differs
/// is only whether the non-boxing access pattern is present.
/// </remarks>
[SkipOnNetFramework] // the test sources need record types and nullable annotations, whose support types
// (.NET Framework has neither IsExternalInit nor UnionAttribute) are not in the box.
public partial class TelemetrySourceGeneratorUnionTests : IncrementalSourceGeneratorTestBase<TelemetrySourceGenerator>
{
	/// <summary>A union that declares the non-boxing access pattern.</summary>
	const string NonBoxingUnion = """

		using System;

		namespace System.Runtime.CompilerServices
		{
			[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
			public sealed class UnionAttribute : Attribute { }
		}

		namespace Testing
		{
			public record class Cat(string Name);
			public record class Dog(string Name);

			[System.Runtime.CompilerServices.Union]
			public struct Pet
			{
				readonly object? _value;

				public Pet(Cat value) => _value = value;

				public Pet(Dog value) => _value = value;

				public object? Value => _value;

				public bool HasValue => _value is not null;

				public bool TryGetValue(out Cat value)
				{
					if (_value is Cat cat)
					{
						value = cat;
						return true;
					}

					value = null!;
					return false;
				}

				public bool TryGetValue(out Dog value)
				{
					if (_value is Dog dog)
					{
						value = dog;
						return true;
					}

					value = null!;
					return false;
				}
			}
		}

		""";

	[Test]
	public async Task Generate_GivenUnionTagAndBaggageParameters_UnwrapsEveryCase(CancellationToken cancellationToken)
	{
		// Arrange
		var source =
			NonBoxingUnion
			+ """

				namespace Testing
				{
					[ActivitySource("testing")]
					public interface ITestActivities
					{
						[Activity]
						void DoWork([Tag] Pet pet, [Baggage] Pet baggage);
					}
				}

				""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		generationResult.AssertNoCompilationErrors();

		var generated = generationResult.GetSource("Activity.g.cs", HintNameMatchMode.Suffix);

		await Assert.That(generated).Contains("UnwrapUnion_Testing_Pet");
		await Assert.That(generated).Contains("value.TryGetValue(out global::Testing.Cat caseValue0)");
		await Assert.That(generated).Contains("value.TryGetValue(out global::Testing.Dog caseValue1)");
		await Assert.That(generated).Contains("SetTag(\"pet\", UnwrapUnion_Testing_Pet(pet))");
		await Assert.That(generated).Contains("SetBaggage(\"baggage\", UnwrapUnion_Testing_Pet(baggage)?.ToString())");
	}

	[Test]
	public async Task Generate_GivenUnionTagParameter_ReadsTheCaseWithoutBoxingTheUnion(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source =
			NonBoxingUnion
			+ """

				namespace Testing
				{
					[Meter("testing-meter")]
					public interface ITestMetrics
					{
						[Counter]
						void Increment(int value, [Tag] Pet pet);
					}
				}

				""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		generationResult.AssertNoCompilationErrors();

		var generated = generationResult.GetSource("Metric.g.cs", HintNameMatchMode.Suffix);

		await Assert.That(generated).Contains("UnwrapUnion_Testing_Pet");
		await Assert.That(generated).Contains(", UnwrapUnion_Testing_Pet(pet))");
	}

	[Test]
	public async Task Generate_GivenUnionLogParameterWithV2Generation_LogsTheCaseValue(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source =
			NonBoxingUnion
			+ """

				namespace Testing
				{
					[Logger(GenerationMode = LoggerGenerationMode.V2)]
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

		var generated = generationResult.GetSource("Logging.g.cs", HintNameMatchMode.Suffix);

		await Assert.That(generated).Contains("UnwrapUnion_Testing_Pet");
		await Assert.That(generated).Contains("public LogPet_LogState(object? pet)");
		await Assert.That(generated).Contains("new LogPet_LogState(UnwrapUnion_Testing_Pet(pet))");
		await Assert.That(generated).Contains("object? _Pet");
	}

	[Test]
	public async Task Generate_GivenUnionLogParameterWithV1Generation_LogsTheCaseValue(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source =
			NonBoxingUnion
			+ """

				namespace Testing
				{
					[Logger(GenerationMode = LoggerGenerationMode.V1)]
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

		var generated = generationResult.GetSource("Logging.g.cs", HintNameMatchMode.Suffix);

		await Assert.That(generated).Contains("UnwrapUnion_Testing_Pet");
		await Assert.That(generated).Contains("Define<object?>");
	}

	[Test]
	public async Task Generate_GivenUnionWithoutNonBoxingAccess_FallsBackToTheValueProperty(
		CancellationToken cancellationToken
	)
	{
		// Arrange: a union declared with the `union` keyword has no TryGetValue, only Value.
		const string source = """

			using System;

			namespace System.Runtime.CompilerServices
			{
				[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
				public sealed class UnionAttribute : Attribute { }
			}

			namespace Testing
			{
				public record class Cat(string Name);
				public record class Dog(string Name);

				[System.Runtime.CompilerServices.Union]
				public struct Pet
				{
					readonly object? _value;

					public Pet(Cat value) => _value = value;

					public Pet(Dog value) => _value = value;

					public object? Value => _value;
				}

				[ActivitySource("testing")]
				public interface ITestActivities
				{
					[Activity]
					void DoWork([Tag] Pet pet);
				}
			}

			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		generationResult.AssertNoCompilationErrors();

		var generated = generationResult.GetSource("Activity.g.cs", HintNameMatchMode.Suffix);

		await Assert.That(generated).Contains("return value.Value;");
		await Assert.That(generated).Contains("SetTag(\"pet\", UnwrapUnion_Testing_Pet(pet))");
	}
}
