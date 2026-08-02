namespace Purview.Telemetry.SourceGenerator.Metrics;

partial class TelemetrySourceGeneratorMetricsTests
{
	[Test]
	public async Task Generate_GivenBasicAutoCounterWithInferredTagsOfInstrumentType_GeneratesMetrics(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicMetric = """


namespace Testing;

[Meter("testing-meter")]
public interface ITestMetrics
{
	[AutoCounter]
	void AutoCounter(int intParam);
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
	public async Task Generate_GivenBasicAutoCounterWithSpecifiedInstrumentMeasurement_GeneratesDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicMetric = """


namespace Testing;

[Meter("testing-meter")]
public interface ITestMetrics
{
	[AutoCounter]
	void AutoCounter([InstrumentMeasurement]int intParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicMetric,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			config: c => c.ScrubInlineGuids(),
			expectsDiagnostics: true,
			validationCompilation: false,
			expectedDiagnosticCodes: ["TSG4002"],
			cancellationToken: cancellationToken
		);
	}

	[Test]
	public async Task Generate_GivenBasicAutoCounter_GeneratesMetrics(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicMetric = """


namespace Testing;

[Meter("testing-meter")]
public interface ITestMetrics
{
	[AutoCounter]
	void AutoCounter([Tag]int intParam, [Tag]bool boolParam);
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
	public async Task Generate_GivenAutoCounterWithInstrumentationValue_GeneratesDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicMetric = """


namespace Testing;

[Meter("testing-meter")]
public interface ITestMetrics
{
	[AutoCounter]
	void AutoCounter([InstrumentMeasurement]int value, [Tag]int intParam, [Tag]bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicMetric,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			expectsDiagnostics: true,
			validationCompilation: false,
			expectedDiagnosticCodes: ["TSG4002"],
			cancellationToken: cancellationToken
		);
	}

	[Test]
	public async Task Generate_GivenBasicCounters_GeneratesMetrics(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicMetric = """


namespace Testing;

[Meter("testing-meter")]
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
	public async Task Generate_GivenBasicCountersWithAutoIncrement_GeneratesMetrics(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicMetric = """


namespace Testing;

[Meter("testing-meter")]
public interface ITestMetrics {
	[Counter(autoIncrement: true)]
	void Counter1([Tag]int intParam, [Tag]bool boolParam);

	[Counter(AutoIncrement = true)]
	void Counter2([Tag]int intParam, [Tag]bool boolParam);

	[Counter(true)]
	void Counter3([Tag]int intParam, [Tag]bool boolParam);
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
	public async Task Generate_GivenBasicObservableCounters_GeneratesMetrics(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicMetric = """

using System.Diagnostics.Metrics;
using System.Collections.Generic;

namespace Testing;

[Meter("testing-observable-meter")]
public interface ITestMetrics {
	[ObservableCounter]
	void ObservableCounter(Func<int> f, [Tag]int intParam, [Tag]bool boolParam);

	[ObservableCounter(ThrowOnAlreadyInitialized = true)]
	void ObservableCounter2(Func<Measurement<int>> f, [Tag]int intParam, [Tag]bool boolParam);

	[ObservableCounter]
	void ObservableCounter3(Func<IEnumerable<Measurement<int>>> f, [Tag]int intParam, [Tag]bool boolParam);
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
