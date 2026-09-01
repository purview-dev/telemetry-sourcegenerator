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
			nameof(ActivitySourceAttribute),
			PurviewTelemetryNamespace
		);

		/// <summary>The <c>System.Diagnostics.ActivitySource</c> type.</summary>
		public static readonly TypeIdentity ActivitySource = new(nameof(ActivitySource), SystemDiagnosticsNamespace);
	}

	public static class Logging
	{
		public const string MicrosoftExtensionsLoggingNamespace = "Microsoft.Extensions.Logging";

		/// <summary>The <c>[Logger]</c> marker attribute.</summary>
		public static readonly TypeIdentity LoggerAttribute = new(nameof(LoggerAttribute), PurviewTelemetryNamespace);

		/// <summary>The <c>Microsoft.Extensions.Logging.ILogger&lt;T&gt;</c> type.</summary>
		public static readonly TypeIdentity ILoggerOfT = new(nameof(ILogger), MicrosoftExtensionsLoggingNamespace)
		{
			GenericArity = 1,
		};

		/// <summary>The <c>Microsoft.Extensions.Logging.ILogger</c> type.</summary>
		public static readonly TypeIdentity ILogger = new(nameof(ILogger), MicrosoftExtensionsLoggingNamespace);

		/// <summary>The <c>Microsoft.Extensions.Logging.EventId</c> type.</summary>
		public static readonly TypeIdentity EventId = new(nameof(EventId), MicrosoftExtensionsLoggingNamespace);

		/// <summary>The <c>Microsoft.Extensions.Logging.LogLevel</c> type.</summary>
		public static readonly TypeIdentity LogLevel = new(nameof(LogLevel), MicrosoftExtensionsLoggingNamespace);
	}

	public static class Metrics
	{
		public const string SystemDiagnosticsMetricsNamespace = "System.Diagnostics.Metrics";

		public static readonly TypeIdentity MeterAttribute = new(nameof(MeterAttribute), PurviewTelemetryNamespace);
		public static readonly TypeIdentity AutoCounterAttribute = new(
			nameof(AutoCounterAttribute),
			PurviewTelemetryNamespace
		);
		public static readonly TypeIdentity CounterAttribute = new(nameof(CounterAttribute), PurviewTelemetryNamespace);
		public static readonly TypeIdentity HistogramAttribute = new(
			nameof(HistogramAttribute),
			PurviewTelemetryNamespace
		);
		public static readonly TypeIdentity UpDownCounterAttribute = new(
			nameof(UpDownCounterAttribute),
			PurviewTelemetryNamespace
		);

		/// <summary>The <c>System.Diagnostics.Metrics.Counter&lt;T&gt;</c> type.</summary>
		public static readonly TypeIdentity Counter = new(nameof(Counter), SystemDiagnosticsMetricsNamespace)
		{
			GenericArity = 1,
		};

		/// <summary>The <c>System.Diagnostics.Metrics.Histogram&lt;T&gt;</c> type.</summary>
		public static readonly TypeIdentity Histogram = new(nameof(Histogram), SystemDiagnosticsMetricsNamespace)
		{
			GenericArity = 1,
		};

		/// <summary>The <c>System.Diagnostics.Metrics.UpDownCounter&lt;T&gt;</c> type.</summary>
		public static readonly TypeIdentity UpDownCounter = new(
			nameof(UpDownCounter),
			SystemDiagnosticsMetricsNamespace
		)
		{
			GenericArity = 1,
		};
	}

	public static class System
	{
		/// <summary>The <see cref="Exception"/> type.</summary>
		public static readonly TypeIdentity Exception = TypeIdentity.Create<Exception>();
	}
}
