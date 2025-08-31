namespace Purview.Telemetry;

/// <summary>
/// Excludes a parameter from Activity telemetry generation when using multi-target generation.
/// Only applicable when the method has <see cref="TelemetryAttribute"/> with
/// <see cref="TelemetryAttribute.GenerateActivity"/> set to true.
/// </summary>
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

#if !EXCLUDE_PURVIEW_TELEMETRY_LOGGING

/// <summary>
/// Excludes a parameter from Logging telemetry generation when using multi-target generation.
/// Only applicable when the method has <see cref="TelemetryAttribute"/> with
/// <see cref="TelemetryAttribute.GenerateLogging"/> set to true.
/// </summary>
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
/// Excludes a parameter from Metrics telemetry generation when using multi-target generation.
/// Only applicable when the method has <see cref="TelemetryAttribute"/> with
/// <see cref="TelemetryAttribute.GenerateMetrics"/> set to true.
/// </summary>
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
