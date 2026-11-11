using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator.Metrics;

partial class TelemetrySourceGeneratorMetricsTests
{
	[Test]
	public async Task Generate_GivenGenericMetricMethodWithConcreteMeasurement_MirrorsTheTypeParameter(
		CancellationToken cancellationToken
	)
	{
		// Arrange: the measurement is concrete, so only the tag parameter is generic.
		const string source = """
			namespace Testing;

			[Meter]
			public interface ITestMetrics
			{
				[Counter]
				void Record<T>(int value, T tag);
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		await Assert.That(generationResult).DoesNotHaveDiagnostic("TSG1005");
		await Assert
			.That(generationResult.GetSource("Testing.TestMetricsCore.Metric.g.cs"))
			.Contains("void Record<T>(int value, T tag)")
			.Because("the generated instrument method mirrors the interface's type parameter");
	}

	[Test]
	public async Task Generate_GivenGenericMeasurementTypeParameter_RaisesTSG1005(CancellationToken cancellationToken)
	{
		// Arrange: an instrument is a field, and a field cannot name a method's type parameter.
		const string source = """
			namespace Testing;

			[Meter]
			public interface ITestMetrics
			{
				[Counter]
				void Record<T>(T value) where T : struct;
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(
			source,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG1005");
		await Assert
			.That(generationResult.GetSource("Testing.TestMetricsCore.Metric.g.cs"))
			.Contains("throw new global::System.NotSupportedException()")
			.Because("the method is emitted as a stub so the interface stays implemented");
	}
}
