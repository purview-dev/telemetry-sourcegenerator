namespace Purview.Telemetry.SourceGenerator.Records;

// Computed members on the ADMG-generated attribute models that expose the
// "not specified" state of optional value-typed fields as null (the generated
// models bake in sentinel defaults for these).

readonly partial record struct LoggerAttributeData
{
	public const int Unset = 99;

	public int? DefaultLevelOrNull => DefaultLevel == Unset ? null : DefaultLevel;

	public int? PrefixTypeOrNull => PrefixType == Unset ? null : PrefixType;

	public int? GenerationModeOrNull => GenerationMode == Unset ? null : GenerationMode;
}

readonly partial record struct LoggerGenerationAttributeData
{
	public const int Unset = 99;

	public int? DefaultLevelOrNull => DefaultLevel == Unset ? null : DefaultLevel;

	public int? GenerationModeOrNull => GenerationMode == Unset ? null : GenerationMode;

	public int? DefaultPrefixTypeOrNull => DefaultPrefixType == Unset ? null : DefaultPrefixType;
}

readonly partial record struct LogAttributeData
{
	public const int UnsetLevel = 99;

	public int? LevelOrNull => Level == UnsetLevel ? null : Level;

	public int? EventIdOrNull => EventId < 0 ? null : EventId;
}
