using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

partial class TelemetrySourceGenerator
{
	static void GenerateActivityFromMultiTarget(
		MultiTargetMethod method,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		// TODO: Implement Activity generation from multi-target method
		logger?.Debug($"Generating Activity for multi-target method: {method.MethodName}");
	}
}
