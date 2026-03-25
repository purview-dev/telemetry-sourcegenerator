namespace Purview.Telemetry
{

/// <summary>
/// Determines if the methods parameters should be
/// added to the current <see cref="global::System.Diagnostics.Activity"/>, using
/// either the <see cref="global::Purview.Telemetry.TagAttribute"/>,
/// the <see cref="global::Purview.Telemetry.BaggageAttribute"/> or inferred.
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
sealed class ContextAttribute : global::System.Attribute
{
}
}
