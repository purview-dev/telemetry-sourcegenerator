namespace Purview.Telemetry.SourceGenerator.Records;

sealed record LoggerAttributeRecord(int? DefaultLevel, string? CustomPrefix, int? PrefixType, int? GenerationMode);

sealed record LoggerGenerationAttributeRecord(int? DefaultLevel, int? GenerationMode, int? DefaultPrefixType);

sealed record LogAttributeRecord(int? Level, string? MessageTemplate, int? EventId, string? Name, int? GenerationMode);

sealed record LogPropertiesAttributeRecord(bool OmitReferenceName, bool SkipNullProperties, bool Transitive);

sealed record ExpandEnumerableAttributeRecord(int MaximumValueCount);
