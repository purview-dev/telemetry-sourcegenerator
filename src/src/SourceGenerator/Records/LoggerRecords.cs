namespace Purview.Telemetry.SourceGenerator.Records;

sealed record LoggerTarget(
	TelemetryGenerationAttributeData TelemetryGeneration,
	GenerationType GenerationType,
	string ClassNameToGenerate,
	EquatableArray<string> ParentClasses,
	TypeReference InterfaceType,
	LoggerAttributeData LoggerAttribute,
	int DefaultLevel,
	EquatableArray<LogMethodTarget> LogMethods,
	bool UseMSLoggingTelemetryBasedGeneration,
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

	public string FullyQualifiedName =>
		FullNamespace is null ? ClassNameToGenerate : FullNamespace + ClassNameToGenerate;
}

sealed record LogMethodTarget(
	string MethodName,
	bool IsScoped,
	string LoggerActionFieldName,
	bool UnknownReturnType,
	string LogName,
	int? EventId,
	string MessageTemplate,
	EquatableArray<MessageTemplateHole> TemplateProperties,
	bool TemplateIsOrdinalBased,
	bool TemplateIsNamedBased,
	string MSLevel,
	EquatableArray<LogParameterTarget> Parameters,
	EquatableArray<LogParameterTarget> ParametersSansException,
	LogParameterTarget? ExceptionParameter,
	bool HasMultipleExceptions,
	bool InferredErrorLevel,
	TargetGeneration TargetGenerationState,
	bool UseV1Generation,
	bool HasExplicitLevel = false,
	bool HasLogPropertiesAndExpandEnumerable = false,
	bool SkipEnabledCheck = false,
	EquatableArray<TypeParameterTarget> TypeParameters = default
)
{
	public int TotalParameterCount => Parameters.Count;

	public int ParameterCountSansException => ParametersSansException.Count;

	public bool IsGeneric => !TypeParameters.IsEmpty;
}

sealed record LogParameterTarget(
	string Name,
	string UpperCasedName,
	TypeReference ParameterType,
	bool IsException,
	bool IsFirstException,
	bool IsIEnumerable,
	bool IsArray,
	bool IsComplexType,
	LogPropertiesAttributeData? LogPropertiesAttribute,
	EquatableArray<LogPropertiesParameterDetails>? LogProperties,
	ExpandEnumerableAttributeData? ExpandEnumerableAttribute,
	GenerationType ExcludedTargets,
	EquatableArray<MessageTemplateHole> ReferencedHoles = default,
	UnionTargetRecord? Union = null,
	TagProviderTarget? TagProvider = null
)
{
	public bool UsedInTemplate => !ReferencedHoles.IsEmpty;

	/// <summary>
	/// The expression that yields the value logged for this parameter: the parameter itself, or the union's
	/// case value when the parameter is a union type.
	/// </summary>
	public string RenderValueExpression() => Union is null ? Name : Union.RenderValueExpression(Name);

	/// <summary>
	/// The type the log state carries for this parameter. A union is logged as its case value, so the state
	/// holds <c>object?</c> instead of the union struct — which is also what the logger would otherwise
	/// format, because a union struct's <c>ToString</c> is not its contents.
	/// </summary>
	public bool RequiresObjectState => Union is not null;
}

/// <summary>
/// One property, or one step of a transitive property path, flattened out of a <c>[LogProperties]</c> parameter.
/// </summary>
/// <param name="PropertyName">
/// The tag name beneath the parameter, dot-separated for a transitive path: <c>TemperatureC</c>, or
/// <c>Location.City</c> for a nested property.
/// </param>
/// <param name="IsNullable">Whether the property's value can be null, which decides <c>SkipNullProperties</c>.</param>
/// <param name="Accessor">
/// The expression appended to the parameter name to read the value, including the null-conditional operators the
/// chain needs: <c>?.TemperatureC</c>, or <c>?.Location?.City</c>.
/// </param>
readonly record struct LogPropertiesParameterDetails(string PropertyName, bool IsNullable, string? Accessor = null)
{
	public string ValueAccessor => Accessor ?? ("." + PropertyName);
}

/// <summary>
/// A resolved <c>[TagProvider]</c>: the static method whose returned pairs are added to the log state as extra tags.
/// </summary>
/// <param name="ProviderTypeName">
/// The provider type's fully-qualified name, including the <c>global::</c> prefix the emitters rely on.
/// </param>
/// <param name="MethodName">The static provider method's name.</param>
/// <param name="OmitReferenceName">
/// Whether the parameter name is left off the contributed tag names, matching the same-named
/// <c>[TagProvider]</c> property.
/// </param>
readonly record struct TagProviderTarget(string ProviderTypeName, string MethodName, bool OmitReferenceName);

readonly record struct LogLevelDetails(TypeIdentity LevelType, int Value, string Name);
