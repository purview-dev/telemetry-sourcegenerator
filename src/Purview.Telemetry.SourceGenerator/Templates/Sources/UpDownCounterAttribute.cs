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
	public UpDownCounterAttribute(string name, string unit = null, string description = null)
	{
		Name = name;
		Unit = unit;
		Description = description;
	}

	/// <summary>
	/// Optionally specifies the name of the meter. If one is not specified, the name
	/// of the method is used.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Optionally specifies the unit of the meter.
	/// </summary>
	public string Unit { get; set; }

	/// <summary>
	/// Optionally specifies the description of the meter.
	/// </summary>
	public string Description { get; set; }
}
}
