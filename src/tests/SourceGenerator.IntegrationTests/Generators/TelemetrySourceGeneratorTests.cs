using Purview.SourceGeneratorFramework;
using Purview.Telemetry.SourceGenerator.Infra;

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
		var query = generationResult.Generated();
		await Assert
			.That(query.HasClass("ActivitySourceAttribute", "Purview.Telemetry"))
			.IsTrue()
			.Because("the shared activity-source attribute must be generated");
		await Assert
			.That(query.HasClass("LoggerAttribute", "Purview.Telemetry"))
			.IsTrue()
			.Because("the shared logger attribute must be generated");
		await Assert
			.That(query.HasClass("MeterAttribute", "Purview.Telemetry"))
			.IsTrue()
			.Because("the shared meter attribute must be generated");
		await Assert
			.That(query.HasClass("TelemetryGenerationAttribute", "Purview.Telemetry"))
			.IsTrue()
			.Because("the shared telemetry-generation attribute must be generated");
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
