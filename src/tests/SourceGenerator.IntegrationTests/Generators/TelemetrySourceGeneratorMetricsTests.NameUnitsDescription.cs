namespace Purview.Telemetry.SourceGenerator.Metrics;

partial class TelemetrySourceGeneratorMetricsTests
{
	[Test]
	[MethodDataSource(nameof(NameUnitsDescriptorData))]
	public async Task Generate_GivenNameUnitsDescription_GeneratesMetrics(
		string attribute,
		string measurementParameter,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var basicMetric = $$"""

using System.Diagnostics.Metrics;
using System.Collections.Generic;

namespace Testing;

[Meter("testing-meter")]
public interface ITestMetrics {
	[{{attribute}}]
	void Metric({{measurementParameter}}[Tag]int intParam, [Tag]bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicMetric, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var metricsClass = query.GetClass("TestMetricsCore", "Testing");
		await Assert
			.That(metricsClass.HasMethod(query, "Metric"))
			.IsTrue()
			.Because("the generated metrics class must contain the instrument method");

		// The instrument name is always the first quoted value in the attribute application.
		var instrumentName = attribute.Split('"')[1];
		await Assert
			.That(generationResult.GetSource("TestMetricsCore.Metric.g.cs"))
			.ContainsGeneratedCode(instrumentName)
			.Because("the instrument must carry the configured name");
	}

	public static IEnumerable<(string, string)> NameUnitsDescriptorData
	{
		get
		{
			List<(string, string)> data =
			[
				(
					"AutoCounter(name: \"a-counter-name-param\", unit: \"cakes-param\", description: \"cake sales per-capita-param.\")",
					""
				),
				(
					"AutoCounter(Name = \"a-counter-name-property\", Unit = \"cakes-property\", Description = \"cake sales per-capita-property.\")",
					""
				),
				(
					"Counter(name: \"a-counter-name-param\", unit: \"cakes-param\", description: \"cake sales per-capita-param.\")",
					"int counterValue, "
				),
				(
					"Counter(Name = \"a-counter-name-property\", Unit = \"cakes-property\", Description = \"cake sales per-capita-property.\")",
					"byte counterValue, "
				),
				(
					"UpDownCounter(name: \"an-updown-counter-name-param\", unit: \"sponges-param\", description: \"sponge sales per-capita-param.\")",
					"int counterValue, "
				),
				(
					"UpDownCounter(Name = \"an-updown-counter-name-property\", Unit = \"sponges-property\", Description = \"sponge sales per-capita-property.\")",
					"byte counterValue, "
				),
				(
					"ObservableCounter(name: \"an-observablecounter-name-param\", unit: \"pie-param\", description: \"pie sales per-capita-param.\")",
					"Func<int> f, "
				),
				(
					"ObservableCounter(Name = \"an-observablecounter-name-property\", Unit = \"pie-property\", Description = \"pie sales per-capita-property.\")",
					"Func<byte> f, "
				),
				(
					"ObservableGauge(name: \"an-observablegauge-name-param\", unit: \"biscuits-param\", description: \"biscuit ake sales per-capita-param.\")",
					"Func<Measurement<int>> f, "
				),
				(
					"ObservableGauge(Name = \"an-observablegauge-name-property\", Unit = \"biscuits-property\", Description = \"biscuit sales per-capita-property.\")",
					"Func<Measurement<byte>> f, "
				),
				(
					"ObservableUpDownCounter(name: \"an-observableupdowncounter-name-param\", unit: \"beer-param\", description: \"beer sales per-capita-param.\")",
					"Func<IEnumerable<Measurement<int>>> f, "
				),
				(
					"ObservableUpDownCounter(Name = \"an-observableupdowncounter-name-property\", Unit = \"beer-property\", Description = \"beer sales per-capita-property.\")",
					"Func<IEnumerable<Measurement<byte>>> f, "
				),
			];

			return data;
		}
	}
}
