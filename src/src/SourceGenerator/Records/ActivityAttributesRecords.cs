namespace Purview.Telemetry.SourceGenerator.Records;

record ActivitySourceAttributeRecord(
	AttributeStringValue Name,
	AttributeValue<bool> DefaultToTags,
	AttributeStringValue BaggageAndTagPrefix,
	AttributeValue<bool> IncludeActivitySourcePrefix,
	AttributeValue<bool> LowercaseBaggageAndTagKeys
);

record ActivitySourceGenerationAttributeRecord(
	AttributeStringValue Name,
	AttributeValue<bool> DefaultToTags,
	AttributeStringValue BaggageAndTagPrefix,
	AttributeStringValue BaggageAndTagSeparator,
	AttributeValue<bool> LowercaseBaggageAndTagKeys,
	AttributeValue<bool> GenerateDiagnosticsForMissingActivity
);

readonly record struct ActivityAttributeRecord(
	AttributeStringValue Name,
	AttributeValue<int> Kind,
	AttributeValue<bool> CreateOnly
);

record EventAttributeRecord(
	AttributeStringValue Name,
	AttributeValue<bool> UseRecordExceptionRules,
	AttributeValue<bool> RecordExceptionEscape,
	AttributeValue<int> StatusCode,
	AttributeStringValue StatusDescription
);
