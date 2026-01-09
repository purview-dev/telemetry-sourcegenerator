namespace Purview.Telemetry;

/// <summary>
/// Specifies the telemetry generation target families.
/// Used with <see cref="ExcludeTargetsAttribute"/> to exclude parameters from specific targets.
/// </summary>
[global::System.Flags]
{CodeGen}
enum Targets
{
	/// <summary>
	/// No targets.
	/// </summary>
	None = 0,

	/// <summary>
	/// Activities generation target (ActivitySource, Activity, Event, Context).
	/// </summary>
	Activities = 1,

	/// <summary>
	/// Logging generation target (ILogger, Log, Info, Debug, etc.).
	/// </summary>
	Logging = 2,

	/// <summary>
	/// Metrics generation target (Meter, Counter, Histogram, etc.).
	/// </summary>
	Metrics = 4,

	/// <summary>
	/// All telemetry generation targets.
	/// </summary>
	All = Activities | Logging | Metrics
}
