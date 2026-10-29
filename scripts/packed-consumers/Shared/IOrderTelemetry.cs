using System.Diagnostics;
using Purview.Telemetry;

// .NET 11: every consumer assembly - and so IOrderTelemetry - creates its activity sources through the injected
// System.Diagnostics.ActivitySourceFactory, registered by services.AddTracing(). PackedTelemetry.Library does not opt
// in, so each application also runs a generated static ActivitySource, and the gate checks both shapes (trimmed,
// Native AOT and WebAssembly AOT).
[assembly: ActivitySourceGeneration("packed.orders", UseActivitySourceFactory = true)]

namespace PackedTelemetry;

/// <summary>
/// One interface, all three targets: compiled into every consumer, so each one runs the packed generator itself.
/// </summary>
/// <remarks>
/// Names are explicit wherever the generator lets them be, so the assertions in <see cref="Expectations"/> follow
/// the attributes rather than a naming convention. The tag and baggage keys are left to the default
/// (OpenTelemetry snake_case) convention on purpose: that transformation is part of what ships.
/// </remarks>
[ActivitySource("packed.orders")]
[Logger]
[Meter("packed.orders")]
public interface IOrderTelemetry
{
	/// <summary>Multi-target: starts an Activity, logs at Information and increments a counter.</summary>
	[Activity("order.place")]
	[Info]
	[AutoCounter("orders.placed")]
	Activity? PlacingOrder([Tag] string orderId, [Baggage] string customerId);

	/// <summary>
	/// Multi-target: adds an ActivityEvent and logs at Debug. The consumers enable logging from Information only,
	/// so the event must be recorded while the log entry must not be.
	/// </summary>
	[Event("order.validated")]
	[Debug]
	void OrderValidated(Activity? activity, [Tag] int lineCount);

	[Histogram("order.total", Unit = "GBP")]
	void OrderTotal([InstrumentMeasurement] double total, [Tag] string currency);

	[UpDownCounter("orders.in_flight")]
	void OrdersInFlight([InstrumentMeasurement] int delta);

	/// <summary>Forces the state-based (v2) log generation, with an expanded array.</summary>
	[Info(GenerationMode = LoggerGenerationMode.V2)]
	void OrderShipped(string orderId, [ExpandEnumerable] string[] carriers);

	/// <summary>
	/// Multi-target: records the exception on the Activity (OpenTelemetry exception rules) and logs it at Error.
	/// </summary>
	[Event("exception")]
	[Error]
	void OrderFailed(Activity? activity, Exception exception, [Tag] string orderId);
}
