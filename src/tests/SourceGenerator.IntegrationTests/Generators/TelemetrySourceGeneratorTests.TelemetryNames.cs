using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator;

partial class TelemetrySourceGeneratorTests
{
	const string TelemetryNamesSource = """
		using Purview.Telemetry;

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
		""";

	const string AttributeHintName = "TestAssembly.GeneratedTelemetryNames.g.cs";
	const string ClassHintName = "TestAssembly.TelemetryNames.g.cs";

	/// <summary>
	/// The default options apply an assembly-level <c>[TelemetryGeneration]</c> to keep the
	/// dependency-injection extension off; it is replaced wholesale here because the attribute does
	/// not allow multiples.
	/// </summary>
	static TelemetrySourceGeneratorTestOptions TelemetryNamesOptions(string arguments) =>
		new()
		{
			AdditionalSources =
			[
				$"[assembly: Purview.Telemetry.TelemetryGeneration(GenerateDependencyExtension = false, {arguments})]",
			],
		};

	[Test]
	public async Task Generate_GivenTelemetryTargets_RecordsNamesInTheAssemblyAttribute(
		CancellationToken cancellationToken
	)
	{
		// Act
		var generationResult = await GenerateAsync(TelemetryNamesSource, cancellationToken: cancellationToken);

		// Assert
		var attributeSource = generationResult.GetSource(AttributeHintName);

		await Assert
			.That(attributeSource)
			.ContainsGeneratedCode("[assembly: global::Purview.Telemetry.GeneratedTelemetryNamesAttribute(")
			.Because("the names must be recorded in metadata for downstream assemblies to aggregate");
		await Assert.That(attributeSource).ContainsGeneratedCode("\"testing-activity-source\"");
		await Assert.That(attributeSource).ContainsGeneratedCode("\"testing-meter\"");
	}

