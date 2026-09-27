namespace Purview.Telemetry.SourceGenerator.Records;

sealed record ActivitySourceTarget(
	TelemetryGenerationAttributeData TelemetryGeneration,
	GenerationType GenerationType,
	string ClassNameToGenerate,
	EquatableArray<string> ParentClasses,
	TypeReference InterfaceType,
	ActivitySourceGenerationAttributeData? ActivitySourceGenerationAttribute,
	string? ActivitySourceName,
	EquatableArray<ActivityBasedGenerationTarget> ActivityMethods,
	ActivitySourceAttributeData ActivityTargetAttributeRecord
)
{
	public string? ClassNamespace => TelemetryGeneration.TelemetryNamesNamespace ?? InterfaceType.Identity.Namespace;

	public string? FullNamespace
	{
		get
		{
			var telemetryNamesNamespace = TelemetryGeneration.TelemetryNamesNamespace;
			if (telemetryNamesNamespace != null)
				return telemetryNamesNamespace + ".";

			var ns = InterfaceType.Identity.Namespace;
			if (ns == null && ParentClasses.IsEmpty)
				return null;

			System.Text.StringBuilder builder = new();
			if (ns != null)
				builder.Append(ns).Append('.');

			for (var i = ParentClasses.Count - 1; i >= 0; i--)
				builder.Append(ParentClasses[i]).Append('.');

			return builder.ToString();
		}
	}

	public string? FullyQualifiedName =>
		FullNamespace is null ? ClassNameToGenerate : FullNamespace + ClassNameToGenerate;
}

sealed record ActivityBasedGenerationTarget(
	string MethodName,
	TypeReference ReturnType,
	string ActivityOrEventName,
	bool HasActivityParameter,
	ActivityAttributeData? ActivityAttribute,
	EventAttributeData? EventAttribute,
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
)
{
	/// <summary>
	/// True when the parameter opts out of the Activities target via
	/// <c>[ExcludeTargets(Targets.Activities)]</c>. The parameter stays part of the generated method
	/// signature (it is declared on the interface) but is never applied to the Activity or ActivityEvent.
	/// </summary>
	public bool ExcludedFromActivities => ExcludedTargets.HasFlag(GenerationType.Activities);
}

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
