namespace Purview.Telemetry.SourceGenerator.Records;

record LoggerAttributeRecord(
	AttributeValue<int> DefaultLevel,
	AttributeStringValue CustomPrefix,
	AttributeValue<int> PrefixType,
	AttributeValue<bool> DisableMSLoggingTelemetryGeneration
);

record LoggerGenerationAttributeRecord(
	AttributeValue<int> DefaultLevel,
	AttributeValue<bool> DisableMSLoggingTelemetryGeneration,
	AttributeValue<int> DefaultPrefixType
);

record LogAttributeRecord(
	AttributeValue<int> Level,
	AttributeStringValue MessageTemplate,
	AttributeValue<int> EventId,
	AttributeStringValue Name,
	AttributeValue<bool> DisableMSLoggingTelemetryGeneration
);

record LogPropertiesAttributeRecord(
	AttributeValue<bool> OmitReferenceName,
	AttributeValue<bool> SkipNullProperties,
	AttributeValue<bool> Transitive
);

record ExpandEnumerableAttributeRecord(AttributeValue<int> MaximumValueCount);
