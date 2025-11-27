namespace Purview.Telemetry.SourceGenerator.Metrics;

partial class TelemetrySourceGeneratorMetricsTests
{
	[Test]
	public async Task Generate_GivenNameWithInterfacePrefix_GeneratesMetricsWithPrefix()
	{
		// Arrange
		var basicMetric =
			@$"
using Purview.Telemetry.Metrics;

namespace Testing;

[Meter(""testing-meter"", InstrumentPrefix = ""This.Is.A.Prefix"")]
interface ITestMetrics 
{{
	[AutoCounter]
	void AutoCounterMetric();
}}
";

		// Act
		var generationResult = await GenerateAsync(basicMetric);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenNameWithAssemblyPrefix_GeneratesMetricsWithPrefix()
	{
		// Arrange
		var basicMetric =
			@$"
using Purview.Telemetry.Metrics;

[assembly: MeterGeneration(InstrumentPrefix = ""This.Is.An.Assembly.Prefix"")]

namespace Testing;

[Meter(""testing-meter"")]
interface ITestMetrics 
{{
	[AutoCounter]
	void AutoCounterMetric();
}}
";

		// Act
		var generationResult = await GenerateAsync(basicMetric);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenNameWithAssemblyAndInterfacePrefix_GeneratesMetricsWithPrefix()
	{
		// Arrange
		var basicMetric =
			@$"
using Purview.Telemetry.Metrics;

[assembly: MeterGeneration(InstrumentPrefix = ""This.Is.An.Assembly.Prefix"")]

namespace Testing;

[Meter(""testing-meter"", InstrumentPrefix = ""This.Is.A.Prefix"")]
interface ITestMetrics 
{{
	[AutoCounter]
	void AutoCounterMetric();
}}
";

		// Act
		var generationResult = await GenerateAsync(basicMetric);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenNameWithAssemblyAndInterfacePrefixAndName_GeneratesMetricsWithPrefix()
	{
		// Arrange
		var basicMetric =
			@$"
using Purview.Telemetry.Metrics;

[assembly: MeterGeneration(InstrumentPrefix = ""This.Is.An.Assembly.Prefix"")]

namespace Testing;

[Meter(""testing-meter"", InstrumentPrefix = ""This.Is.A.Prefix"")]
interface ITestMetrics 
{{
	[AutoCounter(""auto-counter"")]
	void AutoCounterMetric();
}}
";

		// Act
		var generationResult = await GenerateAsync(basicMetric);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenNameShouldBeLowerCase_GeneratesMetricsWithLowercaseName()
	{
		// Arrange
		var basicMetric =
			@$"
using Purview.Telemetry.Metrics;

namespace Testing;

[Meter(""testing-meter"")]
interface ITestMetrics 
{{
	[AutoCounter]
	void AutoCounterMetric();
}}
";

		// Act
		var generationResult = await GenerateAsync(basicMetric);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenNameShouldBeDefaultLowerCase_GeneratesMetricsWithLowercaseName()
	{
		// Arrange
		var basicMetric =
			@$"
using Purview.Telemetry.Metrics;

namespace Testing;

[Meter(""testing-meter"")]
interface ITestMetrics 
{{
	[AutoCounter]
	void AutoCounterMetric();
}}
";

		// Act
		var generationResult = await GenerateAsync(basicMetric);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenNameShouldBeDefinedCase_GeneratesMetricsWithLowercaseName()
	{
		// Arrange
		var basicMetric =
			@$"
using Purview.Telemetry.Metrics;

namespace Testing;

[Meter(""testing-meter"", LowercaseInstrumentName = false)]
interface ITestMetrics
{{
	[AutoCounter]
	void AutoCounterMetric();
}}
";

		// Act
		var generationResult = await GenerateAsync(basicMetric);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}
}
