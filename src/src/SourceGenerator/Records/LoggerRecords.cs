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
	bool UseMSLoggingTelemetryBasedGeneration
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

			var builder = new System.Text.StringBuilder();
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

readonly record struct LogPropertiesParameterDetails(string PropertyName, bool IsNullable);

readonly record struct LogLevelDetails(TypeIdentity LevelType, int Value, string Name);
