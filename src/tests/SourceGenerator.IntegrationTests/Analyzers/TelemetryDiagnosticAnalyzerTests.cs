using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator.Analyzers;

/// <summary>
/// Standalone analyzer tests that run <see cref="TelemetryDiagnosticAnalyzer"/> directly through the
/// framework's <c>TUnitDiagnosticAnalyzerTestBase</c>, without executing the generator. The telemetry
/// attribute types are supplied by <see cref="TelemetryAnalyzerTestOptions"/>.
/// </summary>
public class TelemetryDiagnosticAnalyzerTests
	: TUnitDiagnosticAnalyzerTestBase<TelemetryDiagnosticAnalyzer, TelemetryAnalyzerTestOptions>
{
	[Test]
	public async Task Analyze_GivenGenericActivityInterface_RaisesGenericInterfaceDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			[ActivitySource]
			public interface ITestActivities<T>
			{
			}
			""";

		var result = await AnalyzeAsync(code, new TelemetryAnalyzerTestOptions(), cancellationToken);

		await Assert.That(result).HasDiagnostic("TSG1004");
	}

	[Test]
	public async Task Analyze_GivenDuplicateActivityMethodNames_RaisesDuplicateNameDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string code = """

			[ActivitySource]
			public interface ITestActivities
			{
				[Activity]
				System.Diagnostics.Activity? DoWork([Baggage]string stringParam);

				[Activity]
				System.Diagnostics.Activity? DoWork([Tag]int intParam);
			}

			""";

		var result = await AnalyzeAsync(code, new TelemetryAnalyzerTestOptions(), cancellationToken);

		await Assert.That(result).HasDiagnostic("TSG1003");
	}

	[Test]
	public async Task Analyze_GivenLogMethodReturningString_RaisesInvalidReturnDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string code = """

			[Logger]
			public interface ITestLogger
			{
				[Log]
				string Log(string message);
			}

			""";

		var result = await AnalyzeAsync(code, new TelemetryAnalyzerTestOptions(), cancellationToken);

		await Assert.That(result).HasDiagnostic("TSG2021");
	}

	[Test]
	[SkipOnNetFramework]
	public async Task Analyze_GivenMetricMethodReturningInt_RaisesInvalidReturnDiagnostic(
		CancellationToken cancellationToken
	)
	{
		const string code = """

			[Meter("testing-meter")]
			public interface ITestMetrics
			{
				[Counter]
				int InvalidReturnType(int value);
			}

			""";

		var result = await AnalyzeAsync(code, new TelemetryAnalyzerTestOptions(), cancellationToken);

		await Assert.That(result).HasDiagnostic("TSG4001");
	}

	[Test]
	public async Task Analyze_GivenValidActivityInterface_RaisesNoDiagnostics(CancellationToken cancellationToken)
	{
		const string code = """

			[ActivitySource]
			public interface ITestActivities
			{
				[Activity]
				System.Diagnostics.Activity? DoWork([Baggage]string stringParam, [Tag]int intParam);
			}

			""";

		var result = await AnalyzeAsync(code, new TelemetryAnalyzerTestOptions(), cancellationToken);

		await Assert.That(result).HasNoDiagnostics();
	}
}
