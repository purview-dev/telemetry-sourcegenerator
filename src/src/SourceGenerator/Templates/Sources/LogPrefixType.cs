#if !EXCLUDE_PURVIEW_TELEMETRY_LOGGING

namespace Purview.Telemetry
{

/// <summary>
/// The types of prefixes that can be used for the log entry name generation.
/// </summary>
{CodeGen}
enum LogPrefixType
{
	/// <summary>
	/// No suffix is added to the name.
	/// </summary>
	Default = 0,

	/// <summary>
	/// The name of the interface used as the source for generation.
	/// </summary>
	Interface = 1,

	/// <summary>
	/// The name of the class, either specified or generated.
	/// </summary>
	Class = 2,

	/// <summary>
	/// Uses the custom name specified by <see cref="global::Purview.Telemetry.LoggerAttribute.CustomPrefix"/>. This is used when
	/// the <see cref="global::Purview.Telemetry.LoggerAttribute.CustomPrefix"/> is set
	/// regardless of <see cref="global::Purview.Telemetry.LoggerAttribute.PrefixType"/>.
	/// </summary>
	Custom = 3,

	/// <summary>
	/// The name of the interface without the "I" prefix or "Log", "Logger" or "Telemetry" suffixes if they exist.
	/// 
	/// For example, IRepositoryLog, IRepositoryLogger, or IRepositoryTelemetry would all be "Repository". 
	/// </summary>
	TrimmedClassName = 4
}

}
#endif
