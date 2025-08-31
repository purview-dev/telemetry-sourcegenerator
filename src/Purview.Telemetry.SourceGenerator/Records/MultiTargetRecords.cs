using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Records;

/// <summary>
/// Represents an interface that uses multi-target telemetry generation.
/// </summary>
sealed record MultiTargetInterface(
	string InterfaceName,
	string FullyQualifiedInterfaceName,
	INamedTypeSymbol InterfaceSymbol,
	string? Namespace,
	string[] ParentClasses,
	TelemetryGenerationAttributeRecord TelemetryGeneration,
	ImmutableArray<MultiTargetMethod> Methods,
	GenerationType GenerationType,
	Location Location
);

/// <summary>
/// Represents a method that uses multi-target telemetry generation.
/// </summary>
sealed record MultiTargetMethod(
	string MethodName,
	string FullyQualifiedMethodName,
	IMethodSymbol MethodSymbol,
	MultiTargetConfiguration Configuration,
	ImmutableArray<MultiTargetParameter> Parameters,
	Location Location
);

/// <summary>
/// Represents a parameter in a multi-target method with exclusion information.
/// </summary>
sealed record MultiTargetParameter(
	string Name,
	string TypeName,
	IParameterSymbol ParameterSymbol,
	ParameterExclusions Exclusions,
	bool IsTag,
	bool IsBaggage,
	string? TagName,
	string? BaggageName
);

/// <summary>
/// Configuration for multi-target telemetry generation.
/// </summary>
sealed record MultiTargetConfiguration(
	bool IsMultiTargetEnabled,
	GenerationType TargetTypes,
	string? ActivityName = null,
	string? LogMessage = null,
	string? LogLevel = null,
	int? LogEventId = null
);

/// <summary>
/// Represents exclusion settings for parameters in multi-target scenarios.
/// </summary>
[Flags]
enum ParameterExclusions
{
	None = 0,
	Activities = 1 << 0,
	Logging = 1 << 1,
	Metrics = 1 << 2,
	All = Activities | Logging | Metrics
}

/// <summary>
/// Represents a multi-target telemetry generation target.
/// </summary>
sealed record MultiTargetGenerationTarget(
	TelemetryGenerationAttributeRecord TelemetryGeneration,
	GenerationType GenerationType,
	string ClassNameToGenerate,
	string? ClassNamespace,
	string[] ParentClasses,
	string? FullNamespace,
	string? FullyQualifiedName,
	PurviewTypeInfo InterfaceType,
	ImmutableArray<MultiTargetMethod> Methods,
	ImmutableDictionary<string, Location[]> DuplicateMethods,
	ImmutableArray<(TelemetryDiagnosticDescriptor, ImmutableArray<Location>)>? Failures
)
{
	public static MultiTargetGenerationTarget Failed(
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
			[],
			null!,
			[(diagnostic, locations)]
		);
}
