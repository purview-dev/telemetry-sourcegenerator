namespace Purview.Telemetry.SourceGenerator.Records;

sealed record LoggerAttributeRecord(
	AttributeValue<int> DefaultLevel,
	AttributeStringValue CustomPrefix,
	AttributeValue<int> PrefixType,
	AttributeValue<bool> DisableMSLoggingTelemetryGeneration
);

sealed record LoggerGenerationAttributeRecord(
	AttributeValue<int> DefaultLevel,
	AttributeValue<bool> DisableMSLoggingTelemetryGeneration,
	AttributeValue<int> DefaultPrefixType
);

sealed record LogAttributeRecord(
	AttributeValue<int> Level,
	AttributeStringValue MessageTemplate,
	AttributeValue<int> EventId,
	AttributeStringValue Name
);

sealed record LogPropertiesAttributeRecord(
	AttributeValue<bool> OmitReferenceName,
	AttributeValue<bool> SkipNullProperties,
	AttributeValue<bool> Transitive
);

sealed record ExpandEnumerableAttributeRecord(AttributeValue<int> MaximumValueCount);
