using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

partial class TelemetrySourceGenerator
{
	static void GenerateLoggingFromMultiTarget(
		MultiTargetMethod method,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		// TODO: Implement Logging generation from multi-target method
		logger?.Debug($"Generating Logging for multi-target method: {method.MethodName}");
	}
}
