namespace Purview.Telemetry.SourceGenerator.Records;

record TagOrBaggageAttributeRecord(AttributeStringValue Name, AttributeValue<bool> SkipOnNullOrEmpty);

record TelemetryGenerationAttributeRecord(
	AttributeValue<bool> GenerateDependencyExtension,
	AttributeStringValue ClassName,
	AttributeStringValue DependencyInjectionClassName,
	AttributeValue<bool> DependencyInjectionClassIsPublic,
	AttributeValue<int> NamingConvention,
	AttributeValue<bool> GenerateTelemetryNamesClass,
	AttributeStringValue TelemetryNamesClassName
);

/// <summary>
/// Record for ExcludeTargetsAttribute on parameters.
/// Tracks which target families a parameter should be excluded from.
/// </summary>
record ExcludeTargetsAttributeRecord(GenerationType ExcludedTargets);
