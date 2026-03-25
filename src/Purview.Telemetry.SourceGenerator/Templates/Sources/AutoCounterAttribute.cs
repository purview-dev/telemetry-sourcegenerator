namespace Purview.Telemetry
{

/// <summary>
/// Specifies the meter type generated corresponds to a 
/// <see cref="global::System.Diagnostics.Metrics.Counter{T}"/> that auto-increments when called.
/// 
/// This is equivalent to applying the <see cref="CounterAttribute"/> with the
/// <see cref="CounterAttribute.AutoIncrement"/> property set to true.
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
[global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
sealed class AutoCounterAttribute : global::System.Attribute
{
	/// <summary>
	/// Creates a new instance of the <see cref="AutoCounterAttribute"/> class.
	/// </summary>
	public AutoCounterAttribute()
	{
	}

	/// <summary>
	/// Creates a new instance of the <see cref="AutoCounterAttribute"/> class, and specifies the
	/// <see cref="Name"/>, and optionally the <see cref="Unit"/> and <see cref="Description"/>.
	/// </summary>
	/// <param name="name">Specifies the <see cref="Name"/>.</param>
	/// <param name="unit">Optionally specifies the <see cref="Unit"/>.</param>
	/// <param name="description">Optionally specifies the <see cref="Description"/>.</param>
	public AutoCounterAttribute(string name, string unit = null, string description = null)
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
