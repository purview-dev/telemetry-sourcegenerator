using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator.Analyzers;

/// <summary>
/// <c>TSG1011</c> is decided by the compilation's preprocessor symbols, which the framework's analyzer
/// harness does not expose, so these tests build the compilation themselves with the symbols the SDK
/// defines for each target framework.
/// </summary>
public class UnsupportedTargetFrameworkTests
{
	/// <summary>The exact TSG1011 message. It names the supported targets and offers no opt-out.</summary>
	const string ExpectedMessage =
		"Purview Telemetry Source Generator requires .NET 8.0 or higher, or .NET Framework 4.8 or higher. "
		+ "The current target framework is not supported, so no telemetry is generated for it. "
		+ "Target net8.0 or later, or net48 or later; a library that also targets another framework "
		+ "(such as netstandard2.0) must reference the generator only from its supported targets.";

	const string TelemetryInterface = """

		namespace Testing;

		[Purview.Telemetry.ActivitySource("testing")]
		public interface ITestActivities
		{
		}

		""";

	[Test]
	public async Task Analyze_GivenNetStandardCompilation_RaisesTSG1011WithSupportedTargetsMessage(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var compilation = CreateCompilation("NETSTANDARD", "NETSTANDARD2_0", "NETSTANDARD2_0_OR_GREATER");

		// Act
		var diagnostics = await AnalyzeAsync(compilation, cancellationToken);

		// Assert
		var messages = diagnostics
			.Where(static diagnostic => diagnostic.Id == "TSG1011")
			.Select(static diagnostic => diagnostic.GetMessage(CultureInfo.InvariantCulture))
			.ToArray();

		await Assert.That(messages.Length).IsEqualTo(1);
		await Assert.That(messages[0]).IsEqualTo(ExpectedMessage);
	}

	[Test]
	public async Task Analyze_GivenNetStandardCompilationDefiningRemovedOptOut_StillRaisesTSG1011(
		CancellationToken cancellationToken
	)
	{
		// Arrange: PURVIEW_TELEMETRY_NON_NULLABLE was documented as suppressing TSG1011, but never did.
		var compilation = CreateCompilation("NETSTANDARD", "NETSTANDARD2_0", "PURVIEW_TELEMETRY_NON_NULLABLE");

		// Act
		var diagnostics = await AnalyzeAsync(compilation, cancellationToken);

		// Assert
		await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Id == "TSG1011")).IsTrue();
	}

	[Test]
	[Arguments("NET;NET8_0;NET8_0_OR_GREATER;NETCOREAPP")]
	[Arguments("NETFRAMEWORK;NET48;NET48_OR_GREATER")]
	public async Task Analyze_GivenSupportedTargetFramework_DoesNotRaiseTSG1011(
		string preprocessorSymbols,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var compilation = CreateCompilation(preprocessorSymbols.Split(';'));

		// Act
		var diagnostics = await AnalyzeAsync(compilation, cancellationToken);

		// Assert
		await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Id == "TSG1011")).IsFalse();
	}

	static CSharpCompilation CreateCompilation(params string[] preprocessorSymbols)
	{
		CSharpParseOptions parseOptions = new(LanguageVersion.Latest, preprocessorSymbols: preprocessorSymbols);
		var references = AppDomain
			.CurrentDomain.GetAssemblies()
			.Where(static assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
			.Select(static assembly => MetadataReference.CreateFromFile(assembly.Location));

		return CSharpCompilation.Create(
			"UnsupportedTargetFrameworkTests",
			[
				CSharpSyntaxTree.ParseText(TelemetryTestAttributeSource.Attributes, parseOptions),
				CSharpSyntaxTree.ParseText(TelemetryInterface, parseOptions),
			],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);
	}

	static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(
		CSharpCompilation compilation,
		CancellationToken cancellationToken
	) =>
		await compilation
			.WithAnalyzers([new TelemetryDiagnosticAnalyzer()], options: null)
			.GetAnalyzerDiagnosticsAsync(cancellationToken)
			.ConfigureAwait(false);
}
