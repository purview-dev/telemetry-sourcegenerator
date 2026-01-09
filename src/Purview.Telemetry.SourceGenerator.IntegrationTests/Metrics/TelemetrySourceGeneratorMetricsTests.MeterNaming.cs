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
		var generationResult = await GenerateAsync(
			basicMetric,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
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
		var generationResult = await GenerateAsync(
			basicMetric,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
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
		var generationResult = await GenerateAsync(
			basicMetric,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
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
		var generationResult = await GenerateAsync(
			basicMetric,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
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
		var generationResult = await GenerateAsync(
			basicMetric,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
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
		var generationResult = await GenerateAsync(
			basicMetric,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
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
		var generationResult = await GenerateAsync(
			basicMetric,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}
}
