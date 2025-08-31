using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

partial class TelemetrySourceGenerator
{
	static void GenerateMetricsFromMultiTarget(
		MultiTargetMethod method,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		// TODO: Implement Metrics generation from multi-target method
		logger?.Debug($"Generating Metrics for multi-target method: {method.MethodName}");
	}
}
