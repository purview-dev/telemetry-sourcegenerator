#if !EXCLUDE_PURVIEW_TELEMETRY_LOGGING

namespace Purview.Telemetry
{

/// <summary>
/// Marker attribute used as an alternative to <see cref="global::Purview.Telemetry.LogAttribute"/>, where the <see cref="global::Purview.Telemetry.LogAttribute.Level"/>
/// is set to <see cref="global::Microsoft.Extensions.Logging.LogLevel.Information"/>.
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
[global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
sealed class InfoAttribute : global::System.Attribute
{
	/// <summary>
	/// Creates a new instance of the <see cref="InfoAttribute"/>, specifying the <see cref="MessageTemplate"/>.
	/// </summary>
	/// <param name="messageTemplate">Specifies the <see cref="MessageTemplate"/>.</param>
	public InfoAttribute(string messageTemplate)
	{
		MessageTemplate = messageTemplate;
	}

	/// <summary>
	/// Creates a new instance of the <see cref="InfoAttribute"/>, specifying the <see cref="EventId"/>.
	/// </summary>
	/// <param name="eventId">Specifies the <see cref="EventId"/>.</param>
	public InfoAttribute(int eventId)
	{
		EventId = eventId;
	}

	/// <summary>
	/// Creates a new instance of the <see cref="InfoAttribute"/>, 
	/// optionally the <see cref="MessageTemplate"/> and <see cref="Name"/>.
	/// </summary>
	/// <param name="messageTemplate">Optionally specifies the <see cref="MessageTemplate"/>.</param>
	/// <param name="name">Optionally specifies the <see cref="Name"/>.</param>
#if NET48_OR_GREATER || PURVIEW_TELEMETRY_NON_NULLABLE
	public InfoAttribute(string messageTemplate = null, string name = null)
#else
	public InfoAttribute(string? messageTemplate = null, string? name = null)
#endif
	{
		MessageTemplate = messageTemplate;
		Name = name;
	}

	/// <summary>
	/// Creates a new instance of the <see cref="InfoAttribute"/>, specifying the <see cref="EventId"/>
	/// and optionally the <see cref="MessageTemplate"/> and <see cref="Name"/>.
	/// </summary>
	/// <param name="eventId">Specifies the <see cref="EventId"/>.</param>
	/// <param name="messageTemplate">Optionally specifies the <see cref="MessageTemplate"/>.</param>
	/// <param name="name">Optionally specifies the <see cref="Name"/>.</param>
#if NET48_OR_GREATER || PURVIEW_TELEMETRY_NON_NULLABLE
	public InfoAttribute(int eventId, string messageTemplate = null, string name = null)
#else
	public InfoAttribute(int eventId, string? messageTemplate = null, string? name = null)
#endif
	{
		MessageTemplate = messageTemplate;
		EventId = eventId;
		Name = name;
	}

	/// <summary>
	/// Optional. The message template used for the log entry, otherwise one is
	/// generated based on the parameters.
	/// </summary>
#if NET48_OR_GREATER || PURVIEW_TELEMETRY_NON_NULLABLE
	public string MessageTemplate { get; set; }
#else
	public string? MessageTemplate { get; set; }
#endif

	/// <summary>
	/// Optional. The event Id for this log entry. If one is not specified, one is automatically generated.
	/// </summary>
	public int? EventId { get; set; }

	/// <summary>
	/// Optional. Gets/ set the name of the log entry. If one is not specified, the method name is used.
	/// </summary>
#if NET48_OR_GREATER || PURVIEW_TELEMETRY_NON_NULLABLE
	public string Name { get; set; }
#else
	public string? Name { get; set; }
#endif

	/// <summary>
	/// Optional. Controls the generation mode for this log method.
	/// <see cref="global::Purview.Telemetry.LoggerGenerationMode.Auto"/> (the default) inherits from the
	/// interface-level <see cref="global::Purview.Telemetry.LoggerAttribute.GenerationMode"/> setting.
	/// </summary>
	public global::Purview.Telemetry.LoggerGenerationMode GenerationMode { get; set; }
}

}
#endif
