using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Purview.Telemetry.SourceGenerator.Records;

/// <summary>
/// Represents a method that uses multi-target telemetry generation.
/// </summary>
sealed record MultiTargetMethod(
	string MethodName,
	string FullyQualifiedMethodName,
	IMethodSymbol MethodSymbol,
	MultiTargetConfiguration Configuration,
	ImmutableArray<MultiTargetParameter> Parameters,
	string ContainingTypeName,
	string Namespace,
	bool IsPartial,
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
	string? ConfigurationName = null
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
	All = Activities | Logging | Metrics,
}
