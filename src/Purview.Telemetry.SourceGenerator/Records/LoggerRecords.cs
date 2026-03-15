using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Records;

record LoggerTarget(
	TelemetryGenerationAttributeRecord TelemetryGeneration,
	GenerationType GenerationType,
	string ClassNameToGenerate,
	string? ClassNamespace,
	string[] ParentClasses,
	string? FullNamespace,
	string FullyQualifiedName,
	PurviewTypeInfo InterfaceType,
	LoggerAttributeRecord LoggerAttribute,
	int DefaultLevel,
	ImmutableArray<LogMethodTarget> LogMethods,
	ImmutableDictionary<string, Location[]> DuplicateMethods,
	bool UseMSLoggingTelemetryBasedGeneration,
	ImmutableArray<(TelemetryDiagnosticDescriptor, ImmutableArray<Location>)>? Failures
)
{
	public static LoggerTarget Failed(
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
			null!,
			Constants.Empty,
			null!,
			0,
			[],
			null!,
			false,
			[(diagnostic, locations)]
		);
}

record LogMethodTarget(
	string MethodName,
	bool IsScoped,
	string LoggerActionFieldName,
	bool UnknownReturnType,
	string LogName,
	int? EventId,
	string MessageTemplate,
	ImmutableArray<MessageTemplateHole> TemplateProperties,
	bool TemplateIsOrdinalBased,
	bool TemplateIsNamedBased,
	string MSLevel,
	ImmutableArray<LogParameterTarget> Parameters,
	ImmutableArray<LogParameterTarget> ParametersSansException,
	LogParameterTarget? ExceptionParameter,
	bool HasMultipleExceptions,
	Location? MethodLocation,
	bool InferredErrorLevel,
	TargetGeneration TargetGenerationState,
	bool UseV1Generation
)
{
	public int TotalParameterCount => Parameters.Length;

	public int ParameterCountSansException => ParametersSansException.Length;
}

record LogParameterTarget(
	string Name,
	string UpperCasedName,
	PurviewTypeInfo ParameterType,
	bool IsException,
	bool IsFirstException,
	bool IsIEnumerable,
	bool IsArray,
	bool IsComplexType,
	ImmutableArray<Location> Locations,
	LogPropertiesAttributeRecord? LogPropertiesAttribute,
	ImmutableArray<LogPropertiesParameterDetails>? LogProperties,
	ExpandEnumerableAttributeRecord? ExpandEnumerableAttribute,
	GenerationType ExcludedTargets
)
{
	public bool UsedInTemplate => ReferencedHoles.Count > 0;

	public List<MessageTemplateHole> ReferencedHoles { get; } = [];
}

record LogPropertiesParameterDetails(string PropertyName, bool IsNullable);
