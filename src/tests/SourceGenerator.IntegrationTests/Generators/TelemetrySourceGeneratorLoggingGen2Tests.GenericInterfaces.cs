using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator.Logging;

partial class TelemetrySourceGeneratorLoggingGen2Tests
{
	[Test]
	public async Task Generate_GivenGenericLoggerInterface_MirrorsTheTypeParametersOntoTheClass(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			[Logger]
			public interface ITestLogger<T> where T : class
			{
				void Log(T value);
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		await Assert.That(generationResult).DoesNotHaveDiagnostic("TSG1004");
		var generated = generationResult.GetSource("TestLoggerCore.Logging.g.cs");
		await Assert
			.That(generated)
			.Contains("class TestLoggerCore<T>")
			.Because("the generated class is generic over the interface's type parameters");
		await Assert
			.That(generated)
			.Contains("global::Testing.ITestLogger<T>")
			.Because("the implemented interface carries the same type argument");
		await Assert
			.That(generated)
			.Contains("where T : class")
			.Because("the interface's constraint is repeated on the generated class");
	}

	[Test]
	public async Task Generate_GivenGenericInterfaceWithDependenciesExtension_RegistersTheOpenGeneric(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			[Logger]
			public interface ITestLogger<T>
			{
				void Log(T value);
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(
			source,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert
			.That(generationResult.GetSource("TestLoggerCoreDIExtension.DependencyInjection.g.cs"))
			.Contains("typeof(global::Testing.ITestLogger<>)")
			.Because("an open generic interface can only be registered in its open form");
	}

	[Test]
	public async Task Generate_GivenGenericInterfaceWithUnsupportedConstraint_RaisesTSG1004(
		CancellationToken cancellationToken
	)
	{
		// Arrange: 'allows ref struct' cannot be repeated on the generated class.
		const string source = """
			using Microsoft.Extensions.Logging;

			namespace Testing;

			[Logger]
			public interface ITestLogger<T> where T : allows ref struct
			{
				void Log<TValue>(TValue value);
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(
			source,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG1004");
	}
}
