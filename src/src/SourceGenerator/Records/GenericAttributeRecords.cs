namespace Purview.Telemetry.SourceGenerator.Records;

sealed record TagOrBaggageAttributeRecord(AttributeStringValue Name, AttributeValue<bool> SkipOnNullOrEmpty);

sealed record TelemetryGenerationAttributeRecord(
	AttributeValue<bool> GenerateDependencyExtension,
	AttributeStringValue ClassName,
	AttributeStringValue DependencyInjectionClassName,
	AttributeValue<bool> DependencyInjectionClassIsPublic,
	AttributeValue<int> NamingConvention,
	AttributeValue<bool> GenerateTelemetryNamesClass,
	AttributeStringValue TelemetryNamesClassName
);

/// <summary>
/// sealed record for ExcludeTargetsAttribute on parameters.
/// Tracks which target families a parameter should be excluded from.
/// </summary>
sealed record ExcludeTargetsAttributeRecord(GenerationType ExcludedTargets);
