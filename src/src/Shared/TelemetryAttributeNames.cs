namespace Purview.Telemetry.SourceGenerator;

/// <summary>
/// Attribute and system type identities shared between the source generator and the refactoring
/// providers. This file is linked into both projects so each identity is defined once, while
/// remaining <see langword="internal"/> to each assembly.
/// </summary>
static class TelemetryAttributeNames
{
	public const string PurviewTelemetryNamespace = "Purview.Telemetry";
	public const string SystemDiagnosticsNamespace = "System.Diagnostics";

	public static class Activities
	{
		/// <summary>The <c>[ActivitySource]</c> marker attribute.</summary>
		public static readonly TypeIdentity ActivitySourceAttribute = new(
			"ActivitySourceAttribute",
			PurviewTelemetryNamespace
		);

		/// <summary>The <c>System.Diagnostics.ActivitySource</c> type.</summary>
		public static readonly TypeIdentity ActivitySource = new("ActivitySource", SystemDiagnosticsNamespace);
	}

	public static class Logging
	{
		/// <summary>The <c>[Logger]</c> marker attribute.</summary>
		public static readonly TypeIdentity LoggerAttribute = new("LoggerAttribute", PurviewTelemetryNamespace);

		/// <summary>The <c>Microsoft.Extensions.Logging.ILogger&lt;T&gt;</c> type.</summary>
		public static readonly TypeIdentity ILoggerOfT = new("ILogger", "Microsoft.Extensions.Logging")
		{
			GenericArity = 1,
		};

		/// <summary>The <c>Microsoft.Extensions.Logging.ILogger</c> type.</summary>
		public static readonly TypeIdentity ILogger = new("ILogger", "Microsoft.Extensions.Logging");

		/// <summary>The <c>Microsoft.Extensions.Logging.EventId</c> type.</summary>
		public static readonly TypeIdentity EventId = new("EventId", "Microsoft.Extensions.Logging");

		/// <summary>The <c>Microsoft.Extensions.Logging.LogLevel</c> type.</summary>
		public static readonly TypeIdentity LogLevel = new("LogLevel", "Microsoft.Extensions.Logging");
	}

	public static class Metrics
	{
		public static readonly TypeIdentity MeterAttribute = new("MeterAttribute", PurviewTelemetryNamespace);
		public static readonly TypeIdentity AutoCounterAttribute = new(
			"AutoCounterAttribute",
			PurviewTelemetryNamespace
		);
		public static readonly TypeIdentity CounterAttribute = new("CounterAttribute", PurviewTelemetryNamespace);
		public static readonly TypeIdentity HistogramAttribute = new("HistogramAttribute", PurviewTelemetryNamespace);
		public static readonly TypeIdentity UpDownCounterAttribute = new(
			"UpDownCounterAttribute",
			PurviewTelemetryNamespace
		);

		/// <summary>The <c>System.Diagnostics.Metrics.Counter&lt;T&gt;</c> type.</summary>
		public static readonly TypeIdentity Counter = new("Counter", "System.Diagnostics.Metrics") { GenericArity = 1 };

		/// <summary>The <c>System.Diagnostics.Metrics.Histogram&lt;T&gt;</c> type.</summary>
		public static readonly TypeIdentity Histogram = new("Histogram", "System.Diagnostics.Metrics")
		{
			GenericArity = 1,
		};

		/// <summary>The <c>System.Diagnostics.Metrics.UpDownCounter&lt;T&gt;</c> type.</summary>
		public static readonly TypeIdentity UpDownCounter = new("UpDownCounter", "System.Diagnostics.Metrics")
		{
			GenericArity = 1,
		};
	}

	public static class System
	{
		/// <summary>The <see cref="Exception"/> type.</summary>
		public static readonly TypeIdentity Exception = new("Exception", "System");
	}
}
