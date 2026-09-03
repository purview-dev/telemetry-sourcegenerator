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
		var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod(query, "AutoCounterMetric"))
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
		var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod(query, "AutoCounterMetric"))
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
		var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod(query, "AutoCounterMetric"))
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
		var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod(query, "AutoCounterMetric"))
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
		var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod(query, "AutoCounterMetric"))
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
		var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod(query, "AutoCounterMetric"))
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
		var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod(query, "AutoCounterMetric"))
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
		var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod(query, "AutoCounterMetric"))
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
		var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod(query, "AutoCounterMetric"))
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
		var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod(query, "AutoCounterMetric"))
			.IsTrue()
			.Because("the generated metrics class must contain the auto-counter method");
		await Assert
			.That(generationResult.GetSource("TestMetricsCore.Metric.g.cs"))
			.ContainsGeneratedCode("test_metrics.auto_counter_metric")
			.Because("the instrument name must be generated for the DotNet-convention meter name");
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
		var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod(query, "AutoCounterMetric"))
			.IsTrue()
			.Because("the generated metrics class must contain the auto-counter method");
		await Assert
			.That(generationResult.GetSource("TestMetricsCore.Metric.g.cs"))
			.ContainsGeneratedCode("interface.override")
			.Because("the interface meter name must override the assembly default");
	}
}
