using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator.Logging;

public partial class TelemetrySourceGeneratorLoggingGen2Tests
	: IncrementalSourceGeneratorTestBase<TelemetrySourceGenerator>
{
	[Test]
	[MethodDataSource<TelemetrySourceGeneratorTests>(nameof(TelemetrySourceGeneratorTests.BasicGenericParameters))]
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
public interface ITestLogger
{{
	void LogEntryWithGenericTypeParam({parameterType} paramName);
}}
";

		// Act
		var generationResult = await GenerateAsync(basicLogger, cancellationToken: cancellationToken);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}

	[Test]
	[MethodDataSource<TelemetrySourceGeneratorTests>(nameof(TelemetrySourceGeneratorTests.GetGenericTypeDefCount))]
	public async Task Generate_GivenInterfaceWithGenerics_RaisesDiagnostics(
		int genericTypeCount,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var genericTypeDef = string.Join(", ", Enumerable.Range(0, genericTypeCount).Select(i => $"T{i}"));
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
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, expectsDiagnostics: true, cancellationToken: cancellationToken);
	}

	[Test]
	[MethodDataSource<TelemetrySourceGeneratorTests>(nameof(TelemetrySourceGeneratorTests.GetGenericTypeDefCount))]
	public async Task Generate_GivenMethodWithGenerics_RaisesDiagnostics(
		int genericTypeCount,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var genericTypeDef = string.Join(", ", Enumerable.Range(0, genericTypeCount).Select(i => $"T{i}"));
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
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, expectsDiagnostics: true, cancellationToken: cancellationToken);
	}

	[Test]
	public async Task Generate_GivenMethodWithMoreThanSixParameters_GeneratesEntry(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicLogger =
			@"

namespace Testing;

[Logger]
public interface ITestLogger {
	void LogEntryWithMoreThanSixParams(int one, int two, int three, int four, int five, int six, int seven, int eight, int nine, int ten, int eleven);
}
";

		// Act
		var generationResult = await GenerateAsync(basicLogger, cancellationToken: cancellationToken);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}
}
