#if !EXCLUDE_PURVIEW_TELEMETRY_LOGGING

namespace Purview.Telemetry;

/// <summary>
/// Marker attribute used as an alternative to <see cref="global::Purview.Telemetry.LogAttribute"/>, where the <see cref="global::Purview.Telemetry.LogAttribute.Level"/>
/// is set to <see cref="global::Microsoft.Extensions.Logging.LogLevel.Error"/>.
/// </summary>
{CodeGen}
[global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = false)]
[global::System.Diagnostics.Conditional("PURVIEW_TELEMETRY_ATTRIBUTES")]
[global::System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
sealed class ErrorAttribute : global::System.Attribute
{
	/// <summary>
	/// Creates a new instance of the <see cref="ErrorAttribute"/>, specifying the <see cref="MessageTemplate"/>.
	/// </summary>
	/// <param name="messageTemplate">Specifies the <see cref="MessageTemplate"/>.</param>
	public ErrorAttribute(string messageTemplate)
	{
		MessageTemplate = messageTemplate;
	}

	/// <summary>
	/// Creates a new instance of the <see cref="ErrorAttribute"/>, specifying the <see cref="EventId"/>.
	/// </summary>
	/// <param name="eventId">Specifies the <see cref="EventId"/>.</param>
	public ErrorAttribute(int eventId)
	{
		EventId = eventId;
	}

	/// <summary>
	/// Creates a new instance of the <see cref="ErrorAttribute"/>, 
	/// optionally the <see cref="MessageTemplate"/> and <see cref="Name"/>.
	/// </summary>
	/// <param name="messageTemplate">Optionally specifies the <see cref="MessageTemplate"/>.</param>
	/// <param name="name">Optionally specifies the <see cref="Name"/>.</param>
	public ErrorAttribute(string? messageTemplate = null, string? name = null)
	{
		MessageTemplate = messageTemplate;
		Name = name;
	}

	/// <summary>
	/// Creates a new instance of the <see cref="ErrorAttribute"/>, specifying the <see cref="EventId"/>
	/// and optionally the <see cref="MessageTemplate"/> and <see cref="Name"/>.
	/// </summary>
	/// <param name="eventId">Specifies the <see cref="EventId"/>.</param>
	/// <param name="messageTemplate">Optionally specifies the <see cref="MessageTemplate"/>.</param>
	/// <param name="name">Optionally specifies the <see cref="Name"/>.</param>
	public ErrorAttribute(int eventId, string? messageTemplate = null, string? name = null)
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
	/// Optional. When <see langword="true" />, disables MS logging telemetry generation for this
	/// method, using the v1 (<see cref="global::Microsoft.Extensions.Logging.LoggerMessage.Define{T}"/>)
	/// pattern instead.
	/// When <see langword="false" />, forces v2 generation for this method.
	/// When not set, inherits the interface-level <see cref="global::Purview.Telemetry.LoggerAttribute.DisableMSLoggingTelemetryGeneration"/> setting.
	/// </summary>
	public bool DisableMSLoggingTelemetryGeneration { get; set; }
}

#endif
