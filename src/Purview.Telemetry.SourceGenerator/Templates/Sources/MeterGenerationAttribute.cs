namespace Purview.Telemetry;

/// <summary>
/// Marker attribute, used to indicate a meter (or group of instruments) and how they should be generated.
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
[global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
sealed class MeterGenerationAttribute : global::System.Attribute
{
	/// <summary>
	/// Creates a new <see cref="MeterGenerationAttribute" /> with optional
	/// <paramref name="instrumentPrefix"/>, <paramref name="lowercaseInstrumentName"/>
	/// and/ or <paramref name="lowercaseTagKeys"/>.
	/// </summary>
	/// <param name="instrumentPrefix">Optionally specifies the <see cref="InstrumentPrefix" />.</param>
	/// <param name="lowercaseInstrumentName">Optionally specifies the <see cref="LowercaseInstrumentName" />.</param>
	/// <param name="lowercaseTagKeys">Optionally specifies the <see cref="LowercaseTagKeys" />.</param>
	public MeterGenerationAttribute(string? instrumentPrefix = null, bool lowercaseInstrumentName = true, bool lowercaseTagKeys = true)
	{
		InstrumentPrefix = instrumentPrefix;
		LowercaseInstrumentName = lowercaseInstrumentName;
		LowercaseTagKeys = lowercaseTagKeys;
	}

	/// <summary>
	/// Optional, gets/ sets the prefix used when generating the instrument name.
	/// </summary>
	public string? InstrumentPrefix { get; set; }

	/// <summary>
	/// Optional, gets/ sets the separator used when
	/// pre-pending any prefixes. Defaults to period.
	/// </summary>
	public string InstrumentSeparator { get; set; } = ".";

	/// <summary>
	/// Optional, gets/ sets a value indicating if the
	/// instrument name is lowercased. Defaults to true. 
	/// </summary>
	public bool LowercaseInstrumentName { get; set; } = true;

	/// <summary>
	/// Optional, get/ sets a value indicating if any tag
	/// keys/ names are lowercased. Defaults to true.
	/// </summary>
	public bool LowercaseTagKeys { get; set; } = true;
}
