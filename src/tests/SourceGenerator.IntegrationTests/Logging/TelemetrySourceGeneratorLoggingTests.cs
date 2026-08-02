namespace Purview.Telemetry.SourceGenerator.Logging;

public partial class TelemetrySourceGeneratorLoggingTests
	: IncrementalSourceGeneratorTestBase<TelemetrySourceGenerator>
{
	[Test]
	[MethodDataSource<TelemetrySourceGeneratorTests>(
		nameof(TelemetrySourceGeneratorTests.BasicGenericParameters)
	)]
	public async Task Generate_GivenMethodWithBasicGenericParams_GeneratesEntryCorrectly(
		string parameterType,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var basicLogger =
			@$"

namespace Testing;

[Logger]
public interface ITestLogger {{
	void LogEntryWithGenericTypeParam({parameterType} paramName);
}}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			cancellationToken: cancellationToken,
			parameters: parameterType
		);
	}

	[Test]
	[MethodDataSource<TelemetrySourceGeneratorTests>(
		nameof(TelemetrySourceGeneratorTests.GetGenericTypeDefCount)
	)]
	public async Task Generate_GivenInterfaceWithGenerics_RaisesDiagnostics(
		int genericTypeCount,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var genericTypeDef = string.Join(
			", ",
			Enumerable.Range(0, genericTypeCount).Select(i => $"T{i}")
		);
		var basicLogger =
			@$"

namespace Testing;

[Logger]
public interface ITestLogger<{genericTypeDef}> {{
	void LogEntryWithGenericTypeParam();
}}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			expectsDiagnostics: true,
			cancellationToken: cancellationToken,
			parameters: genericTypeCount
		);
	}

	[Test]
	[MethodDataSource<TelemetrySourceGeneratorTests>(
		nameof(TelemetrySourceGeneratorTests.GetGenericTypeDefCount)
	)]
	public async Task Generate_GivenMethodWithGenerics_RaisesDiagnostics(
		int genericTypeCount,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var genericTypeDef = string.Join(
			", ",
			Enumerable.Range(0, genericTypeCount).Select(i => $"T{i}")
		);
		var basicLogger =
			@$"

namespace Testing;

[Logger]
public interface ITestLogger<{genericTypeDef}> {{
	void LogEntryWithGenericTypeParam();
}}
";

		// Act
		var generationResult = await GenerateAsync(
			basicLogger,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			c => c.ScrubInlineGuids(),
			expectsDiagnostics: true,
			cancellationToken: cancellationToken,
			parameters: genericTypeCount
		);
	}
}
