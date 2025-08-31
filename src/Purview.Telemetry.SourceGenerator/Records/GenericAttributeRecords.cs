namespace Purview.Telemetry.SourceGenerator.Records;

sealed record TagOrBaggageAttributeRecord(
	AttributeStringValue Name,
	AttributeValue<bool> SkipOnNullOrEmpty
);

sealed record TelemetryGenerationAttributeRecord(
	AttributeValue<bool> GenerateDependencyExtension,
	AttributeStringValue ClassName,
	AttributeStringValue DependencyInjectionClassName,
	AttributeValue<bool> DependencyInjectionClassIsPublic
);
