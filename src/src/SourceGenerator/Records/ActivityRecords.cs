namespace Purview.Telemetry.SourceGenerator.Records;

sealed record ActivitySourceTarget(
	TelemetryGenerationAttributeData TelemetryGeneration,
	GenerationType GenerationType,
	string ClassNameToGenerate,
	EquatableArray<string> ParentClasses,
	TypeReference InterfaceType,
	ActivitySourceGenerationAttributeData? ActivitySourceGenerationAttribute,
	string? ActivitySourceName,
	EquatableArray<ActivityMethodTarget> ActivityMethods,
	ActivitySourceAttributeData ActivityTargetAttributeRecord,
	EquatableArray<TypeParameterTarget> InterfaceTypeParameters = default
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

/// <summary>
/// A method declared on an activity interface. A method is exactly one of these: it starts an activity, records an
/// event, or returns a context, so the hierarchy is <c>closed</c> and a <c>switch</c> over it is exhaustive once the
/// three cases are handled.
/// </summary>
/// <remarks>
/// The payload of each case lives on the case type rather than as a nullable member of the base, so a method that
/// starts an activity cannot be read for an event's attribute. The base carries only what every case needs.
/// </remarks>
/// <param name="MethodName">The interface method's name.</param>
/// <param name="ReturnType">The method's declared return type.</param>
/// <param name="ActivityOrEventName">The activity or event name, explicit or inferred.</param>
/// <param name="HasActivityParameter">Whether the method takes the <c>Activity</c> to record against.</param>
/// <param name="Parameters">Every parameter, in declaration order.</param>
/// <param name="Baggage">The parameters recorded as baggage.</param>
/// <param name="Tags">The parameters recorded as tags.</param>
/// <param name="TargetGenerationState">The method's generation state.</param>
/// <param name="TypeParameters">The method's generic type parameters and their constraints, empty for a non-generic method.</param>
closed record ActivityMethodTarget(
	string MethodName,
	TypeReference ReturnType,
	string ActivityOrEventName,
	bool HasActivityParameter,
	EquatableArray<ActivityBasedParameterTarget> Parameters,
	EquatableArray<ActivityBasedParameterTarget> Baggage,
	EquatableArray<ActivityBasedParameterTarget> Tags,
	TargetGeneration TargetGenerationState,
	EquatableArray<TypeParameterTarget> TypeParameters = default
);

/// <summary>A method that starts an activity: <c>[Activity]</c>, or inferred from an <c>Activity</c> return type.</summary>
sealed record StartActivityMethod(
	string MethodName,
	TypeReference ReturnType,
	string ActivityOrEventName,
	bool HasActivityParameter,
	EquatableArray<ActivityBasedParameterTarget> Parameters,
	EquatableArray<ActivityBasedParameterTarget> Baggage,
	EquatableArray<ActivityBasedParameterTarget> Tags,
	TargetGeneration TargetGenerationState,
	ActivityAttributeData? ActivityAttribute,
	EquatableArray<TypeParameterTarget> TypeParameters = default
)
	: ActivityMethodTarget(
		MethodName,
		ReturnType,
		ActivityOrEventName,
		HasActivityParameter,
		Parameters,
		Baggage,
		Tags,
		TargetGenerationState,
		TypeParameters
	);

/// <summary>A method that records an activity event: <c>[Event]</c>, or inferred from an <c>Event</c> name or an
/// <c>Activity</c> first parameter.</summary>
sealed record ActivityEventMethod(
	string MethodName,
	TypeReference ReturnType,
	string ActivityOrEventName,
	bool HasActivityParameter,
	EquatableArray<ActivityBasedParameterTarget> Parameters,
	EquatableArray<ActivityBasedParameterTarget> Baggage,
	EquatableArray<ActivityBasedParameterTarget> Tags,
	TargetGeneration TargetGenerationState,
	EventAttributeData? EventAttribute,
	EquatableArray<TypeParameterTarget> TypeParameters = default
)
	: ActivityMethodTarget(
		MethodName,
		ReturnType,
		ActivityOrEventName,
		HasActivityParameter,
		Parameters,
		Baggage,
		Tags,
		TargetGenerationState,
		TypeParameters
	);

/// <summary>A method that returns a context: <c>[Context]</c>, or inferred from a <c>Context</c> name.</summary>
sealed record ActivityContextMethod(
	string MethodName,
	TypeReference ReturnType,
	string ActivityOrEventName,
	bool HasActivityParameter,
	EquatableArray<ActivityBasedParameterTarget> Parameters,
	EquatableArray<ActivityBasedParameterTarget> Baggage,
	EquatableArray<ActivityBasedParameterTarget> Tags,
	TargetGeneration TargetGenerationState,
	EquatableArray<TypeParameterTarget> TypeParameters = default
)
	: ActivityMethodTarget(
		MethodName,
		ReturnType,
		ActivityOrEventName,
		HasActivityParameter,
		Parameters,
		Baggage,
		Tags,
		TargetGenerationState,
		TypeParameters
	);

sealed record ActivityBasedParameterTarget(
	string ParameterName,
	TypeReference ParameterType,
	string GeneratedName,
	ActivityParameterDestination ParamDestination,
	bool SkipOnNullOrEmpty,
	bool IsException,
	GenerationType ExcludedTargets,
	UnionTargetRecord? Union = null
)
{
	/// <summary>
	/// The expression that yields the value recorded for this parameter: the parameter itself, or the
	/// union's case value when the parameter is a union type.
	/// </summary>
	public string RenderValueExpression() => Union is null ? ParameterName : Union.RenderValueExpression(ParameterName);

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
