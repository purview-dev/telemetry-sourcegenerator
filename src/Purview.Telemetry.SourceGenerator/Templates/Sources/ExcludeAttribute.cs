namespace Purview.Telemetry;

/// <summary>
/// Excludes the method from any activity, logging, or meter generation.
/// <para>
/// <em>Note:</em> You must implement the method yourself when using this attribute through
/// the use of partial classes.
/// </para>
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
sealed class ExcludeAttribute : global::System.Attribute { }
