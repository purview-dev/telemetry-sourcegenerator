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

	[Test]
	public async Task Generate_GivenGeneratedAttributes_EmitsWarningFreeRegistrationOutput(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string empty =
			@"

namespace Testing;

";

		// Act
		var generationResult = await GenerateAsync(empty, cancellationToken: cancellationToken);

		// Assert
		var autoCounterAttribute = generationResult.GetSource("Purview.Telemetry.AutoCounterAttribute.g.cs");
		await Assert
			.That(autoCounterAttribute)
			.ContainsGeneratedCode("#pragma warning disable CS8625")
			.Because("the null-default string parameters must not emit CS8625 under #nullable enable");
		var has1591Pragma = autoCounterAttribute?.ContainsGeneratedCode("#pragma warning disable 1591") ?? false;
		await Assert
			.That(has1591Pragma)
			.IsFalse()
			.Because("missing-documentation warnings must be resolved with XML summaries, not pragmas");

		var targetsEnum = generationResult.GetSource("Purview.Telemetry.TargetsEnum.g.cs");
		await Assert
			.That(targetsEnum)
			.ContainsGeneratedCode("/// <summary>Excludes logging targets.</summary>")
			.Because("the public enum members must carry XML summaries so CS1591 is not raised");
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
