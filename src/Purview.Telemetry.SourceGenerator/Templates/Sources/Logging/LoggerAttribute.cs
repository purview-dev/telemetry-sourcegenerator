#if !EXCLUDE_PURVIEW_TELEMETRY_LOGGING

namespace Purview.Telemetry;

/// <summary>
/// Marker attribute required for Log generation.
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Interface, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
[global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
sealed class LoggerAttribute : global::System.Attribute
{
	/// <summary>
	/// Creates a new instance of the <see cref="LoggerAttribute"/> class.
	/// </summary>
	public LoggerAttribute()
	{

	}

	/// <summary>
	/// Creates a new instance of the <see cref="LoggerAttribute"/>, specifying the <see cref="DefaultLevel"/>
	/// and optionally the <see cref="CustomPrefix"/>.
	/// </summary>
	/// <param name="defaultLevel">The default <see cref="global::Microsoft.Extensions.Logging.LogLevel"/> to use
	/// when one is not specified.</param>
	/// <param name="customPrefix">If specified, also sets the <see cref="global::Purview.Telemetry.LogPrefixType"/> to <see cref="global::Purview.Telemetry.LogPrefixType.Custom"/>.</param>
	/// <param name="disableMSLoggingTelemetryGeneration">Disables the generation of the new style of telemetry generation for Microsoft.Extensions.Logging.</param>
	public LoggerAttribute(global::Microsoft.Extensions.Logging.LogLevel defaultLevel, string? customPrefix = null, bool disableMSLoggingTelemetryGeneration = false)
	{
		DefaultLevel = defaultLevel;
		CustomPrefix = customPrefix;
		DisableMSLoggingTelemetryGeneration = disableMSLoggingTelemetryGeneration;

		if (!string.IsNullOrWhiteSpace(CustomPrefix))
		{
			PrefixType = global::Purview.Telemetry.LogPrefixType.Custom;
		}
	}

	/// <summary>
	/// Optional. Gets the <see cref="global::Microsoft.Extensions.Logging.LogLevel">level</see> of the
	/// log entry. Defaults to <see cref="global::Microsoft.Extensions.Logging.LogLevel.Information"/>, unless there is
	/// an <see cref="global::System.Exception"/> parameter and no-other override is defined.
	/// </summary>
	public global::Microsoft.Extensions.Logging.LogLevel DefaultLevel { get; set; } = global::Microsoft.Extensions.Logging.LogLevel.Information;

	/// <summary>
	/// Optional. The prefix used to when generating the log entry name.
	/// </summary>
	public string? CustomPrefix { get; set; }

	/// <summary>
	/// Specifies the mode used to generate or override the prefix for the log entry.
	/// </summary>
	public global::Purview.Telemetry.LogPrefixType PrefixType { get; set; }

	/// <summary>
	/// Disables the generation of the new style of telemetry generation for Microsoft.Extensions.Logging.
	/// 
	/// Defaults to false.
	/// </summary>
	public bool DisableMSLoggingTelemetryGeneration { get; set; }
}

#endif