	[Test]
	public async Task Generate_GivenNoActivitySourceOrMeter_DoesNotRecordTheAssemblyAttribute(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			using Purview.Telemetry;

			namespace Testing;

			[Logger]
			public interface ITestLogger {
				void Log(int value);
			}
			""";

		// Act
		var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		var hintNames = generationResult
			.DriverResult.Results.SelectMany(result => result.GeneratedSources)
			.Select(generated => generated.HintName);

		await Assert
			.That(hintNames)
			.DoesNotContain(AttributeHintName)
			.Because("logging produces neither an activity source nor a meter name");
	}

	[Test]
	public async Task Generate_GivenGenerateTelemetryNamesAttributeFalse_DoesNotRecordTheAssemblyAttribute(
		CancellationToken cancellationToken
	)
	{
		// Act
		var generationResult = await GenerateAsync(
			TelemetryNamesSource,
			TelemetryNamesOptions("GenerateTelemetryNamesAttribute = false"),
			cancellationToken: cancellationToken
		);

		// Assert
		var hintNames = generationResult
			.DriverResult.Results.SelectMany(result => result.GeneratedSources)
			.Select(generated => generated.HintName);

		await Assert
			.That(hintNames)
			.DoesNotContain(AttributeHintName)
			.Because("the attribute is emitted by default, but must be suppressible");
	}

	[Test]
	public async Task Generate_GivenDefaults_DoesNotGenerateTheTelemetryNamesClass(CancellationToken cancellationToken)
	{
		// Act
		var generationResult = await GenerateAsync(TelemetryNamesSource, cancellationToken: cancellationToken);

		// Assert
		await Assert
			.That(generationResult.Generated().HasClass("TelemetryNames", "TestAssembly"))
			.IsFalse()
			.Because("the class is opt-in; the assembly attribute replaces it by default");
	}

	[Test]
	public async Task Generate_GivenGenerateTelemetryNamesClass_GeneratesTheClass(CancellationToken cancellationToken)
	{
		// Act
		var generationResult = await GenerateAsync(
			TelemetryNamesSource,
			TelemetryNamesOptions("GenerateTelemetryNamesClass = true"),
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult.Generated().HasClass("TelemetryNames", "TestAssembly")).IsTrue();

		var classSource = generationResult.GetSource(ClassHintName);
		await Assert.That(classSource).ContainsGeneratedCode("\"testing-activity-source\"");
		await Assert.That(classSource).ContainsGeneratedCode("\"testing-meter\"");
	}

	[Test]
	public async Task Generate_GivenReferencedAssemblyNames_AggregatesThemIntoTheClass(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var options = TelemetryNamesOptions("GenerateTelemetryNamesClass = true") with
		{
			AdditionalReferences =
			[
				TelemetryNamesReference.Create(
					"Referenced.Telemetry",
					["referenced-activity-source"],
					["referenced-meter"]
				),
			],
		};

		// Act
		var generationResult = await GenerateAsync(TelemetryNamesSource, options, cancellationToken: cancellationToken);

		// Assert
		var classSource = generationResult.GetSource(ClassHintName);

		await Assert.That(classSource).ContainsGeneratedCode("\"testing-activity-source\"");
		await Assert.That(classSource).ContainsGeneratedCode("\"testing-meter\"");
		await Assert
			.That(classSource)
			.ContainsGeneratedCode("\"referenced-activity-source\"")
			.Because("the names recorded by a referenced assembly must be aggregated");
		await Assert.That(classSource).ContainsGeneratedCode("\"referenced-meter\"");
	}

	[Test]
	public async Task Generate_GivenNoTargetsOfItsOwn_AggregatesReferencedAssemblyNamesIntoTheClass(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		// The opt-in lives on the assembly alone: a host project that only wires up the telemetry its
		// references generate has no interface of its own to carry [TelemetryGeneration].
		const string noTelemetry = "namespace Testing;";
		var options = TelemetryNamesOptions("GenerateTelemetryNamesClass = true") with
		{
			AdditionalReferences =
			[
				TelemetryNamesReference.Create(
					"Referenced.Telemetry",
					["referenced-activity-source"],
					["referenced-meter"]
				),
			],
		};

		// Act
		var generationResult = await GenerateAsync(noTelemetry, options, cancellationToken: cancellationToken);

		// Assert
		await Assert
			.That(generationResult.Generated().HasClass("TelemetryNames", "TestAssembly"))
			.IsTrue()
			.Because("the assembly-level opt-in must apply without a local activity source or meter");

		var classSource = generationResult.GetSource(ClassHintName);
		await Assert.That(classSource).ContainsGeneratedCode("\"referenced-activity-source\"");
		await Assert.That(classSource).ContainsGeneratedCode("\"referenced-meter\"");
	}

	[Test]
	public async Task Generate_GivenNoTargetsOfItsOwnAndNoReferencedNames_DoesNotGenerateTheClass(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string noTelemetry = "namespace Testing;";

		// Act
		var generationResult = await GenerateAsync(
			noTelemetry,
			TelemetryNamesOptions("GenerateTelemetryNamesClass = true"),
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert
			.That(generationResult.Generated().HasClass("TelemetryNames", "TestAssembly"))
			.IsFalse()
			.Because("there is nothing to name: neither this assembly nor its references generated any");
	}

	[Test]
	public async Task Generate_GivenAggregationDisabled_ExcludesReferencedAssemblyNames(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var options = TelemetryNamesOptions(
			"GenerateTelemetryNamesClass = true, AggregateReferencedTelemetryNames = false"
		) with
		{
			AdditionalReferences =
			[
				TelemetryNamesReference.Create(
					"Referenced.Telemetry",
					["referenced-activity-source"],
					["referenced-meter"]
				),
			],
		};

		// Act
		var generationResult = await GenerateAsync(TelemetryNamesSource, options, cancellationToken: cancellationToken);

		// Assert
		var classSource = generationResult.GetSource(ClassHintName);

		await Assert.That(classSource).ContainsGeneratedCode("\"testing-activity-source\"");
		await Assert.That(classSource).DoesNotContain("referenced-activity-source");
		await Assert.That(classSource).DoesNotContain("referenced-meter");
	}
}
