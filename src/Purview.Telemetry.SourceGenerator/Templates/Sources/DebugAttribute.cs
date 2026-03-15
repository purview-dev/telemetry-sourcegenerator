#if !EXCLUDE_PURVIEW_TELEMETRY_LOGGING

namespace Purview.Telemetry;

/// <summary>
/// Marker attribute used as an alternative to <see cref="global::Purview.Telemetry.LogAttribute"/>, where the <see cref="global::Purview.Telemetry.LogAttribute.Level"/>
/// is set to <see cref="global::Microsoft.Extensions.Logging.LogLevel.Debug"/>.
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
[global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
sealed class DebugAttribute : global::System.Attribute
{
	/// <summary>
	/// Creates a new instance of the <see cref="DebugAttribute"/>, specifying the <see cref="MessageTemplate"/>.
	/// </summary>
	/// <param name="messageTemplate">Specifies the <see cref="MessageTemplate"/>.</param>
	public DebugAttribute(string messageTemplate)
	{
		MessageTemplate = messageTemplate;
	}

	/// <summary>
	/// Creates a new instance of the <see cref="DebugAttribute"/>, specifying the <see cref="EventId"/>.
	/// </summary>
	/// <param name="eventId">Specifies the <see cref="EventId"/>.</param>
	public DebugAttribute(int eventId)
	{
		EventId = eventId;
	}

	/// <summary>
	/// Creates a new instance of the <see cref="DebugAttribute"/>, 
	/// optionally the <see cref="MessageTemplate"/> and <see cref="Name"/>.
	/// </summary>
	/// <param name="messageTemplate">Optionally specifies the <see cref="MessageTemplate"/>.</param>
	/// <param name="name">Optionally specifies the <see cref="Name"/>.</param>
	public DebugAttribute(string? messageTemplate = null, string? name = null)
	{
		MessageTemplate = messageTemplate;
		Name = name;
	}

	/// <summary>
	/// Creates a new instance of the <see cref="DebugAttribute"/>, specifying the <see cref="EventId"/>
	/// and optionally the <see cref="MessageTemplate"/> and <see cref="Name"/>.
	/// </summary>
	/// <param name="eventId">Specifies the <see cref="EventId"/>.</param>
	/// <param name="messageTemplate">Optionally specifies the <see cref="MessageTemplate"/>.</param>
	/// <param name="name">Optionally specifies the <see cref="Name"/>.</param>
	public DebugAttribute(int eventId, string? messageTemplate = null, string? name = null)
	{
		MessageTemplate = messageTemplate;
		EventId = eventId;
		Name = name;
	}

	/// <summary>
	/// Optional. The message template used for the log entry, otherwise one is
	/// generated based on the parameters.
	/// </summary>
	public string? MessageTemplate { get; set; }

	/// <summary>
	/// Optional. The event Id for this log entry. If one is not specified, one is automatically generated.
	/// </summary>
	public int? EventId { get; set; }

	/// <summary>
	/// Optional. Gets/ set the name of the log entry. If one is not specified, the method name is used.
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// Optional. Controls the generation mode for this log method.
	/// <see cref="global::Purview.Telemetry.LoggerGenerationMode.Auto"/> (the default) inherits from the
	/// interface-level <see cref="global::Purview.Telemetry.LoggerAttribute.GenerationMode"/> setting.
	/// </summary>
	public global::Purview.Telemetry.LoggerGenerationMode GenerationMode { get; set; }
}

#endif
