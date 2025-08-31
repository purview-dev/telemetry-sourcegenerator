namespace Purview.Telemetry;

/// <summary>
/// Assembly-level attribute to enable multi-target telemetry generation.
/// When applied to an assembly, allows methods to use <see cref="TelemetryAttribute"/>
/// and related exclusion attributes.
/// </summary>
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
