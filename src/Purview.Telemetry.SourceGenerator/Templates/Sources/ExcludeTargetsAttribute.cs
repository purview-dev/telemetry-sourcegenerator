namespace Purview.Telemetry
{

/// <summary>
/// Excludes a parameter from specific telemetry generation targets.
/// Use this attribute when a method has multiple generation targets (Activity + Log + Metric)
/// and certain parameters should not be included in specific targets.
/// </summary>
/// <example>
/// <code>
/// [Activity]
/// [Info]
/// [Counter]
/// Activity? CacheHit(
///     string cacheName,
///     [ExcludeTargets(Targets.Metrics)] string message,  // Excluded from metrics
///     [InstrumentMeasurement] int count
/// );
/// </code>
/// </example>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Parameter, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
sealed class ExcludeTargetsAttribute : global::System.Attribute
{
	/// <summary>
	/// Creates a new instance of <see cref="ExcludeTargetsAttribute"/> with the specified targets to exclude.
	/// </summary>
	/// <param name="targets">The generation targets to exclude this parameter from.</param>
	public ExcludeTargetsAttribute(global::Purview.Telemetry.Targets targets)
	{
		ExcludedTargets = targets;
	}

	/// <summary>
	/// Gets the targets this parameter should be excluded from.
	/// </summary>
	public global::Purview.Telemetry.Targets ExcludedTargets { get; }
}
}
