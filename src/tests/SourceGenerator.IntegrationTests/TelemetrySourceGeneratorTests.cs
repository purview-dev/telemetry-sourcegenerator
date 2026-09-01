using Purview.SourceGeneratorFramework;

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
				new TypeIdentity(typeof(List<>).MakeGenericType(typeof(string))).ToString(),
				new TypeIdentity(typeof(IEnumerable<>).MakeGenericType(typeof(string))).ToString(),
				new TypeIdentity(typeof(Dictionary<,>).MakeGenericType(typeof(string), typeof(int))).ToString(),
				new TypeIdentity(typeof(IDictionary<,>).MakeGenericType(typeof(string), typeof(int))).ToString(),
			];

			return parameter;
		}
	}

	public static IEnumerable<int> GetGenericTypeDefCount => [1, 2, 5];
}
