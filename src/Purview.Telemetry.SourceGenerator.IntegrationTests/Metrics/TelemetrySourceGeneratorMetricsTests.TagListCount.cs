namespace Purview.Telemetry.SourceGenerator.Metrics;

partial class TelemetrySourceGeneratorMetricsTests
{
	[Test]
	public async Task Generate_ValidatingUsageOfTagListBasedOnTagCount_GeneratesTagListOrNot(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicMetric = """
namespace Testing;

[Meter("testing-meter")]
public interface ITestMetrics
{
	// No TagList
	[AutoCounter]
	void NoTagList0();

	[AutoCounter]
	void NoTagList1(int param1);

	[AutoCounter]
	void NoTagList2(int param1, int param2);

	[AutoCounter]
	void NoTagList3(int param1, int param2, int param3);

	// Has TagList
	[AutoCounter]
	void HasTagList4(int param1, int param2, int param3, int param4);

	[AutoCounter]
	void HasTagList5(int param1, int param2, int param3, int param4, int param5);

	[AutoCounter]
	void HasTagList10(int param1, int param2, int param3, int param4, int param5, int param6, int param7, int param8, int param9, int param10);
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
