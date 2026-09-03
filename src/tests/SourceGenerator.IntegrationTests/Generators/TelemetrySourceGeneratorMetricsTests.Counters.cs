using Purview.SourceGeneratorFramework;
using Purview.Telemetry.SourceGenerator.Infra;

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
		var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod(query, "AutoCounter", TypeReference.Create<int>()))
			.IsTrue()
			.Because("the generated metrics class must contain the auto-counter method");
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
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG4002");
	}

	[Test]
	public async Task Generate_GivenBasicAutoCounter_GeneratesMetrics(CancellationToken cancellationToken)
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
		var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(
				metricsClass.HasMethod(query, "AutoCounter", TypeReference.Create<int>(), TypeReference.Create<bool>())
			)
			.IsTrue()
			.Because("the generated metrics class must contain the auto-counter method");
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
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG4002");
	}

	[Test]
	public async Task Generate_GivenBasicCounters_GeneratesMetrics(CancellationToken cancellationToken)
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
		var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		string[] counterMethods = ["Counter", "Counter2", "Counter3", "Counter4", "Counter5", "Counter6", "Counter7"];
		foreach (var methodName in counterMethods)
		{
			await Assert
				.That(metricsClass.HasMethod(query, methodName))
				.IsTrue()
				.Because($"the generated metrics class must contain the {methodName} method");
		}
	}

	[Test]
	public async Task Generate_GivenBasicCountersWithAutoIncrement_GeneratesMetrics(CancellationToken cancellationToken)
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
		var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		string[] counterMethods = ["Counter1", "Counter2", "Counter3"];
		foreach (var methodName in counterMethods)
		{
			await Assert
				.That(metricsClass.HasMethod(query, methodName))
				.IsTrue()
				.Because($"the generated metrics class must contain the {methodName} method");
		}
	}

	[Test]
	public async Task Generate_GivenBasicObservableCounters_GeneratesMetrics(CancellationToken cancellationToken)
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
		var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod(query, "ObservableCounter"))
			.IsTrue()
			.Because("the generated metrics class must contain the observable counter method");
		await Assert
			.That(metricsClass.HasMethod(query, "ObservableCounter2"))
			.IsTrue()
			.Because("the generated metrics class must contain the second observable counter method");
		await Assert
			.That(metricsClass.HasMethod(query, "ObservableCounter3"))
			.IsTrue()
			.Because("the generated metrics class must contain the third observable counter method");
	}
}
