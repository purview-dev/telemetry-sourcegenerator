#if !EXCLUDE_PURVIEW_TELEMETRY_LOGGING

namespace Purview.Telemetry;

/// <summary>
/// Sets defaults for the generation of loggers and log entries.
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Assembly, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
[global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
sealed class LoggerGenerationAttribute : global::System.Attribute
{
	/// <summary>
	/// Creates a new instance of <see cref="LoggerGenerationAttribute"/>.
	/// </summary>
	public LoggerGenerationAttribute()
	{
	}

	/// <summary>
	/// Creates a new instance of <see cref="LoggerGenerationAttribute"/> with the specified
	/// <see cref="DefaultLevel"/>.
	/// </summary>
	/// <param name="defaultLevel"></param>
	public LoggerGenerationAttribute(global::Microsoft.Extensions.Logging.LogLevel defaultLevel)
	{
		DefaultLevel = defaultLevel;
	}

	/// <summary>
	/// Gets/ sets the default <see cref="global::Microsoft.Extensions.Logging.LogLevel">level</see> of the
	/// logger. Defaults to <see cref="global::Microsoft.Extensions.Logging.LogLevel.Information"/>.
	/// </summary>
	public global::Microsoft.Extensions.Logging.LogLevel DefaultLevel { get; set; } = global::Microsoft.Extensions.Logging.LogLevel.Information;

	/// <summary>
	/// Disables the generation of the new style of telemetry generation for Microsoft.Extensions.Logging.
	/// 
	/// Defaults to false.
	/// </summary>
	public bool DisableMSLoggingTelemetryGeneration { get; set; }

	/// <summary>
	/// Specifies the default mode used to generate or override the prefix for the log entry.
	/// 
	/// Default when the <see cref="global::Purview.Telemetry.LoggerAttribute.PrefixType"/> is not set.
	/// 
	/// Defaults to <see cref="global::Purview.Telemetry.LogPrefixType.Default"/>.
	/// </summary>
	public global::Purview.Telemetry.LogPrefixType DefaultPrefixType { get; set; }
}

#endif
