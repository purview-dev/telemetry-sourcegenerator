namespace Purview.Telemetry.SourceGenerator.Records;

sealed record LoggerTarget(
	TelemetryGenerationAttributeData TelemetryGeneration,
	GenerationType GenerationType,
	string ClassNameToGenerate,
	string? ClassNamespace,
	EquatableArray<string> ParentClasses,
	string? FullNamespace,
	string FullyQualifiedName,
	TypeReference InterfaceType,
	LoggerAttributeData LoggerAttribute,
	int DefaultLevel,
	EquatableArray<LogMethodTarget> LogMethods,
	bool UseMSLoggingTelemetryBasedGeneration
);

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
	bool HasLogPropertiesAndExpandEnumerable = false
)
{
	public int TotalParameterCount => Parameters.Count;

	public int ParameterCountSansException => ParametersSansException.Count;
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
	EquatableArray<MessageTemplateHole> ReferencedHoles = default
)
{
	public bool UsedInTemplate => !ReferencedHoles.IsEmpty;
}

sealed record LogPropertiesParameterDetails(string PropertyName, bool IsNullable);
