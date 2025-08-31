namespace Purview.Telemetry;

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

#if !EXCLUDE_PURVIEW_TELEMETRY_LOGGING

	/// <summary>
	/// Determines if Logging telemetry should be generated for this method.
	/// </summary>
	public bool GenerateLogging { get; set; }
#endif

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
