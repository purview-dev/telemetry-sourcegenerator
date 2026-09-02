using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingGen2Tests
{
	[Test]
	[MethodDataSource(nameof(ExpandableArrays))]
	public async Task Generate_GivenMethodWithExpandableArrayOrEnumerable_GeneratesCorrectElements(
		string parameter,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var basicLogger =
			@$"

namespace Testing;

[Logger]
public interface ITestLogger
{{
	void Log({parameter});
}}
";

		// Act
		var generationResult = await GenerateAsync(basicLogger, cancellationToken: cancellationToken);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}

	[Test]
	[MethodDataSource(nameof(ExpandableMaxCount))]
	public async Task Generate_GivenMethodWithExpandableAndHighMaxCount_GeneratesDiagnostic(
		int maxCount,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var basicLogger =
			@$"

namespace Testing;

[Logger]
public interface ITestLogger
{{
		void Log([ExpandEnumerable(maximumValueCount: {maxCount})]string[] paramValue);

		void Log2([ExpandEnumerable(MaximumValueCount = {maxCount})]string[] paramValue2);
}}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG2008");
	}

	public static IEnumerable<string> ExpandableArrays
	{
		get
		{
			List<string> data =
			[
				"[ExpandEnumerable]string[] paramValue",
				"[ExpandEnumerable]System.String[] paramValue",
				"[ExpandEnumerable]System.Collections.Generic.IEnumerable<System.String> paramValue",
				"[ExpandEnumerable]System.Collections.Generic.IEnumerable<string> paramValue",
				"[ExpandEnumerable]System.Collections.Generic.ICollection<string> paramValue",
				"[ExpandEnumerable]System.Collections.Generic.IDictionary<string, int> paramValue",
			];

			return data;
		}
	}

	public static IEnumerable<int> ExpandableMaxCount
	{
		get
		{
			List<int> data = [6, 12, 100, 10_000, int.MaxValue];

			return data;
		}
	}
}
