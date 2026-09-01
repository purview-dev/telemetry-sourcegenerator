namespace Purview.Telemetry.SourceGenerator.Records;

sealed record ActivitySourceAttributeRecord(
	string? Name,
	bool DefaultToTags,
	string? BaggageAndTagPrefix,
	bool IncludeActivitySourcePrefix,
	bool LowercaseBaggageAndTagKeys
);

sealed record ActivitySourceGenerationAttributeRecord(
	string? Name,
	bool DefaultToTags,
	string? BaggageAndTagPrefix,
	string? BaggageAndTagSeparator,
	bool LowercaseBaggageAndTagKeys,
	bool GenerateDiagnosticsForMissingActivity
);

sealed record ActivityAttributeRecord(string? Name, int? Kind, bool CreateOnly);

sealed record EventAttributeRecord(
	string? Name,
	bool UseRecordExceptionRules,
	bool RecordExceptionEscape,
	int StatusCode,
	string? StatusDescription
);
