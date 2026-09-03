using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Purview.Telemetry.SourceGenerator.Infra;

/// <summary>
/// Options for standalone analyzer tests. The analyzer resolves the telemetry attribute types by
/// metadata name, but a standalone analyzer run does not execute the generator's
/// <c>RegisterPostInitializationOutput</c>, so the attribute definitions are supplied via
/// <see cref="TelemetryTestAttributeSource.Attributes"/>. Seeded assemblies mirror
/// <see cref="TelemetrySourceGeneratorTestOptions"/>; metrics types are only referenced when
/// available (.NET Core+).
/// </summary>
public sealed record TelemetryAnalyzerTestOptions : AnalyzerTestOptions
{
	public TelemetryAnalyzerTestOptions()
	{
		AdditionalNamespaces = ["Purview.Telemetry"];

		AdditionalAssemblyTypes = [typeof(ILogger), typeof(ActivitySource)];

		// System.Diagnostics.Metrics is not available on .NET Framework.
		if (
			typeof(ActivitySource).Assembly.GetType("System.Diagnostics.Metrics.Meter", throwOnError: false) is not null
		)
			AdditionalAssemblyTypes = AdditionalAssemblyTypes.Add(typeof(Meter));

		AdditionalSources =
		[
			"[assembly: Purview.Telemetry.TelemetryGeneration(GenerateDependencyExtension = false)]",
			TelemetryTestAttributeSource.Attributes,
		];
	}
}
