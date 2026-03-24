using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Records;

record ActivitySourceTarget(
	TelemetryGenerationAttributeRecord TelemetryGeneration,
	GenerationType GenerationType,
	string ClassNameToGenerate,
	string? ClassNamespace,
	EquatableArray<string> ParentClasses,
	string? FullNamespace,
	string? FullyQualifiedName,
	PurviewTypeInfo InterfaceType,
	ActivitySourceGenerationAttributeRecord? ActivitySourceGenerationAttribute,
	string? ActivitySourceName,
	EquatableArray<ActivityBasedGenerationTarget> ActivityMethods,
	ActivitySourceAttributeRecord ActivityTargetAttributeRecord
);

record ActivityBasedGenerationTarget(
	string MethodName,
	PurviewTypeInfo ReturnType,
	string ActivityOrEventName,
	bool HasActivityParameter,
	ActivityAttributeRecord? ActivityAttribute,
	EventAttributeRecord? EventAttribute,
	ActivityMethodType MethodType,
	EquatableArray<ActivityBasedParameterTarget> Parameters,
	EquatableArray<ActivityBasedParameterTarget> Baggage,
	EquatableArray<ActivityBasedParameterTarget> Tags,
	TargetGeneration TargetGenerationState
);

record ActivityBasedParameterTarget(
	string ParameterName,
	PurviewTypeInfo ParameterType,
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
