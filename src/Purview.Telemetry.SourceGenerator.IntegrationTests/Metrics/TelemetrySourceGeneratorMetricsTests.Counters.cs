namespace Purview.Telemetry.SourceGenerator.Metrics;

partial class TelemetrySourceGeneratorMetricsTests
{
	[Fact]
	public async Task Generate_GivenBasicAutoCounterWithInferredTagsOfInstrumentType_GeneratesMetrics()
	{
		// Arrange
		const string basicMetric =
			@"
using Purview.Telemetry.Metrics;

namespace Testing;

[Meter(""testing-meter"")]
public interface ITestMetrics 
{
	[AutoCounter]
	void AutoCounter(int intParam);
}
";

		// Act
		var generationResult = await GenerateAsync(basicMetric);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Fact]
	public async Task Generate_GivenBasicAutoCounterWithSpecifiedInstrumentMeasurement_GeneratesDiagnostic()
	{
		// Arrange
		const string basicMetric =
			@"
using Purview.Telemetry.Metrics;

namespace Testing;

[Meter(""testing-meter"")]
public interface ITestMetrics 
{
	[AutoCounter]
	void AutoCounter([InstrumentMeasurement]int intParam);
}
";

		// Act
		var generationResult = await GenerateAsync(basicMetric);

		// Assert
		await TestHelpers.Verify(
			generationResult,
			config: c => c.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG4002"],
			validationCompilation: false
		);
	}

	[Fact]
	public async Task Generate_GivenBasicAutoCounter_GeneratesMetrics()
	{
		// Arrange
		const string basicMetric =
			@"
using Purview.Telemetry.Metrics;

namespace Testing;

[Meter(""testing-meter"")]
public interface ITestMetrics 
{
	[AutoCounter]
	void AutoCounter([Tag]int intParam, [Tag]bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(basicMetric);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Fact]
	public async Task Generate_GivenAutoCounterWithInstrumentationValue_GeneratesDiagnostic()
	{
		// Arrange
		const string basicMetric =
			@"
using Purview.Telemetry.Metrics;

namespace Testing;

[Meter(""testing-meter"")]
public interface ITestMetrics 
{
	[AutoCounter]
	void AutoCounter([InstrumentMeasurement]int value, [Tag]int intParam, [Tag]bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(basicMetric);

		// Assert
		await TestHelpers.Verify(
			generationResult,
			c => c.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG4002"],
			validationCompilation: false
		);
	}

	[Fact]
	public async Task Generate_GivenBasicCounters_GeneratesMetrics()
	{
		// Arrange
		const string basicMetric =
			@"
using Purview.Telemetry.Metrics;

namespace Testing;

[Meter(""testing-meter"")]
public interface ITestMetrics {
	[Counter]
	void Counter(int counterValue, [Tag]int intParam, [Tag]bool boolParam);

	[Counter]
	void Counter2(byte counterValue, [Tag]int intParam, [Tag]bool boolParam);

	[Counter]
	void Counter3(long counterValue, [Tag]int intParam, [Tag]bool boolParam);

	[Counter]
	void Counter4([InstrumentMeasurement]short counterValue, [Tag]int intParam, [Tag]bool boolParam);

	[Counter]
	void Counter5([InstrumentMeasurement]double counterValue, [Tag]int intParam, [Tag]bool boolParam);

	[Counter]
	void Counter6([InstrumentMeasurement]float counterValue, [Tag]int intParam, [Tag]bool boolParam);

	[Counter]
	void Counter7([InstrumentMeasurement]decimal counterValue, [Tag]int intParam, [Tag]bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(basicMetric);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Fact]
	public async Task Generate_GivenBasicCountersWithAutoIncrement_GeneratesMetrics()
	{
		// Arrange
		const string basicMetric =
			@"
using Purview.Telemetry.Metrics;

namespace Testing;

[Meter(""testing-meter"")]
public interface ITestMetrics {
	[Counter(autoIncrement: true)]
	void Counter1([Tag]int intParam, [Tag]bool boolParam);

	[Counter(AutoIncrement = true)]
	void Counter2([Tag]int intParam, [Tag]bool boolParam);

	[Counter(true)]
	void Counter3([Tag]int intParam, [Tag]bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(basicMetric);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Fact]
	public async Task Generate_GivenBasicObservableCounters_GeneratesMetrics()
	{
		// Arrange
		const string basicMetric =
			@"
using Purview.Telemetry.Metrics;
using System.Diagnostics.Metrics;
using System.Collections.Generic;

namespace Testing;

[Meter(""testing-observable-meter"")]
public interface ITestMetrics {
	[ObservableCounter]
	void ObservableCounter(Func<int> f, [Tag]int intParam, [Tag]bool boolParam);

	[ObservableCounter(ThrowOnAlreadyInitialized = true)]
	void ObservableCounter2(Func<Measurement<int>> f, [Tag]int intParam, [Tag]bool boolParam);

	[ObservableCounter]
	void ObservableCounter3(Func<IEnumerable<Measurement<int>>> f, [Tag]int intParam, [Tag]bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(basicMetric);

		// Assert
		await TestHelpers.Verify(generationResult);
	}
}
