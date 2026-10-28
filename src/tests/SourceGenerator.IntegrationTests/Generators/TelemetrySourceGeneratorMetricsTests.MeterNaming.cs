namespace Purview.Telemetry.SourceGenerator.Metrics;

partial class TelemetrySourceGeneratorMetricsTests
{
	[Test]
	public async Task Generate_GivenNameWithInterfacePrefix_GeneratesMetricsWithPrefix(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicMetric = """


namespace Testing;

[Meter("testing-meter", InstrumentPrefix = "This.Is.A.Prefix")]
interface ITestMetrics
{
	[AutoCounter]
	void AutoCounterMetric();
}

""";

		// Act
		using var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod("AutoCounterMetric"))
			.IsTrue()
			.Because("the generated metrics class must contain the auto-counter method");
		await Assert
			.That(generationResult.GetSource("TestMetricsCore.Metric.g.cs"))
			.ContainsGeneratedCode("this.is.a.prefix.auto_counter_metric")
			.Because("the instrument name must be prefixed and lowercased");
	}

	[Test]
	public async Task Generate_GivenNameWithAssemblyPrefix_GeneratesMetricsWithPrefix(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicMetric = """


[assembly: MeterGeneration(InstrumentPrefix = "This.Is.An.Assembly.Prefix")]

namespace Testing;

[Meter("testing-meter")]
interface ITestMetrics
{
	[AutoCounter]
	void AutoCounterMetric();
}

""";

		// Act
		using var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod("AutoCounterMetric"))
			.IsTrue()
			.Because("the generated metrics class must contain the auto-counter method");
		await Assert
			.That(generationResult.GetSource("TestMetricsCore.Metric.g.cs"))
			.ContainsGeneratedCode("this.is.an.assembly.prefix.test_metrics.auto_counter_metric")
			.Because("the instrument name must be prefixed from the assembly and lowercased");
	}

	[Test]
	public async Task Generate_GivenNameWithAssemblyAndInterfacePrefix_GeneratesMetricsWithPrefix(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicMetric = """


[assembly: MeterGeneration(InstrumentPrefix = "This.Is.An.Assembly.Prefix")]

namespace Testing;

[Meter("testing-meter", InstrumentPrefix = "This.Is.A.Prefix")]
interface ITestMetrics
{
	[AutoCounter]
	void AutoCounterMetric();
}

""";

		// Act
		using var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod("AutoCounterMetric"))
			.IsTrue()
			.Because("the generated metrics class must contain the auto-counter method");
		await Assert
			.That(generationResult.GetSource("TestMetricsCore.Metric.g.cs"))
			.ContainsGeneratedCode("this.is.an.assembly.prefix.this.is.a.prefix.auto_counter_metric")
			.Because("the assembly and interface prefixes must be combined");
	}

	[Test]
	public async Task Generate_GivenNameWithAssemblyAndInterfacePrefixAndName_GeneratesMetricsWithPrefix(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicMetric = """


[assembly: MeterGeneration(InstrumentPrefix = "This.Is.An.Assembly.Prefix")]

namespace Testing;

[Meter("testing-meter", InstrumentPrefix = "This.Is.A.Prefix")]
interface ITestMetrics
{
	[AutoCounter("auto-counter")]
	void AutoCounterMetric();
}

""";

		// Act
		using var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod("AutoCounterMetric"))
			.IsTrue()
			.Because("the generated metrics class must contain the auto-counter method");
		await Assert
			.That(generationResult.GetSource("TestMetricsCore.Metric.g.cs"))
			.ContainsGeneratedCode("auto-counter")
			.Because("the explicit instrument name must be used");
	}

	[Test]
	public async Task Generate_GivenNameShouldBeLowerCase_GeneratesMetricsWithLowercaseName(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicMetric = """


namespace Testing;

[Meter("testing-meter")]
interface ITestMetrics
{
	[AutoCounter]
	void AutoCounterMetric();
}

""";

		// Act
		using var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod("AutoCounterMetric"))
			.IsTrue()
			.Because("the generated metrics class must contain the auto-counter method");
		await Assert
			.That(generationResult.GetSource("TestMetricsCore.Metric.g.cs"))
			.ContainsGeneratedCode("test_metrics.auto_counter_metric")
			.Because("the instrument name must be lowercased");
	}

	[Test]
	public async Task Generate_GivenNameShouldBeDefaultLowerCase_GeneratesMetricsWithLowercaseName(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicMetric = """


namespace Testing;

[Meter("testing-meter")]
interface ITestMetrics
{
	[AutoCounter]
	void AutoCounterMetric();
}

""";

		// Act
		using var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod("AutoCounterMetric"))
			.IsTrue()
			.Because("the generated metrics class must contain the auto-counter method");
		await Assert
			.That(generationResult.GetSource("TestMetricsCore.Metric.g.cs"))
			.ContainsGeneratedCode("test_metrics.auto_counter_metric")
			.Because("the default instrument name must be lowercased");
	}

	[Test]
	public async Task Generate_GivenNameShouldBeDefinedCase_GeneratesMetricsWithLowercaseName(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicMetric = """


namespace Testing;

[Meter("testing-meter", LowercaseInstrumentName = false)]
interface ITestMetrics
{
	[AutoCounter]
	void AutoCounterMetric();
}

""";

		// Act
		using var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod("AutoCounterMetric"))
			.IsTrue()
			.Because("the generated metrics class must contain the auto-counter method");
		await Assert
			.That(generationResult.GetSource("TestMetricsCore.Metric.g.cs"))
			.ContainsGeneratedCode("AutoCounterMetric")
			.Because("the instrument name must preserve its defined case");
	}

	[Test]
	public async Task Generate_GivenNoMeterName_UsesAssemblyNameWithOpenTelemetryConvention(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicMetric = """


namespace Testing;

[Meter]
interface ITestMetrics
{
	[AutoCounter]
	void AutoCounterMetric();
}

""";

		// Act
		using var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod("AutoCounterMetric"))
			.IsTrue()
			.Because("the generated metrics class must contain the auto-counter method");
		await Assert
			.That(generationResult.GetSource("TestMetricsCore.Metric"))
			.ContainsGeneratedCode("test_metrics.auto_counter_metric")
			.Because("the instrument name must be generated for the assembly-derived meter name");
	}

	[Test]
	public async Task Generate_GivenAssemblyMeterName_UsesAssemblyMeterName(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicMetric = """


[assembly: MeterGeneration(MeterName = "my.custom.meter")]

namespace Testing;

[Meter]
interface ITestMetrics
{
	[AutoCounter]
	void AutoCounterMetric();
}

""";

		// Act
		using var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod("AutoCounterMetric"))
			.IsTrue()
			.Because("the generated metrics class must contain the auto-counter method");
		await Assert
			.That(generationResult.GetSource("TestMetricsCore.Metric.g.cs"))
			.ContainsGeneratedCode("my.custom.meter")
			.Because("the assembly meter name must be used");
	}

	[Test]
	public async Task Generate_GivenDotNetMeterNameGenerationType_UsesAssemblyNamePreserveCase(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicMetric = """


[assembly: MeterGeneration(MeterNameGenerationType = MeterNameGenerationType.DotNet)]

namespace Testing;

[Meter]
interface ITestMetrics
{
	[AutoCounter]
	void AutoCounterMetric();
}

""";

		// Act
		using var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod("AutoCounterMetric"))
			.IsTrue()
			.Because("the generated metrics class must contain the auto-counter method");
		await Assert
			.That(generationResult.GetSource("TestMetricsCore.Metric.g.cs"))
			.ContainsGeneratedCode("test_metrics.auto_counter_metric")
			.Because("the instrument name must be generated for the DotNet-convention meter name");
	}

	/// <summary>
	/// The interface every naming example in README.md, docs/wiki/Metrics.md and docs/wiki/FAQ.md uses, so the
	/// documented names are pinned by the generator itself.
	/// </summary>
	const string DocumentedProductTelemetry = """
		[Meter("MyApp.Products")]
		interface IProductTelemetry
		{
			[Counter]
			void RecordCount([InstrumentMeasurement] int count);
		}
		""";

	[Test]
	public async Task Generate_GivenTheDocumentedExampleWithDefaults_PrefixesTheInterfaceNameButNotTheMeterName(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = $"""
			namespace Testing;

			{DocumentedProductTelemetry}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		var generated = generationResult.GetSource("ProductTelemetryCore.Metric.g.cs");
		await Assert
			.That(generated)
			.ContainsGeneratedCode("\"product.record_count\"")
			.Because(
				"the default (OpenTelemetry naming, MeterNameGenerationType.DotNet) is the interface-derived prefix and the snake_case method name, without the meter name"
			);
		await Assert
			.That(generated)
			.ContainsGeneratedCode("\"MyApp.Products\"")
			.Because("the meter keeps the name it was given");
		await Assert.That(generated).DoesNotContain("my_app.products.");
	}

	[Test]
	public async Task Generate_GivenTheDocumentedExampleWithOpenTelemetryMeterNames_PrependsTheMeterName(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = $"""
			[assembly: MeterGeneration(MeterNameGenerationType = MeterNameGenerationType.OpenTelemetry)]

			namespace Testing;

			{DocumentedProductTelemetry}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		await Assert
			.That(generationResult.GetSource("ProductTelemetryCore.Metric.g.cs"))
			.ContainsGeneratedCode("\"my_app.products.product.record_count\"")
			.Because(
				"MeterNameGenerationType.OpenTelemetry prepends the snake_cased meter name as the outermost namespace, ahead of the interface prefix"
			);
	}

	[Test]
	public async Task Generate_GivenTheDocumentedExampleWithLegacyNaming_LowercasesWithoutWordBoundaries(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		// Interface-level, because the test options already declare the assembly-level [TelemetryGeneration].
		const string source = $"""
			namespace Testing;

			[TelemetryGeneration(NamingConvention = NamingConvention.Legacy)]
			{DocumentedProductTelemetry}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		await Assert
			.That(generationResult.GetSource("ProductTelemetryCore.Metric.g.cs"))
			.ContainsGeneratedCode("\"product.recordcount\"")
			.Because("the legacy convention lowercases the name and the interface prefix without splitting words");
	}

	[Test]
	public async Task Generate_GivenOpenTelemetryMeterNamesAndSeveralInstruments_PrefixesEachInstrumentOnce(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			[assembly: MeterGeneration(MeterNameGenerationType = MeterNameGenerationType.OpenTelemetry)]

			namespace Testing;

			[Meter("orders")]
			interface IOrderTelemetry
			{
				[AutoCounter]
				void Placed();

				[AutoCounter]
				void Shipped();
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		var generated = generationResult.GetSource("OrderTelemetryCore.Metric.g.cs");
		await Assert.That(generated).ContainsGeneratedCode("\"orders.order.placed\"");
		await Assert
			.That(generated)
			.ContainsGeneratedCode("\"orders.order.shipped\"")
			.Because("the meter name must not accumulate across the interface's instruments");
	}

	[Test]
	public async Task Generate_GivenInterfaceMeterNameOverridesAssemblyDefault(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicMetric = """


[assembly: MeterGeneration(MeterName = "assembly.default")]

namespace Testing;

[Meter("interface.override")]
interface ITestMetrics
{
	[AutoCounter]
	void AutoCounterMetric();
}

""";

		// Act
		using var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod("AutoCounterMetric"))
			.IsTrue()
			.Because("the generated metrics class must contain the auto-counter method");
		await Assert
			.That(generationResult.GetSource("TestMetricsCore.Metric.g.cs"))
			.ContainsGeneratedCode("interface.override")
			.Because("the interface meter name must override the assembly default");
	}
}
