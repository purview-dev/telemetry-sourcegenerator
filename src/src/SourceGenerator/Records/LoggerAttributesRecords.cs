using Microsoft.CodeAnalysis;

namespace Purview.Telemetry.SourceGenerator.Records;

sealed record LoggerAttributeRecord(
	AttributeValue<int> DefaultLevel,
	AttributeStringValue CustomPrefix,
	AttributeValue<int> PrefixType,
	AttributeValue<int> GenerationMode
);

sealed record LoggerGenerationAttributeRecord(
	AttributeValue<int> DefaultLevel,
	AttributeValue<int> GenerationMode,
	AttributeValue<int> DefaultPrefixType
);

sealed record LogAttributeRecord(
	AttributeValue<int> Level,
	AttributeStringValue MessageTemplate,
	AttributeValue<int> EventId,
	AttributeStringValue Name,
	AttributeValue<int> GenerationMode
);

sealed record LogPropertiesAttributeRecord(
	AttributeValue<bool> OmitReferenceName,
	AttributeValue<bool> SkipNullProperties,
	AttributeValue<bool> Transitive
);

sealed record ExpandEnumerableAttributeRecord(AttributeValue<int> MaximumValueCount);
