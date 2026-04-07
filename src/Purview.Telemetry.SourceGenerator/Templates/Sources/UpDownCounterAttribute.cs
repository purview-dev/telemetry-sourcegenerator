namespace Purview.Telemetry
{

/// <summary>
/// Specifies the meter type generated corresponds to a <see cref="global::System.Diagnostics.Metrics.UpDownCounter{T}"/>.
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
[global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
sealed class UpDownCounterAttribute : System.Attribute
{
	/// <summary>
	/// Creates a new instance of the <see cref="UpDownCounterAttribute"/> class.
	/// </summary>
	public UpDownCounterAttribute()
	{
	}

	/// <summary>
	/// Creates a new instance of the <see cref="UpDownCounterAttribute"/> class, and specifies the
	/// <see cref="Name"/>, and optionally the <see cref="Unit"/> and <see cref="Description"/>.
	/// </summary>
	/// <param name="name">Specifies the <see cref="Name"/>.</param>
	/// <param name="unit">Optionally specifies the <see cref="Unit"/>.</param>
	/// <param name="description">Optionally specifies the <see cref="Description"/>.</param>
#if NET48_OR_GREATER || PURVIEW_TELEMETRY_NON_NULLABLE
	public UpDownCounterAttribute(string name, string unit = null, string description = null)
#else
	public UpDownCounterAttribute(string name, string? unit = null, string? description = null)
#endif
	{
		Name = name;
		Unit = unit;
		Description = description;
	}

	/// <summary>
	/// Optionally specifies the name of the meter. If one is not specified, the name
	/// of the method is used.
	/// </summary>
#if NET48_OR_GREATER || PURVIEW_TELEMETRY_NON_NULLABLE
	public string Name { get; set; }
#else
	public string? Name { get; set; }
#endif

	/// <summary>
	/// Optionally specifies the unit of the meter.
	/// </summary>
#if NET48_OR_GREATER || PURVIEW_TELEMETRY_NON_NULLABLE
	public string Unit { get; set; }
#else
	public string? Unit { get; set; }
#endif

	/// <summary>
	/// Optionally specifies the description of the meter.
	/// </summary>
#if NET48_OR_GREATER || PURVIEW_TELEMETRY_NON_NULLABLE
	public string Description { get; set; }
#else
	public string? Description { get; set; }
#endif
}
}
