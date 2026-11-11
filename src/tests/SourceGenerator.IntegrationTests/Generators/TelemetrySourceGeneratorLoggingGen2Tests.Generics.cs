namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingGen2Tests
{
	[Test]
	public async Task Generate_GivenGenericLogMethod_GeneratesAGenericImplementation(
		CancellationToken cancellationToken
	)
	{
		// Arrange: LoggerMessage.Define cannot carry an open generic, so this must use the state path.
		const string source = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			[Logger]
			public interface ITestLogger
			{
				void Log<T>(T value);
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		await Assert.That(generationResult).DoesNotHaveDiagnostic("TSG1005");
		var generated = generationResult.GetSource("TestLoggerCore.Logging.g.cs");
		await Assert
			.That(generated)
			.Contains("void Log<T>(T value)")
			.Because("the generated method mirrors the interface's type parameter");
		await Assert
			.That(generated)
			.DoesNotContain("LoggerMessage.Define")
			.Because("an open generic cannot be carried by LoggerMessage.Define");
	}

	[Test]
	public async Task Generate_GivenGenericLogMethodWithConstraints_MirrorsTheConstraints(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			using System;
			using Microsoft.Extensions.Logging;

			namespace Testing;

			[Logger]
			public interface ITestLogger
			{
				void LogStruct<T>(T value) where T : struct;

				void LogClass<T>(T value) where T : class, IDisposable, new();

				void LogUnmanaged<T>(T value) where T : unmanaged;

				void LogNotNull<T>(T value) where T : notnull;

				void LogBase<T>(T value) where T : System.IComparable<T>;
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		var generated = generationResult.GetSource("TestLoggerCore.Logging.g.cs");
		await Assert.That(generated).Contains("where T : struct");
		await Assert.That(generated).Contains("where T : class, global::System.IDisposable, new()");
		await Assert.That(generated).Contains("where T : unmanaged");
		await Assert.That(generated).Contains("where T : notnull");
		await Assert.That(generated).Contains("where T : global::System.IComparable<T>");
	}

	[Test]
	public async Task Generate_GivenGenericLogMethodWithMultipleTypeParameters_MirrorsAll(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			[Logger]
			public interface ITestLogger
			{
				void Log<TKey, TValue>(TKey key, TValue value);
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		await Assert
			.That(generationResult.GetSource("TestLoggerCore.Logging.g.cs"))
			.Contains("void Log<TKey, TValue>(TKey key, TValue value)");
	}
}
