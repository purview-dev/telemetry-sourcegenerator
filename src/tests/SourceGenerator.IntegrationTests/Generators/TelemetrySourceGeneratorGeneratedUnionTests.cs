using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator.Generators;

/// <summary>
/// Union parameters whose type is a <c>[Purview.Unions.GenerateUnion]</c> target. The unions generator emits the
/// <c>[Union]</c> attribute, the constructors, <c>HasValue</c> and one <c>TryGetValue(out T)</c> per case into
/// generated output, which Roslyn never shows another generator. So the telemetry generator has to read the case list
/// from what <em>is</em> in source — the attribute's <c>typeof</c> list, the partial static factory signatures, a
/// nested <c>IUnionMembers</c> provider, or the constructors — and then emit the accessor against the members that will
/// exist once both generators have run.
/// </summary>
/// <remarks>
/// The union generator is not referenced from this repository, so each test declares a stand-in
/// <c>Purview.Unions.GenerateUnionAttribute</c> and a union with source-visible <c>HasValue</c>/<c>TryGetValue</c>/
/// <c>Value</c> members, which is what makes the emitted accessor compile when only this generator runs. The case list
/// still has to come from the <c>[GenerateUnion]</c> source surface, which is what the assertions pin.
/// <para>
/// The real two-generator integration test — Purview.Unions and this generator in one compilation — is a follow-up: the
/// package is not on a CI-safe feed yet, so it cannot be a ProjectReference or PackageReference here.
/// </para>
/// </remarks>
[SkipOnNetFramework] // the test sources need records and nullable annotations, whose support types are not in the .NET Framework box.
public partial class TelemetrySourceGeneratorGeneratedUnionTests
	: IncrementalSourceGeneratorTestBase<TelemetrySourceGenerator>
{
	/// <summary>
	/// The stand-in <c>Purview.Unions.GenerateUnionAttribute</c>, matched by name and namespace. Its constructor
	/// mirrors the real one: a <c>params Type[]</c> case list.
	/// </summary>
	const string GenerateUnionAttribute = """

		using System;

		namespace Purview.Unions
		{
			[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
			public sealed class GenerateUnionAttribute : Attribute
			{
				public GenerateUnionAttribute(params Type[] caseTypes) => CaseTypes = caseTypes;

				public Type[] CaseTypes { get; }
			}
		}

		""";

	const string CaseTypes = """

		namespace Testing
		{
			public record class Cat(string Name);

			public record class Dog(string Name);

			public record class Bird(string Name);
		}

		""";

	/// <summary>
	/// Surface A: the case list is the attribute's <c>typeof</c> list. The union also has a public single-parameter
	/// constructor for a type that is <em>not</em> a case, so the assertions prove the attribute list wins rather than
	/// the constructors being picked up.
	/// </summary>
	const string SurfaceAUnion = """

		namespace Testing
		{
			[Purview.Unions.GenerateUnion(typeof(Cat), typeof(Dog))]
			public partial struct Pet
			{
				readonly object? _value;

				Pet(object? value) => _value = value;

				public Pet(Bird value) => _value = value;

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
	public async Task Generate_GivenGeneratedUnionSurfaceA_UnwrapsTheCaseValueForActivitiesAndMetrics(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source =
			GenerateUnionAttribute
			+ CaseTypes
			+ SurfaceAUnion
			+ """

				namespace Testing
				{
					[ActivitySource("testing")]
					public interface ITestActivities
					{
						[Activity]
						void DoWork([Tag] Pet pet, [Baggage] Pet baggage);
					}

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

		var activity = generationResult.GetSource("Activity.g.cs", HintNameMatchMode.Suffix);

		await Assert.That(activity).Contains("UnwrapUnion_Testing_Pet");
		await Assert.That(activity).Contains("value.TryGetValue(out global::Testing.Cat caseValue0)");
		await Assert.That(activity).Contains("value.TryGetValue(out global::Testing.Dog caseValue1)");
		await Assert.That(activity).Contains("SetTag(\"pet\", UnwrapUnion_Testing_Pet(pet))");
		await Assert.That(activity).Contains("SetBaggage(\"baggage\", UnwrapUnion_Testing_Pet(baggage)?.ToString())");

		// The case list came from the attribute, not from the union's constructors.
		await Assert
			.That(activity)
			.DoesNotContain("global::Testing.Bird")
			.Because("only the attribute's typeof list supplies cases");

		var metric = generationResult.GetSource("Metric.g.cs", HintNameMatchMode.Suffix);

		await Assert.That(metric).Contains("UnwrapUnion_Testing_Pet");
		await Assert.That(metric).Contains(", UnwrapUnion_Testing_Pet(pet))");
	}

	[Test]
	public async Task Generate_GivenGeneratedUnionSurfaceAWithV2Logging_LogsTheCaseValue(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source =
			GenerateUnionAttribute
			+ CaseTypes
			+ SurfaceAUnion
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
		await Assert.That(generated).Contains("new LogPet_LogState(UnwrapUnion_Testing_Pet(pet))");
		await Assert.That(generated).Contains("object? _Pet");
	}

	[Test]
	public async Task Generate_GivenGeneratedUnionSurfaceAWithV1Logging_LogsTheCaseValue(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source =
			GenerateUnionAttribute
			+ CaseTypes
			+ SurfaceAUnion
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
	public async Task Generate_GivenGeneratedUnionSurfaceB_ReadsTheFactoryCases(CancellationToken cancellationToken)
	{
		// Arrange: no attribute case list, so the partial static factory signatures are the case list. The factories
		// carry an implementing part so the stand-in compiles without the unions generator; the definition part is what
		// makes them a case list.
		var source =
			GenerateUnionAttribute
			+ CaseTypes
			+ """

				namespace Testing
				{
					[Purview.Unions.GenerateUnion]
					public partial struct Pet
					{
						public static partial Pet Create(Cat value);

						public static partial Pet Create(Dog value);

						public static partial Pet Create(Cat value) => new(value);

						public static partial Pet Create(Dog value) => new(value);

						readonly object? _value;

						Pet(object? value) => _value = value;

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

		await Assert.That(generated).Contains("UnwrapUnion_Testing_Pet");
		await Assert.That(generated).Contains("value.TryGetValue(out global::Testing.Cat caseValue0)");
		await Assert.That(generated).Contains("value.TryGetValue(out global::Testing.Dog caseValue1)");
		await Assert.That(generated).Contains("SetTag(\"pet\", UnwrapUnion_Testing_Pet(pet))");
	}

	[Test]
	public async Task Generate_GivenGeneratedUnionSurfaceC_ReadsTheProviderCases(CancellationToken cancellationToken)
	{
		// Arrange: a nested IUnionMembers provider is the portable route when another generator also needs the case
		// list. The union also has a constructor for a non-case type, so the provider is what supplies the list.
		var source =
			GenerateUnionAttribute
			+ CaseTypes
			+ """

				namespace Testing
				{
					[Purview.Unions.GenerateUnion]
					public partial struct Pet
					{
						public interface IUnionMembers
						{
							static Pet Create(Cat value) => new(value);

							static Pet Create(Dog value) => new(value);
						}

						readonly object? _value;

						Pet(object? value) => _value = value;

						public Pet(Bird value) => _value = value;

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

		await Assert.That(generated).Contains("UnwrapUnion_Testing_Pet");
		await Assert.That(generated).Contains("value.TryGetValue(out global::Testing.Cat caseValue0)");
		await Assert.That(generated).Contains("value.TryGetValue(out global::Testing.Dog caseValue1)");
		await Assert.That(generated).DoesNotContain("global::Testing.Bird");
	}

	[Test]
	public async Task Generate_GivenGeneratedUnionWithNoVisibleCases_RecordsTheUnionAsAnOrdinaryValue(
		CancellationToken cancellationToken
	)
	{
		// Arrange: no attribute list, no factories, no provider, no constructors. The unions generator reports UNI0003
		// for this, so telemetry stays silent and records the value as it would any other.
		var source =
			GenerateUnionAttribute
			+ CaseTypes
			+ """

				namespace Testing
				{
					[Purview.Unions.GenerateUnion]
					public partial struct Pet { }

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
		await Assert.That(generationResult).HasNoErrorDiagnostics();

		var generated = generationResult.GetSource("Activity.g.cs", HintNameMatchMode.Suffix);

		await Assert.That(generated).DoesNotContain("UnwrapUnion_");
		await Assert.That(generated).Contains("SetTag(\"pet\", pet)");
	}

	[Test]
	public async Task Generate_GivenGeneratedUnionWithNullableCases_NormalisesThemAsTheUnionGeneratorDoes(
		CancellationToken cancellationToken
	)
	{
		// Arrange: a nullable value type is exposed through its underlying type, so the case is int and the accessor
		// reads TryGetValue(out int). A nullable reference annotation is not part of a case's identity.
		var source =
			GenerateUnionAttribute
			+ """

				namespace Testing
				{
					[Purview.Unions.GenerateUnion(typeof(int?), typeof(string))]
					public partial struct Pet
					{
						readonly object? _value;

						Pet(object? value) => _value = value;

						public object? Value => _value;

						public bool HasValue => _value is not null;

						public bool TryGetValue(out int value)
						{
							if (_value is int number)
							{
								value = number;
								return true;
							}

							value = 0;
							return false;
						}

						public bool TryGetValue(out string value)
						{
							if (_value is string text)
							{
								value = text;
								return true;
							}

							value = string.Empty;
							return false;
						}
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

		await Assert.That(generated).Contains("value.TryGetValue(out int caseValue0)");
		await Assert.That(generated).Contains("value.TryGetValue(out string caseValue1)");
		await Assert.That(generated).DoesNotContain("int? caseValue");
	}

	[Test]
	public async Task Generate_GivenGeneratedUnionWithNullableReferenceFactoryCase_StripsTheAnnotation(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var source =
			GenerateUnionAttribute
			+ CaseTypes
			+ """

				namespace Testing
				{
					[Purview.Unions.GenerateUnion]
					public partial struct Pet
					{
						public static partial Pet Create(string? value);

						public static partial Pet Create(string? value) => new(value);

						readonly object? _value;

						Pet(object? value) => _value = value;

						public object? Value => _value;

						public bool HasValue => _value is not null;

						public bool TryGetValue(out string value)
						{
							if (_value is string text)
							{
								value = text;
								return true;
							}

							value = string.Empty;
							return false;
						}
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

		await Assert.That(generated).Contains("value.TryGetValue(out string caseValue0)");
		await Assert.That(generated).DoesNotContain("string? caseValue");
	}

	[Test]
	public async Task Generate_GivenGeneratedUnionWithAResolvableCaseList_DoesNotFailOnABrokenCaseType(
		CancellationToken cancellationToken
	)
	{
		// Arrange: a typeof argument that names nothing makes the union broken. The unions generator owns that error,
		// so telemetry must degrade to "not a union" rather than fail the build with TSG1000.
		var source =
			GenerateUnionAttribute
			+ CaseTypes
			+ """

				namespace Testing
				{
					[Purview.Unions.GenerateUnion(typeof(DoesNotExist))]
					public partial struct Pet { }

					[ActivitySource("testing")]
					public interface ITestActivities
					{
						[Activity]
						void DoWork([Tag] Pet pet);
					}
				}

				""";

		// Act
		using var generationResult = await GenerateAsync(
			source,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).DoesNotHaveDiagnostic("TSG1000");

		var generated = generationResult.GetSource("Activity.g.cs", HintNameMatchMode.Suffix);

		await Assert.That(generated).DoesNotContain("UnwrapUnion_");
		await Assert.That(generated).Contains("SetTag(\"pet\", pet)");
	}

	[Test]
	public async Task Generate_GivenGeneratedUnionWhoseVisibleMembersLackThePattern_StillReadsTryGetValue(
		CancellationToken cancellationToken
	)
	{
		// Arrange: the stand-in declares HasValue plus a single generic TryGetValue<T>, which the visible-member
		// detection would reject for a two-case union. Reads here prove the accessor is emitted for a [GenerateUnion]
		// target on the strength of what the unions generator will emit, not on what is visible now.
		var source =
			GenerateUnionAttribute
			+ CaseTypes
			+ """

				namespace Testing
				{
					[Purview.Unions.GenerateUnion(typeof(Cat), typeof(Dog))]
					public partial struct Pet
					{
						readonly object? _value;

						Pet(object? value) => _value = value;

						public object? Value => _value;

						public bool HasValue => _value is not null;

						public bool TryGetValue<T>(out T value)
						{
							if (_value is T typed)
							{
								value = typed;
								return true;
							}

							value = default!;
							return false;
						}
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

		await Assert.That(generated).Contains("value.TryGetValue(out global::Testing.Cat caseValue0)");
		await Assert.That(generated).DoesNotContain("return value.Value;");
	}
}
