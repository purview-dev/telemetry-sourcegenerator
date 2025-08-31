using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Records;

record ActivitySourceTarget(
	TelemetryGenerationAttributeRecord TelemetryGeneration,
	GenerationType GenerationType,
	string ClassNameToGenerate,
	string? ClassNamespace,
	string[] ParentClasses,
	string? FullNamespace,
	string? FullyQualifiedName,
	PurviewTypeInfo InterfaceType,
	ActivitySourceGenerationAttributeRecord? ActivitySourceGenerationAttribute,
	string? ActivitySourceName,
	ImmutableArray<ActivityBasedGenerationTarget> ActivityMethods,
	ActivitySourceAttributeRecord ActivityTargetAttributeRecord,
	Location? InterfaceLocation,
	ImmutableDictionary<string, Location[]> DuplicateMethods,
	ImmutableArray<(TelemetryDiagnosticDescriptor, ImmutableArray<Location>)>? Failures
)
{
	public static ActivitySourceTarget Failed(
		TelemetryDiagnosticDescriptor diagnostic,
		ImmutableArray<Location> locations
	) =>
		new(
			null!,
			GenerationType.None,
			null!,
			null,
			null!,
			null,
			null,
			Constants.Empty,
			null,
			null,
			[],
			null!,
			null,
			null!,
			[(diagnostic, locations)]
		);
}

record ActivityBasedGenerationTarget(
	string MethodName,
	PurviewTypeInfo ReturnType,
	string ActivityOrEventName,
	bool HasActivityParameter,
	ImmutableArray<Location> Locations,
	ActivityAttributeRecord? ActivityAttribute,
	EventAttributeRecord? EventAttribute,
	ActivityMethodType MethodType,
	ImmutableArray<ActivityBasedParameterTarget> Parameters,
	ImmutableArray<ActivityBasedParameterTarget> Baggage,
	ImmutableArray<ActivityBasedParameterTarget> Tags,
	TargetGeneration TargetGenerationState
);

record ActivityBasedParameterTarget(
	string ParameterName,
	PurviewTypeInfo ParameterType,
	string GeneratedName,
	ActivityParameterDestination ParamDestination,
	bool SkipOnNullOrEmpty,
	bool IsException,
	ImmutableArray<Location> Locations
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
