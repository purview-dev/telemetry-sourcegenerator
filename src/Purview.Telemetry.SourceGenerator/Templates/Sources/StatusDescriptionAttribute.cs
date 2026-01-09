namespace Purview.Telemetry;

/// <summary>
/// Marker attribute used to control the generation
/// of <see cref="System.Diagnostics.ActivityEvent">events</see>
/// when the status code is set to <see cref="System.Diagnostics.ActivityStatusCode.Error"/>.
/// 
/// Its presence on a parameter will be used as the status description.
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Parameter, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
[global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
sealed class StatusDescriptionAttribute : global::System.Attribute
{
}
