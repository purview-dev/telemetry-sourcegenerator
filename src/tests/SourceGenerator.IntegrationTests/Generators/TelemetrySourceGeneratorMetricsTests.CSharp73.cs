using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator.Metrics;

partial class TelemetrySourceGeneratorMetricsTests
{
	[Test]
	public async Task Generate_GivenBasicGen_GeneratesMetrics_WithCSharp73LanguageVersion(
		CancellationToken cancellationToken
	)
	{
		// Arrange: C# 7.3-compatible interface — no nullable reference annotations,
		// no file-scoped namespace.
		// Exercises Counter, Histogram, and AutoCounter instruments whose emitted bodies
		// contain KeyValuePair<string, object?> which must become KeyValuePair<string, object>
		// under C# 7.3, and initialization with = default! which must become = default.
		const string basicMetrics =
			@"

namespace Testing {

[Meter(""testing-meter"")]
public interface ITestMetrics {
	[AutoCounter]
	void AutoCounter(string stringParam);

	[Counter]
	void Counter([InstrumentMeasurement]int value, string stringParam);

	[Histogram]
	void Histogram([InstrumentMeasurement]int value, string stringParam);

	[UpDownCounter]
	void UpDownCounter([InstrumentMeasurement]int value, string stringParam);
}

}
";

		// Act
		var generationResult = await GenerateAsync(basicMetrics, cancellationToken: cancellationToken);

		// Assert: GenerateAsync's EnsureValid (default) verifies generated code compiles under C# 7.3.
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}
}
