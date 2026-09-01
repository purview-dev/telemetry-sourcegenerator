namespace Purview.Telemetry.SourceGenerator.Records;

sealed record ActivitySourceTarget(
	TelemetryGenerationAttributeRecord TelemetryGeneration,
	GenerationType GenerationType,
	string ClassNameToGenerate,
	string? ClassNamespace,
	EquatableArray<string> ParentClasses,
	string? FullNamespace,
	string? FullyQualifiedName,
	TypeReference InterfaceType,
	ActivitySourceGenerationAttributeRecord? ActivitySourceGenerationAttribute,
	string? ActivitySourceName,
	EquatableArray<ActivityBasedGenerationTarget> ActivityMethods,
	ActivitySourceAttributeRecord ActivityTargetAttributeRecord
);

sealed record ActivityBasedGenerationTarget(
	string MethodName,
	TypeReference ReturnType,
	string ActivityOrEventName,
	bool HasActivityParameter,
	ActivityAttributeRecord? ActivityAttribute,
	EventAttributeRecord? EventAttribute,
	ActivityMethodType MethodType,
	EquatableArray<ActivityBasedParameterTarget> Parameters,
	EquatableArray<ActivityBasedParameterTarget> Baggage,
	EquatableArray<ActivityBasedParameterTarget> Tags,
	TargetGeneration TargetGenerationState,
	EquatableArray<string> TypeParameters = default
);

sealed record ActivityBasedParameterTarget(
	string ParameterName,
	TypeReference ParameterType,
	string GeneratedName,
	ActivityParameterDestination ParamDestination,
	bool SkipOnNullOrEmpty,
	bool IsException,
	GenerationType ExcludedTargets
);

enum ActivityParameterDestination
{
	Tag,
	Baggage,
	ParentContextOrId,
	TagsEnumerable,
	LinksEnumerable,
	Activity,
	StartTime,
	Timestamp,
	Escape,
	StatusDescription,
}

enum ActivityMethodType
{
	Activity,
	Event,
	Context,
}
