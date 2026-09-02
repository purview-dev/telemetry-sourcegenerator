using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Purview.SourceGeneratorFramework;
using Purview.Telemetry.SourceGenerator.Analyzers;
using Purview.Telemetry.SourceGenerator.Helpers;

namespace Purview.Telemetry.SourceGenerator.Infra;

public sealed record TelemetrySourceGeneratorTestOptions : SourceGeneratorTestOptions
{
	public TelemetrySourceGeneratorTestOptions()
	{
		AdditionalNamespaces = ["Purview.Telemetry"];

		AdditionalAssemblyTypes =
		[
			typeof(Activity),
			typeof(Meter),
			typeof(IServiceCollection),
			typeof(LogLevel),
			typeof(LogPropertiesAttribute),
		];

		AnalyzerTypes = [typeof(TelemetryDiagnosticAnalyzer)];

		// Most tests do not want a dependency-injection extension generated.
		AdditionalSources = ["[assembly: Purview.Telemetry.TelemetryGeneration(GenerateDependencyExtension = false)]"];

		ExcludeGeneratedSourceHintNames =
		[
			PurviewTypeLibrary.Microsoft.CodeAnalysis.EmbeddedAttribute,
			.. TypeLibrary.TelemetryShared.GetGeneratedTypes().Select(c => c.MetadataFullName),
			.. TypeLibrary.Activities.GetGeneratedTypes().Select(c => c.MetadataFullName),
			.. TypeLibrary.Logging.GetGeneratedTypes().Select(c => c.MetadataFullName),
			.. TypeLibrary.Metrics.GetGeneratedTypes().Select(c => c.MetadataFullName),
		];
	}

	/// <summary>
	/// Disables the framework's automatic <see cref="DriverRunResult.EnsureValid"/> check.
	/// Pass to <c>GenerateAsync</c> for tests that intentionally produce compilation errors
	/// (invalid return types, duplicate method names, etc.). All other tests keep validation on.
	/// </summary>
	public static readonly TelemetrySourceGeneratorTestOptions NoValidation = new()
	{
		ThrowOnGenerationException = false,
	};

	public static new TelemetrySourceGeneratorTestOptions Default => new();
}
