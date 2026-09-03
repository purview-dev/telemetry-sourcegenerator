using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator.Metrics;

[SkipOnNetFramework]
public partial class TelemetrySourceGeneratorMetricsTests : IncrementalSourceGeneratorTestBase<TelemetrySourceGenerator>
{
	[Test]
	[MethodDataSource<TelemetrySourceGeneratorTests>(nameof(TelemetrySourceGeneratorTests.BasicGenericParameters))]
	public async Task Generate_GivenMethodWithBasicGenericParams_GeneratesEntryCorrectly(
		string parameterType,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var basicActivity = $$"""


namespace Testing;

[Meter("testing-meter")]
public interface ITestMetrics
{
	[AutoCounter]
	void AutoCounter({{parameterType}} genericParameter);

	[Counter(AutoIncrement = true)]
	void Counter_AutoIncrement({{parameterType}} genericParameter);

	[Counter]
	void Counter([InstrumentMeasurement]int value, {{parameterType}} genericParameter);

	[Histogram]
	void Histogram([InstrumentMeasurement]int value, {{parameterType}} genericParameter);

	[UpDownCounter]
	void UpDownCounter([InstrumentMeasurement]int value, {{parameterType}} genericParameter);

	[ObservableCounter]
	void ObservableCounter(Func<int> valueFunc, {{parameterType}} genericParameter);

	[ObservableGauge]
	void ObservableGauge(Func<int> valueFunc, {{parameterType}} genericParameter);

	[ObservableUpDownCounter]
	void ObservableUpDownCounter(Func<int> valueFunc, {{parameterType}} genericParameter);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicActivity, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod(query, "AutoCounter"))
			.IsTrue()
			.Because("the generated metrics class must contain the auto-counter method");
		await Assert
			.That(metricsClass.HasMethod(query, "Counter_AutoIncrement"))
			.IsTrue()
			.Because("the generated metrics class must contain the auto-increment counter method");
		await Assert
			.That(metricsClass.HasMethod(query, "Counter"))
			.IsTrue()
			.Because("the generated metrics class must contain the counter method");
		await Assert
			.That(metricsClass.HasMethod(query, "Histogram"))
			.IsTrue()
			.Because("the generated metrics class must contain the histogram method");
		await Assert
			.That(metricsClass.HasMethod(query, "UpDownCounter"))
			.IsTrue()
			.Because("the generated metrics class must contain the up-down counter method");
		await Assert
			.That(metricsClass.HasMethod(query, "ObservableCounter"))
			.IsTrue()
			.Because("the generated metrics class must contain the observable counter method");
		await Assert
			.That(metricsClass.HasMethod(query, "ObservableGauge"))
			.IsTrue()
			.Because("the generated metrics class must contain the observable gauge method");
		await Assert
			.That(metricsClass.HasMethod(query, "ObservableUpDownCounter"))
			.IsTrue()
			.Because("the generated metrics class must contain the observable up-down counter method");
	}

	[Test]
	[MethodDataSource<TelemetrySourceGeneratorTests>(nameof(TelemetrySourceGeneratorTests.GetGenericTypeDefCount))]
	public async Task Generate_GivenInterfaceWithGenerics_RaisesDiagnostics(
		int genericTypeCount,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var genericTypeDef = string.Join(", ", Enumerable.Range(0, genericTypeCount).Select(i => $"T{i}"));
		var basicMeter = $$"""


namespace Testing;

[Meter("testing-meter")]
public interface ITestMetrics<{{genericTypeDef}}>  {
	[AutoCounter]
	void AutoCounter();
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicMeter,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG1004");
	}

	[Test]
	[MethodDataSource<TelemetrySourceGeneratorTests>(nameof(TelemetrySourceGeneratorTests.GetGenericTypeDefCount))]
	public async Task Generate_GivenMethodWithGenerics_RaisesDiagnostics(
		int genericTypeCount,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var genericTypeDef = string.Join(", ", Enumerable.Range(0, genericTypeCount).Select(i => $"T{i}"));
		var basicMeter = $$"""


namespace Testing;

[Meter("testing-meter")]
public interface ITestMetrics<{{genericTypeDef}}>  {
	[AutoCounter]
	void AutoCounter();
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicMeter,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG1004");
	}
}
