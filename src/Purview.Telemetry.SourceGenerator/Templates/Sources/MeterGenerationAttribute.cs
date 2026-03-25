namespace Purview.Telemetry
{

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
	/// <param name="meterName">Optionally specifies the <see cref="MeterName" />.</param>
	/// <param name="nameGenerationType">Optionally specifies the <see cref="MeterNameGenerationType" />.</param>
	/// <param name="instrumentPrefix">Optionally specifies the <see cref="InstrumentPrefix" />.</param>
	/// <param name="lowercaseInstrumentName">Optionally specifies the <see cref="LowercaseInstrumentName" />.</param>
	/// <param name="lowercaseTagKeys">Optionally specifies the <see cref="LowercaseTagKeys" />.</param>
#if NET48_OR_GREATER || PURVIEW_TELEMETRY_NON_NULLABLE
	public MeterGenerationAttribute(string meterName = null,
#else
	public MeterGenerationAttribute(string? meterName = null,
#endif
		MeterNameGenerationType nameGenerationType = MeterNameGenerationType.DotNet,
#if NET48_OR_GREATER || PURVIEW_TELEMETRY_NON_NULLABLE
		string instrumentPrefix = null,
#else
		string? instrumentPrefix = null,
#endif
		bool lowercaseInstrumentName = true,
		bool lowercaseTagKeys = true)
	{
		MeterName = meterName;
		MeterNameGenerationType = nameGenerationType;
		InstrumentPrefix = instrumentPrefix;
		LowercaseInstrumentName = lowercaseInstrumentName;
		LowercaseTagKeys = lowercaseTagKeys;
	}

	/// <summary>
	/// Optional, gets/ sets the assembly-wide default meter name.
	/// Used when a <see cref="MeterAttribute"/> does not specify a name.
	/// If not set, the assembly name is used based on <see cref="MeterNameGenerationType"/>.
	/// </summary>
#if NET48_OR_GREATER || PURVIEW_TELEMETRY_NON_NULLABLE
	public string MeterName { get; set; }
#else
	public string? MeterName { get; set; }
#endif

	/// <summary>
	/// Optional, gets/ sets how meter names are generated when not explicitly specified.
	/// Defaults to <see cref="MeterNameGenerationType.DotNet"/>.
	/// </summary>
	public MeterNameGenerationType MeterNameGenerationType { get; set; } = MeterNameGenerationType.DotNet;

	/// <summary>
	/// Optional, gets/ sets the prefix used when generating the instrument name.
	/// </summary>
#if NET48_OR_GREATER || PURVIEW_TELEMETRY_NON_NULLABLE
	public string InstrumentPrefix { get; set; }
#else
	public string? InstrumentPrefix { get; set; }
#endif

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
}
