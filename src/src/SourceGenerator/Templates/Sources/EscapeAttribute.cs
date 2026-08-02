namespace Purview.Telemetry
{

/// <summary>
/// Used during <see cref="global::System.Diagnostics.ActivityEvent"/> generation
/// when specifying an <see cref="global::System.Exception"/>. When true, determines if the
/// exception should be marked as escaped, i.e. the exception caused the
/// process/ action to end unexpectedly.
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Parameter, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
sealed class EscapeAttribute : global::System.Attribute
{
}
}
