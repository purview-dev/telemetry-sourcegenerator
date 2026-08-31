namespace Purview.Telemetry.SourceGenerator;

public partial class TelemetrySourceGeneratorTests : IncrementalSourceGeneratorTestBase<TelemetrySourceGenerator>
{
	[Test]
	public async Task Generate_GivenGeneratedAttributes_GeneratesAsExpected(CancellationToken cancellationToken)
	{
		// Arrange
		const string empty =
			@"

namespace Testing;

";

		// Act
		var generationResult = await GenerateAsync(empty, cancellationToken: cancellationToken);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}

	public static IEnumerable<string> BasicGenericParameters
	{
		get
		{
			List<string> parameter =
			[
				TestHelpers.GetFriendlyTypeName(typeof(List<>).MakeGenericType(typeof(string))),
				TestHelpers.GetFriendlyTypeName(
					typeof(IEnumerable<>).MakeGenericType(typeof(string)),
					useSystemType: false
				),
				TestHelpers.GetFriendlyTypeName(typeof(Dictionary<,>).MakeGenericType(typeof(string), typeof(int))),
				TestHelpers.GetFriendlyTypeName(
					typeof(IDictionary<,>).MakeGenericType(typeof(string), typeof(int)),
					useSystemType: false
				),
			];

			return parameter;
		}
	}

	public static IEnumerable<int> GetGenericTypeDefCount => [1, 2, 5];
}
