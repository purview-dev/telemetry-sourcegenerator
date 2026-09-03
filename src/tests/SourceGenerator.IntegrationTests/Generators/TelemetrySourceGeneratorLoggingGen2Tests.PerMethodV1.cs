using Purview.SourceGeneratorFramework;
using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator.Logging;

public partial class TelemetrySourceGeneratorLoggingGen2Tests
{
	[Test]
	public async Task Generate_GivenV2InterfaceWithPerMethodV1Override_GeneratesV1StyleForOverriddenMethod(
		CancellationToken cancellationToken
	)
	{
		// Arrange: explicit v2 interface, one method forces v1 via GenerationMode
		const string source =
			@"
namespace Testing;

[Logger(GenerationMode = LoggerGenerationMode.V2)]
public interface ITestLogger {
	void RegularV2LogEntry(int value);

	[Log(GenerationMode = LoggerGenerationMode.V1)]
	void HotPathV1LogEntry(int value);
}
";

		// Act
		var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var loggerClass = query.GetClass("TestLoggerCore", "Testing");
		await Assert
			.That(loggerClass.HasMethod(query, "RegularV2LogEntry", TypeReference.Create<int>()))
			.IsTrue()
			.Because("the generated logger must contain the regular V2 log method");
		await Assert
			.That(loggerClass.HasMethod(query, "HotPathV1LogEntry", TypeReference.Create<int>()))
			.IsTrue()
			.Because("the generated logger must contain the V1-overridden log method");
	}

	[Test]
	public async Task Generate_GivenV2InterfaceWithSpecificAttributeV1Override_GeneratesV1StyleForOverriddenMethod(
		CancellationToken cancellationToken
	)
	{
		// Arrange: explicit v2 interface, one method forces v1 using a specific level attribute
		const string source =
			@"
namespace Testing;

[Logger(GenerationMode = LoggerGenerationMode.V2)]
public interface ITestLogger {
	void RegularV2LogEntry(string message);

	[Debug(GenerationMode = LoggerGenerationMode.V1)]
	void HotPathDebugEntry(string message);
}
";

		// Act
		var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		var loggerClass = query.GetClass("TestLoggerCore", "Testing");
		await Assert
			.That(loggerClass.HasMethod(query, "RegularV2LogEntry", TypeReference.Create<string>()))
			.IsTrue()
			.Because("the generated logger must contain the regular V2 log method");
		await Assert
			.That(loggerClass.HasMethod(query, "HotPathDebugEntry", TypeReference.Create<string>()))
			.IsTrue()
			.Because("the generated logger must contain the V1-overridden debug log method");
	}

	[Test]
	public async Task Generate_GivenV2InterfaceWithPerMethodV1Override_WithTooManyParams_RaisesDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange: explicit v1 override on a method with 7 params (v1 limit is 6) — should raise diagnostic
		const string source =
			@"
namespace Testing;

[Logger]
public interface ITestLogger {
	[Log(GenerationMode = LoggerGenerationMode.V1)]
	void HotPathV1LogEntry(int a, int b, int c, int d, int e, int f, int g);
}
";

		// Act
		var generationResult = await GenerateAsync(
			source,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG2001");
	}
}
