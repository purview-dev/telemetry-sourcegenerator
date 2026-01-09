namespace Purview.Telemetry.SourceGenerator.Records;

record TagOrBaggageAttributeRecord(
	AttributeStringValue Name,
	AttributeValue<bool> SkipOnNullOrEmpty
);

record TelemetryGenerationAttributeRecord(
	AttributeValue<bool> GenerateDependencyExtension,
	AttributeStringValue ClassName,
	AttributeStringValue DependencyInjectionClassName,
	AttributeValue<bool> DependencyInjectionClassIsPublic
);

/// <summary>
/// Record for ExcludeTargetsAttribute on parameters.
/// Tracks which target families a parameter should be excluded from.
/// </summary>
record ExcludeTargetsAttributeRecord(GenerationType ExcludedTargets);
