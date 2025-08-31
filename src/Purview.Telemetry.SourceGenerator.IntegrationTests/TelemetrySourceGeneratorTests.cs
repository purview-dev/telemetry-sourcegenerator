namespace Purview.Telemetry.SourceGenerator;

public partial class TelemetrySourceGeneratorTests(ITestOutputHelper testOutputHelper)
	: IncrementalSourceGeneratorTestBase<TelemetrySourceGenerator>(testOutputHelper)
{
	[Fact(DisplayName = "Tests the generated attributes are included and compile")]
	public async Task Generate_GivenGeneratedAttributes_GeneratesAsExpected()
	{
		// Arrange
		const string empty =
			@"
using Purview.Telemetry.Logging;

namespace Testing;

";

		// Act
		var generationResult = await GenerateAsync(empty);

		// Assert
		await TestHelpers.Verify(generationResult, autoVerifyTemplates: false);
	}

	public static TheoryData<string> BasicGenericParameters
	{
		get
		{
			TheoryData<string> parameter = [];

			parameter.Add(
				TestHelpers.GetFriendlyTypeName(typeof(List<>).MakeGenericType(typeof(string)))
			);
			parameter.Add(
				TestHelpers.GetFriendlyTypeName(
					typeof(IEnumerable<>).MakeGenericType(typeof(string)),
					useSystemType: false
				)
			);
			parameter.Add(
				TestHelpers.GetFriendlyTypeName(
					typeof(Dictionary<,>).MakeGenericType(typeof(string), typeof(int))
				)
			);
			parameter.Add(
				TestHelpers.GetFriendlyTypeName(
					typeof(IDictionary<,>).MakeGenericType(typeof(string), typeof(int)),
					useSystemType: false
				)
			);

			return parameter;
		}
	}

	public static TheoryData<int> GetGenericTypeDefCount => [1, 2, 5];
}
