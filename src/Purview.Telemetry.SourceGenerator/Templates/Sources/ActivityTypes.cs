namespace Purview.Telemetry.Activities;

/// <summary>
/// Marker attribute used to control the generation
/// of <see cref="global::System.Diagnostics.Activity">activities</see>.
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
[global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
sealed class ActivityAttribute : global::System.Attribute
{
	/// <summary>
	/// Constructs a new <see cref="ActivityAttribute"/>.
	/// </summary>
	public ActivityAttribute()
	{
	}

	/// <summary>
	/// Constructs a new <see cref="ActivityAttribute"/> and specifies the <see cref="Name"/>.
	/// </summary>
	/// <param name="name">Specifies the <see cref="Name"/>.</param>
	public ActivityAttribute(string name)
	{
		Name = name;
	}

	/// <summary>
	/// Constructs a new <see cref="ActivityAttribute"/> and specifies the <see cref="Kind"/>.
	/// </summary>
	/// <param name="kind">Specifies the <see cref="Kind"/>.</param>
	public ActivityAttribute(global::System.Diagnostics.ActivityKind kind)
	{
		Kind = kind;
	}

	/// <summary>
	/// Constructs a new <see cref="ActivityAttribute"/> and specifies the <see cref="Name" /> and
	/// optionally the <see cref="Kind"/> and/ or <see cref="CreateOnly"/>.
	/// </summary>
	/// <param name="name">Specifies the <see cref="Name"/>.</param>
	/// <param name="kind">Optionally specifies the <see cref="Kind"/>.</param>
	/// <param name="createOnly">Optionally specifies <see cref="CreateOnly"/>.</param>
	public ActivityAttribute(string name, global::System.Diagnostics.ActivityKind kind, bool createOnly = false)
	{
		Name = name;
		Kind = kind;
		CreateOnly = createOnly;
	}

	/// <summary>
	/// Optional. Gets/ sets the name of the <see cref="global::System.Diagnostics.Activity"/>.
	/// If this is not specified, the name of the method is used.
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// Optional. Gets/ sets the <see cref="global::System.Diagnostics.ActivityKind">kind</see> of the
	/// activity. Defaults to <see cref="global::System.Diagnostics.ActivityKind.Internal"/>.
	/// </summary>
	public global::System.Diagnostics.ActivityKind Kind { get; set; } = global::System.Diagnostics.ActivityKind.Internal;

	/// <summary>
	/// If true, the <see cref="global::System.Diagnostics.Activity"/> is created using
	/// <see cref="global::System.Diagnostics.ActivitySource.CreateActivity(string, global::System.Diagnostics.ActivityKind, string?, global::System.Collections.Generic.IEnumerable{global::System.Collections.Generic.KeyValuePair{string, object?}}?, global::System.Collections.Generic.IEnumerable{global::System.Diagnostics.ActivityLink}?, global::System.Diagnostics.ActivityIdFormat)" />, meaning it is not started by default. Otherwise
	/// <see cref="global::System.Diagnostics.ActivitySource.StartActivity(string, global::System.Diagnostics.ActivityKind, string?, global::System.Collections.Generic.IEnumerable{global::System.Collections.Generic.KeyValuePair{string, object?}}?, global::System.Collections.Generic.IEnumerable{global::System.Diagnostics.ActivityLink}?, global::System.DateTimeOffset)" />is used. The default is false.
	/// </summary>
	public bool CreateOnly { get; set; }
}

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

/// <summary>
/// Marker attribute used to control the generation
/// of <see cref="global::System.Diagnostics.ActivityEvent">events</see>.
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
[global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
sealed class EventAttribute : global::System.Attribute
{
    /// <summary>
    /// Generates a new <see cref="EventAttribute"/>.
    /// </summary>
    public EventAttribute(global::System.Diagnostics.ActivityStatusCode statusCode = global::System.Diagnostics.ActivityStatusCode.Unset)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Generates a new <see cref="EventAttribute"/>, specifying the <see cref="Name"/> and optionally
    /// the <see cref="UseRecordExceptionRules"/> property and/ or <see cref="RecordExceptionAsEscaped"/>.
    /// </summary>
    public EventAttribute(string name, bool useRecordExceptionRules = true, bool recordExceptionAsEscaped = true, global::System.Diagnostics.ActivityStatusCode statusCode = global::System.Diagnostics.ActivityStatusCode.Unset)
        : this(statusCode)
    {
        Name = name;
        UseRecordExceptionRules = useRecordExceptionRules;
        RecordExceptionAsEscaped = recordExceptionAsEscaped;
    }

    /// <summary>
    /// Optional. Gets/ sets the name of the event. If null, empty or white-space
    /// then the name of the method is used.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Determines if the event should use OpenTelemetry exception recording rules.
    /// </summary>
    public bool UseRecordExceptionRules { get; set; } = true;

    /// <summary>
    /// Determines if a recorded exception (when <see cref="UseRecordExceptionRules"/> is true and an exception parameter exists)
    /// if the exception prevented the operation from completing (true) or if the exception was caught and handled (false)
    /// and did not affect the operation. Alternatively, use the <see cref="EscapeAttribute"/> to override this value by
    /// providing a value dynamically.
    /// </summary>
    public bool RecordExceptionAsEscaped { get; set; } = true;

	/// <summary>
	/// Optional. Gets/ sets the status code of the event. If status code is <see cref="global::System.Diagnostics.ActivityStatusCode.Error"/>
	/// the status description is determined using following values in order of precedence:
	///	<list type="number">
	///		<item>A string parameter with the <see cref="StatusDescriptionAttribute"/> defined.</item>
	///		<item>The <see cref="StatusDescription"/> property.</item>
	///		<item>If none of the above are found the first parameter that is of type <see cref="global::System.Exception"/>, it's <see cref="global::System.Exception.Message"/> property.</item>
	///		<item>Null, or no value is specified.</item>
	/// </list>
	///
	/// Defaults to <see cref="global::System.Diagnostics.ActivityStatusCode.Unset"/>."
	/// </summary>
	public global::System.Diagnostics.ActivityStatusCode StatusCode { get; set; }

	/// <summary>
	/// Optionally provides a description for the <see cref="StatusCode"/> when
	/// set to <see cref="global::System.Diagnostics.ActivityStatusCode.Error"/>.
	/// </summary>
	public string? StatusDescription { get; set; }
}

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

/// <summary>
/// Determines if the methods parameters should be
/// added to the current <see cref="global::System.Diagnostics.Activity"/>, using
/// either the <see cref="global::Purview.Telemetry.TagAttribute"/>,
/// the <see cref="global::Purview.Telemetry.Activities.BaggageAttribute"/> or inferred.
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
sealed class ContextAttribute : global::System.Attribute
{
}

/// <summary>
/// Marker attribute required for explicitly setting a
/// parameter as baggage when generating and <see cref="global::System.Diagnostics.Activity"/>
/// or an <see cref="global::System.Diagnostics.ActivityEvent"/>.
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Parameter, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
[global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
sealed class BaggageAttribute : global::System.Attribute
{
	/// <summary>
	/// Create a new <see cref="BaggageAttribute"/>.
	/// </summary>
	public BaggageAttribute()
	{
	}

	/// <summary>
	/// Create a new <see cref="BaggageAttribute"/> and sets the <see cref="SkipOnNullOrEmpty"/>
	/// property.
	/// </summary>
	public BaggageAttribute(bool skipOnNullOrEmpty)
	{
		SkipOnNullOrEmpty = skipOnNullOrEmpty;
	}

	/// <summary>
	/// Create a new <see cref="BaggageAttribute"/> and sets the <see cref="Name"/>
	/// and optionally the <see cref="SkipOnNullOrEmpty"/> property.
	/// </summary>
	/// <param name="name">Sets the <see cref="Name"/>.</param>
	/// <param name="skipOnNullOrEmpty">Optionally sets the <see cref="SkipOnNullOrEmpty"/> (defaults to false).</param>
	public BaggageAttribute(string? name, bool skipOnNullOrEmpty = false)
	{
		Name = name;
		SkipOnNullOrEmpty = skipOnNullOrEmpty;
	}

	/// <summary>
	/// Specifies the name of the baggage item. If null, empty or white-space
	/// defaults to the name of the parameter.
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// Determines if the parameter should be skipped when the value is a default value.
	/// </summary>
	public bool SkipOnNullOrEmpty { get; set; }
}

/// <summary>
/// Determines the default <see cref="global::System.Diagnostics.ActivitySource.Name" /> for generated
/// <see cref="global::System.Diagnostics.Activity">activities</see> and <see cref="global::System.Diagnostics.ActivityEvent">events</see>.
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Assembly, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
[global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
sealed class ActivitySourceGenerationAttribute : global::System.Attribute
{
	/// <summary>
	/// Constructs a new <see cref="ActivitySourceGenerationAttribute"/>.
	/// </summary>
	/// <param name="name">The name of the activity source.</param>
	/// <param name="defaultToTags">Determines if the default for method parameters are Tags (default) or Baggage.</param>
	/// <param name="generateDiagnosticsForMissingActivity">Determines if diagnostics are generated for missing activities. Defaults to true.</param>
	/// <exception cref="ArgumentNullException">If the <paramref name="name"/> is null, empty or white-space.</exception>
	public ActivitySourceGenerationAttribute(string name, bool defaultToTags = true, bool generateDiagnosticsForMissingActivity = true)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new System.ArgumentNullException(nameof(name));

		Name = name;
		DefaultToTags = defaultToTags;
		GenerateDiagnosticsForMissingActivity = generateDiagnosticsForMissingActivity;
	}

	/// <summary>
	/// Specifies the default <see cref="global::System.Diagnostics.ActivitySource.Name"/> to use.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Specifies the default used when inferring between
	/// <see cref="global::Purview.Telemetry.TagAttribute"/>
	/// or <see cref="global::Purview.Telemetry.Activities.BaggageAttribute"/>, unless
	/// explicitly marked. Overridden when specifying <see cref="global::Purview.Telemetry.Activities.ActivitySourceAttribute.DefaultToTags"/>.
	/// </summary>
	public bool DefaultToTags { get; set; } = true;

	/// <summary>
	/// Prefix used to when generating the tag or baggage name. Prepended
	/// before the <see cref="global::Purview.Telemetry.TagAttribute.Name"/> or
	/// <see cref="global::Purview.Telemetry.Activities.BaggageAttribute.Name"/>, unless
	/// explicitly marked. Overridden when specifying <see cref="global::Purview.Telemetry.Activities.ActivitySourceAttribute.BaggageAndTagPrefix"/>.
	/// </summary>
	public string? BaggageAndTagPrefix { get; set; }

	/// <summary>
	/// Determines the separator used between the <see cref="global::System.Diagnostics.ActivitySource.Name"/> and
	/// the various prefix options. The default is a period.
	/// </summary>
	public string BaggageAndTagSeparator { get; set; } = ".";

	/// <summary>
	/// Determines if the <see cref="global::Purview.Telemetry.TagAttribute.Name"/> or
	/// <see cref="global::Purview.Telemetry.Activities.BaggageAttribute.Name"/> (including
	/// any prefixes) are lower-cased, unless
	/// explicitly marked. Overridden when specifying <see cref="global::Purview.Telemetry.Activities.ActivitySourceAttribute.LowercaseBaggageAndTagKeys"/>.
	/// </summary>
	public bool LowercaseBaggageAndTagKeys { get; set; } = true;

	/// <summary>
	/// Determines if diagnostics are generated for when an Activity method does not return an activity, or
	/// when an Event or Context method does not include and Activity as a parameter.
	/// </summary>
	public bool GenerateDiagnosticsForMissingActivity { get; set; } = true;
}

/// <summary>
/// Marker attribute required for <see cref="global::System.Diagnostics.Activity"/>
/// and <see cref="global::System.Diagnostics.ActivityEvent"/> generation.
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Interface, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
[global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
sealed class ActivitySourceAttribute : global::System.Attribute
{
	/// <summary>
	/// Constructs a new <see cref="ActivitySourceAttribute"/>.
	/// </summary>
	public ActivitySourceAttribute()
	{
	}

	/// <summary>
	/// Constructs a new <see cref="ActivitySourceAttribute"/> specifying the <see cref="Name"/>.
	/// </summary>
	/// <param name="name">The <see cref="Name"/>.</param>
	public ActivitySourceAttribute(string name)
	{
		Name = name;
	}

	/// <summary>
	/// Sets the name for the generated <see cref="global::System.Diagnostics.ActivitySource.Name"/>,
	/// overriding the <see cref="global::Purview.Telemetry.Activities.ActivitySourceGenerationAttribute.Name"/>.
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// Specifies the default when inferring between
	/// <see cref="global::Purview.Telemetry.TagAttribute"/> or
	/// <see cref="global::Purview.Telemetry.Activities.BaggageAttribute"/>, unless
	/// explicitly marked.
	/// </summary>
	public bool DefaultToTags { get; set; } = true;

	/// <summary>
	/// Prefix used to when generating the tag or baggage name. Prepended
	/// before the <see cref="global::Purview.Telemetry.TagAttribute.Name"/> or
	/// <see cref="global::Purview.Telemetry.Activities.BaggageAttribute.Name"/>.
	/// </summary>
	public string? BaggageAndTagPrefix { get; set; }

	/// <summary>
	/// Determines if the <see cref="Name"/> (or <see cref="global::Purview.Telemetry.Activities.ActivitySourceGenerationAttribute.BaggageAndTagPrefix"/>)
	/// is used as a prefix, before the <see cref="BaggageAndTagPrefix"/>.
	/// </summary>
	public bool IncludeActivitySourcePrefix { get; set; } = true;

	/// <summary>
	/// Determines if the <see cref="global::Purview.Telemetry.TagAttribute.Name"/> or
	/// <see cref="global::Purview.Telemetry.Activities.BaggageAttribute.Name"/> (including
	/// any prefixes) are lower-cased.
	/// </summary>
	public bool LowercaseBaggageAndTagKeys { get; set; } = true;
}
