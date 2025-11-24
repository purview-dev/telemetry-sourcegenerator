namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingGen2Tests
{
	[Theory]
	[MemberData(nameof(ExpandableArrays))]
	public async Task Generate_GivenMethodWithExpandableArrayOrEnumerable_GeneratesCorrectElements(
		string parameter
	)
	{
		// Arrange
		var basicLogger =
			@$"
using Purview.Telemetry.Logging;

namespace Testing;

[Logger]
public interface ITestLogger
{{
	void Log({parameter});
}}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			includeLoggerTypes: IncludeLoggerTypes.Telemetry
		);

		// Assert
		await TestHelpers.Verify(generationResult, parameters: parameter);
	}

	[Theory]
	[MemberData(nameof(ExpandableMaxCount))]
	public async Task Generate_GivenMethodWithExpandableAndHighMaxCount_GeneratesDiagnostic(
		int maxCount
	)
	{
		// Arrange
		var basicLogger =
			@$"
using Purview.Telemetry.Logging;

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
			includeLoggerTypes: IncludeLoggerTypes.Telemetry
		);

		// Assert
		await TestHelpers.Verify(
			generationResult,
			c => c.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG2008"],
			parameters: maxCount
		);
	}

	public static TheoryData<string> ExpandableArrays
	{
		get
		{
			TheoryData<string> data = [];

			data.Add("[ExpandEnumerable]string[] paramValue");
			data.Add("[ExpandEnumerable]System.String[] paramValue");
			data.Add(
				"[ExpandEnumerable]System.Collections.Generic.IEnumerable<System.String> paramValue"
			);
			data.Add("[ExpandEnumerable]System.Collections.Generic.IEnumerable<string> paramValue");
			data.Add("[ExpandEnumerable]System.Collections.Generic.ICollection<string> paramValue");
			data.Add(
				"[ExpandEnumerable]System.Collections.Generic.IDictionary<string, int> paramValue"
			);

			return data;
		}
	}

	public static TheoryData<int> ExpandableMaxCount
	{
		get
		{
			TheoryData<int> data = [];

			data.Add(6);
			data.Add(12);
			data.Add(100);
			data.Add(10_000);
			data.Add(int.MaxValue);

			return data;
		}
	}
}
