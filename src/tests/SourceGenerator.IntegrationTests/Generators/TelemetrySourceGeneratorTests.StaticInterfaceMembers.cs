using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator;

/// <summary>
/// Static interface members are not part of what a class implements. An interface could already declare them on
/// .NET Core 3.0+ and .NET 7+ (<c>static abstract</c>); C# 15 lets one declare non-virtual static members on any
/// runtime, .NET Framework included, so a helper on a telemetry interface must be left alone by the generator and the
/// analyzer. The .NET Framework leg is skipped because the C# version these compilations use predates that.
/// </summary>
partial class TelemetrySourceGeneratorTests
{
	[Test]
	[SkipOnNetFramework]
	public async Task Generate_GivenAStaticInterfaceMember_ImplementsOnlyTheInstanceMethods(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			using System.Diagnostics;

			namespace Testing;

			[ActivitySource("testing-activity-source")]
			[Logger]
			[Meter("testing-meter")]
			public interface ITestTelemetry
			{
				[Activity]
				[Info]
				[AutoCounter]
				Activity? Work([Tag] int id);

				static string Describe(int id) => "work " + id;
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		var implementation = generationResult.Generated().GetClass("TestTelemetryCore", "Testing");
		await Assert.That(implementation.HasMethod("Work")).IsTrue();
		await Assert
			.That(implementation.HasMethod("Describe"))
			.IsFalse()
			.Because("a static interface member has nothing for the class to implement");
		await Assert
			.That(generationResult)
			.DoesNotHaveDiagnostic("TSG1001")
			.Because("a static helper is not a method the generator must infer a target for");
	}

	[Test]
	[SkipOnNetFramework]
	public async Task Generate_GivenAStaticOverloadOfATelemetryMethod_DoesNotReportDuplicateNames(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string source = """
			using System.Diagnostics;

			namespace Testing;

			[ActivitySource("testing-activity-source")]
			public interface ITestActivities
			{
				[Activity]
				Activity? Work([Tag] int id);

				static Activity? Work(ITestActivities telemetry, string id) => telemetry.Work(int.Parse(id));
			}
			""";

		// Act
		using var generationResult = await GenerateAsync(source, cancellationToken: cancellationToken);

		// Assert
		await Assert
			.That(generationResult)
			.DoesNotHaveDiagnostic("TSG1003")
			.Because("only instance methods share a generated implementation, so a static overload is no duplicate");
		await Assert
			.That(generationResult.Generated().GetClass("TestActivitiesCore", "Testing").HasMethod("Work"))
			.IsTrue();
	}
}
