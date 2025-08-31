namespace Purview.Telemetry;

/// <summary>
/// Assembly-level attribute to enable multi-target telemetry generation.
/// When applied to an assembly, allows methods to use <see cref="TelemetryAttribute"/>
/// and related exclusion attributes.
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Assembly, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
[global::System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1019:Define accessors for attribute arguments"
)]
sealed class EnableMultiTargetGenerationAttribute : global::System.Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="EnableMultiTargetGenerationAttribute"/> class.
	/// </summary>
	public EnableMultiTargetGenerationAttribute() { }

	/// <summary>
	/// Determines if backwards compatibility mode is enabled.
	/// When true, existing single-target attributes continue to work alongside multi-target attributes.
	/// When false, only multi-target attributes are supported.
	/// Defaults to true.
	/// </summary>
	public bool BackwardsCompatibility { get; set; } = true;
}

/// <summary>
/// Excludes a parameter from Activity telemetry generation when using multi-target generation.
/// Only applicable when the method has <see cref="TelemetryAttribute"/> with
/// <see cref="TelemetryAttribute.GenerateActivity"/> set to true.
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Parameter, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
[global::System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1019:Define accessors for attribute arguments"
)]
sealed class ExcludeFromActivityAttribute : global::System.Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ExcludeFromActivityAttribute"/> class.
	/// </summary>
	public ExcludeFromActivityAttribute() { }
}

/// <summary>
/// Excludes a parameter from Metrics telemetry generation when using multi-target generation.
/// Only applicable when the method has <see cref="TelemetryAttribute"/> with
/// <see cref="TelemetryAttribute.GenerateMetrics"/> set to true.
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Parameter, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
[global::System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1019:Define accessors for attribute arguments"
)]
sealed class ExcludeFromMetricsAttribute : global::System.Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ExcludeFromMetricsAttribute"/> class.
	/// </summary>
	public ExcludeFromMetricsAttribute() { }
}

#if !EXCLUDE_PURVIEW_TELEMETRY_LOGGING

/// <summary>
/// Excludes a parameter from Logging telemetry generation when using multi-target generation.
/// Only applicable when the method has <see cref="TelemetryAttribute"/> with
/// <see cref="TelemetryAttribute.GenerateLogging"/> set to true.
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Parameter, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
[global::System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1019:Define accessors for attribute arguments"
)]
sealed class ExcludeFromLoggingAttribute : global::System.Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ExcludeFromLoggingAttribute"/> class.
	/// </summary>
	public ExcludeFromLoggingAttribute() { }
}

#endif

/// <summary>
/// Marker attribute to enable multi-target telemetry generation from a single method.
/// Allows generating Activity, Logging, and/or Metrics telemetry from one method.
/// Requires assembly-level opt-in via <see cref="EnableMultiTargetGenerationAttribute"/>.
/// </summary>
[global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
[global::System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Design",
	"CA1019:Define accessors for attribute arguments"
)]
sealed class TelemetryAttribute : global::System.Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TelemetryAttribute"/> class.
	/// </summary>
	public TelemetryAttribute() { }

	/// <summary>
	/// Determines if Activity telemetry should be generated for this method.
	/// </summary>
	public bool GenerateActivity { get; set; }

	/// <summary>
	/// Determines if Metrics telemetry should be generated for this method.
	/// </summary>
	public bool GenerateMetrics { get; set; }

	/// <summary>
	/// Optional name for the Activity. If not specified, uses the method name.
	/// Only used when <see cref="GenerateActivity"/> is true.
	/// </summary>
	public string? ActivityName { get; set; }

	/// <summary>
	/// Optional activity kind for the Activity. Defaults to Internal.
	/// Only used when <see cref="GenerateActivity"/> is true.
	/// </summary>
	public global::System.Diagnostics.ActivityKind ActivityKind { get; set; }

#if !EXCLUDE_PURVIEW_TELEMETRY_LOGGING

	/// <summary>
	/// Determines if Logging telemetry should be generated for this method.
	/// </summary>
	public bool GenerateLogging { get; set; }

	/// <summary>
	/// Optional log level for the Logging telemetry. Use Microsoft.Extensions.Logging.LogLevel values.
	/// Only used when <see cref="GenerateLogging"/> is true.
	/// </summary>
	public global::Microsoft.Extensions.Logging.LogLevel LogLevel { get; set; } =
		global::Microsoft.Extensions.Logging.LogLevel.Information;

	/// <summary>
	/// Optional log message template for the Logging telemetry.
	/// Only used when <see cref="GenerateLogging"/> is true.
	/// </summary>
	public string? LogMessage { get; set; }

	/// <summary>
	/// Optional event ID for the Logging telemetry.
	/// Only used when <see cref="GenerateLogging"/> is true.
	/// </summary>
	public int? LogEventId { get; set; }

#endif
}
