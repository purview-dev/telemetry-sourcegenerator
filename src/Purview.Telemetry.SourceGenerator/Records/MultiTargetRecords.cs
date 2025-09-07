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
	string? BaggageName,
	bool IsActivity,
	bool IsParentContext,
	bool IsStartTime,
	bool IsTimestamp,
	bool IsEscape,
	bool IsStatusDescription,
	bool IsException,
	bool IsTagsEnumerable,
	bool IsLinksEnumerable
);

/// <summary>
/// Configuration for multi-target telemetry generation.
/// </summary>
sealed record MultiTargetConfiguration(
	bool IsMultiTargetEnabled,
	GenerationType TargetTypes,
	// Activity configuration
	string? ActivityName = null,
	int ActivityKind = Constants.Activities.DefaultActivityKind,
	bool CreateActivityOnly = false,
	ActivityMethodType ActivityMethodType = ActivityMethodType.Activity,
	int ActivityStatusCode = 0, // 0 == Unset in System.Diagnostics.ActivityStatusCode
	string? ActivityStatusDescription = null,
	// Logging configuration
	string? LogMessage = null,
	string? LogLevel = null,
	int? LogEventId = null,
	string? LogName = null,
	bool UsesScopedLogging = false,
	// Metrics configuration
	MetricType MetricType = MetricType.Counter,
	string? MetricName = null,
	string? MetricUnit = null,
	string? MetricDescription = null
);

/// <summary>
/// Defines the type of metric to generate for multi-target generation.
/// </summary>
enum MetricType
{
	/// <summary>
	/// Counter metric that tracks cumulative values.
	/// </summary>
	Counter,

	/// <summary>
	/// UpDownCounter metric that can increase and decrease.
	/// </summary>
	UpDownCounter,

	/// <summary>
	/// Histogram metric that tracks distribution of values.
	/// </summary>
	Histogram,

	/// <summary>
	/// Gauge metric that tracks current values.
	/// </summary>
	Gauge,
}

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

	/// <summary>
	/// Determines the appropriate return type for a multi-target method based on enabled telemetry types.
	/// </summary>
	/// <param name="configuration">The multi-target configuration.</param>
	/// <param name="originalReturnType">The original method return type.</param>
	/// <returns>The return type to use for the generated method.</returns>
	public static string DetermineReturnType(
		MultiTargetConfiguration configuration,
		ITypeSymbol originalReturnType
	)
	{
		var hasActivity =
			configuration.TargetTypes.HasFlag(GenerationType.Activities)
			&& configuration.ActivityMethodType == ActivityMethodType.Activity;
		var hasScopedLogging =
			configuration.TargetTypes.HasFlag(GenerationType.Logging)
			&& configuration.UsesScopedLogging;

		// If both Activity and Scoped Logging are enabled, use the combined return type
		if (hasActivity && hasScopedLogging)
		{
			return "MultiTargetTelemetryResult";
		}

		// If only Activity is enabled and it's an Activity method type, return Activity?
		if (hasActivity)
		{
			return "global::System.Diagnostics.Activity?";
		}

		// If only Scoped Logging is enabled, return IDisposable?
		if (hasScopedLogging)
		{
			return "global::System.IDisposable?";
		}

		// Otherwise, return void (for Metrics-only, Event methods, Context methods, etc.)
		return "void";
	}
}
