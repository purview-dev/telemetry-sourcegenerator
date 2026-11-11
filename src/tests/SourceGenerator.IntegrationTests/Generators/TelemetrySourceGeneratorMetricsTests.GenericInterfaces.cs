namespace Purview.Telemetry.SourceGenerator.Metrics;

partial class TelemetrySourceGeneratorMetricsTests
{
	[Test]
	public async Task Generate_GivenGenericMeterInterfaceWithGenericMeasurement_GeneratesAGenericClass(
		CancellationToken cancellationToken
	)
	{
		// Arrange: the measurement is an interface type parameter, so the instrument field can name it.
		const string source = """
			namespace Testing;

			[Meter("testing-meter")]
			public interface ITestMetrics<T> where T : struct
			{
				[Counter]
				void Record(T value);
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		await Assert.That(generationResult).DoesNotHaveDiagnostic("TSG1004");
		var generated = generationResult.GetSource("Testing.TestMetricsCore.Metric.g.cs");
		await Assert
			.That(generated)
			.Contains("class TestMetricsCore<T>")
			.Because("the generated class is generic over the interface's type parameters");
		await Assert
			.That(generated)
			.Contains("Counter<T>")
			.Because("the instrument's measurement type is the interface's type parameter");
	}
}
