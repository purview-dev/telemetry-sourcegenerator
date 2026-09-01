namespace Purview.Telemetry.SourceGenerator.Records;

sealed record TagOrBaggageAttributeRecord(string? Name, bool SkipOnNullOrEmpty);

sealed record TelemetryGenerationAttributeRecord(
	bool GenerateDependencyExtension,
	string? ClassName,
	string? DependencyInjectionClassName,
	bool DependencyInjectionClassIsPublic,
	int NamingConvention,
	bool GenerateTelemetryNamesClass,
	string? TelemetryNamesClassName
);

/// <summary>
/// sealed record for ExcludeTargetsAttribute on parameters.
/// Tracks which target families a parameter should be excluded from.
/// </summary>
sealed record ExcludeTargetsAttributeRecord(GenerationType ExcludedTargets);
