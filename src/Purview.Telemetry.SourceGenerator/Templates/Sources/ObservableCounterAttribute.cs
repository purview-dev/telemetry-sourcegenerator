namespace Purview.Telemetry
{

/// <summary>
/// Specifies the meter type generated corresponds to a <see cref="global::System.Diagnostics.Metrics.ObservableCounter{T}"/>.
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
[global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
sealed class ObservableCounterAttribute : global::System.Attribute
{
	/// <summary>
	/// Creates a new instance of the <see cref="ObservableCounterAttribute"/> class.
	/// </summary>
	public ObservableCounterAttribute()
	{
	}

	/// <summary>
	/// Creates a new instance of the <see cref="ObservableCounterAttribute"/> class, and specifies the
	/// <see cref="Name"/>, and optionally the <see cref="Unit"/>, <see cref="Description"/>
	/// and <see cref="ThrowOnAlreadyInitialized"/> properties.
	/// </summary>
	/// <param name="name">Specifies the <see cref="Name"/>.</param>
	/// <param name="unit">Optionally specifies the <see cref="Unit"/>.</param>
	/// <param name="description">Optionally specifies the <see cref="Description"/>.</param>
	/// <param name="throwOnAlreadyInitialized">Optionally specifies if the observable counter throws an exception if it is already initialised. <see cref="ThrowOnAlreadyInitialized" />.</param>
	public ObservableCounterAttribute(string name, string unit = null, string description = null, bool throwOnAlreadyInitialized = false)
	{
		Name = name;
		Unit = unit;
		Description = description;
		ThrowOnAlreadyInitialized = throwOnAlreadyInitialized;
	}

	/// <summary>
	/// Optionally specifies the name of the instrument. If
	/// one is not specified, the method is used.
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

	/// <summary>
	/// Optional, determines if the instrument method throws
	/// if it's already initialised. Defaults to false.
	/// </summary>
	public bool ThrowOnAlreadyInitialized { get; set; }
}
}
