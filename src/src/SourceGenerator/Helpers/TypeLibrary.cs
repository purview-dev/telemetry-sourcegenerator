using System.Collections.Concurrent;
using System.Collections.Immutable;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

/// <summary>
/// Registry of the type identities and references used during generation.
/// </summary>
static class TypeLibrary
{
	public static class System
	{
		public static readonly TypeIdentity Void = new("void", null);
		public static readonly TypeIdentity String = PurviewTypeLibrary.System.String;
		public static readonly TypeIdentity Object = PurviewTypeLibrary.System.Object;
		public static readonly TypeIdentity Boolean = PurviewTypeLibrary.System.Boolean;
		public static readonly TypeIdentity Int32 = PurviewTypeLibrary.System.Int32;
		public static readonly TypeIdentity Int64 = PurviewTypeLibrary.System.Int64;
		public static readonly TypeIdentity Exception = TypeIdentity.Create<Exception>();
		public static readonly TypeIdentity IDisposable = TypeIdentity.Create<IDisposable>();
		public static readonly TypeIdentity DateTimeOffset = TypeIdentity.Create<DateTimeOffset>();
		public static readonly TypeIdentity Func = new(nameof(Func), "System");
		public static readonly TypeIdentity Action = new(nameof(Action), "System");
		public static readonly TypeIdentity IEnumerable = TypeIdentity.Create<global::System.Collections.IEnumerable>();
		public static readonly TypeIdentity GenericIEnumerable = new(typeof(IEnumerable<>));
		public static readonly TypeIdentity List = new(typeof(List<>));
		public static readonly TypeIdentity Dictionary = new(typeof(Dictionary<,>));
		public static readonly TypeIdentity ConcurrentDictionary = new(typeof(ConcurrentDictionary<,>));
		public static readonly TypeIdentity TagList = new("TagList", "System.Diagnostics");
	}

	public static class Activities
	{
		public static class SystemDiagnostics
		{
			public static readonly TypeIdentity Activity = new("Activity", "System.Diagnostics");
			public static readonly TypeIdentity ActivitySource = new("ActivitySource", "System.Diagnostics");
			public static readonly TypeIdentity ActivityEvent = new("ActivityEvent", "System.Diagnostics");
			public static readonly TypeIdentity ActivityContext = new("ActivityContext", "System.Diagnostics");
			public static readonly TypeIdentity ActivityKind = new("ActivityKind", "System.Diagnostics");
			public static readonly TypeIdentity ActivityStatusCode = new("ActivityStatusCode", "System.Diagnostics");
			public static readonly TypeIdentity ActivityTagsCollection = new(
				"ActivityTagsCollection",
				"System.Diagnostics"
			);
			public static readonly TypeIdentity ActivityLink = new("ActivityLink", "System.Diagnostics");

			public static readonly TypeReference ActivityTagIEnumerable = System.GenericIEnumerable.MakeGeneric(
				new TypeReference(
					new TypeIdentity("KeyValuePair", "System.Collections.Generic").MakeGeneric(
						System.String.AsTypeReference(),
						System.Object.AsTypeReference().Nullable()
					)
				)
			);

			public static readonly TypeReference ActivityLinkIEnumerable = System.GenericIEnumerable.MakeGeneric(
				ActivityLink.AsTypeReference()
			);

			public static readonly TypeReference ActivityLinkArray = new TypeReference(ActivityLink).MakeArray();
		}

		public static readonly TypeIdentity ActivitySourceGenerationAttribute = new(
			"ActivitySourceGenerationAttribute",
			"Purview.Telemetry"
		);
		public static readonly TypeIdentity ActivitySourceAttribute = new(
			"ActivitySourceAttribute",
			"Purview.Telemetry"
		);
		public static readonly TypeIdentity ActivityAttribute = new("ActivityAttribute", "Purview.Telemetry");
		public static readonly TypeIdentity EventAttribute = new("EventAttribute", "Purview.Telemetry");
		public static readonly TypeIdentity ContextAttribute = new("ContextAttribute", "Purview.Telemetry");
		public static readonly TypeIdentity BaggageAttribute = new("BaggageAttribute", "Purview.Telemetry");
		public static readonly TypeIdentity EscapeAttribute = new("EscapeAttribute", "Purview.Telemetry");
		public static readonly TypeIdentity StatusDescriptionAttribute = new(
			"StatusDescriptionAttribute",
			"Purview.Telemetry"
		);
	}

	public static class Logging
	{
		public static class MicrosoftExtensions
		{
			public static readonly TypeIdentity ILogger = new("ILogger", "Microsoft.Extensions.Logging");
			public static readonly TypeIdentity LoggerMessage = new("LoggerMessage", "Microsoft.Extensions.Logging");
			public static readonly TypeIdentity LogLevel = new("LogLevel", "Microsoft.Extensions.Logging");
			public static readonly TypeIdentity EventId = new("EventId", "Microsoft.Extensions.Logging");
			public static readonly TypeIdentity LoggerMessageHelper = new(
				"LoggerMessageHelper",
				"Microsoft.Extensions.Logging"
			);
			public static readonly TypeIdentity LogPropertiesAttribute = new(
				"LogPropertiesAttribute",
				"Microsoft.Extensions.Logging"
			);
			public static readonly TypeIdentity LogPropertyIgnoreAttribute = new(
				"LogPropertyIgnoreAttribute",
				"Microsoft.Extensions.Logging"
			);
		}

		public static readonly TypeIdentity LoggerGenerationAttribute = new(
			"LoggerGenerationAttribute",
			"Purview.Telemetry"
		);
		public static readonly TypeIdentity LoggerAttribute = new("LoggerAttribute", "Purview.Telemetry");
		public static readonly TypeIdentity LogAttribute = new("LogAttribute", "Purview.Telemetry");
		public static readonly TypeIdentity LogPrefixType = new("LogPrefixType", "Purview.Telemetry");
		public static readonly TypeIdentity LoggerGenerationMode = new("LoggerGenerationMode", "Purview.Telemetry");
		public static readonly TypeIdentity ExpandEnumerableAttribute = new(
			"ExpandEnumerableAttribute",
			"Purview.Telemetry"
		);
		public static readonly TypeIdentity TraceAttribute = new("TraceAttribute", "Purview.Telemetry");
		public static readonly TypeIdentity DebugAttribute = new("DebugAttribute", "Purview.Telemetry");
		public static readonly TypeIdentity InfoAttribute = new("InfoAttribute", "Purview.Telemetry");
		public static readonly TypeIdentity WarningAttribute = new("WarningAttribute", "Purview.Telemetry");
		public static readonly TypeIdentity ErrorAttribute = new("ErrorAttribute", "Purview.Telemetry");
		public static readonly TypeIdentity CriticalAttribute = new("CriticalAttribute", "Purview.Telemetry");
	}

	public static class Metrics
	{
		public static class SystemDiagnostics
		{
			public static readonly TypeIdentity Meter = new("Meter", "System.Diagnostics.Metrics");
			public static readonly TypeIdentity IMeterFactory = new("IMeterFactory", "System.Diagnostics.Metrics");
			public static readonly TypeIdentity MeterOptions = new("MeterOptions", "System.Diagnostics.Metrics");
			public static readonly TypeIdentity Measurement = new("Measurement", "System.Diagnostics.Metrics")
			{
				GenericArity = 1,
			};
			public static readonly TypeIdentity Counter = new("Counter", "System.Diagnostics.Metrics");
			public static readonly TypeIdentity UpDownCounter = new("UpDownCounter", "System.Diagnostics.Metrics");
			public static readonly TypeIdentity Histogram = new("Histogram", "System.Diagnostics.Metrics");
			public static readonly TypeIdentity ObservableCounter = new(
				"ObservableCounter",
				"System.Diagnostics.Metrics"
			);
			public static readonly TypeIdentity ObservableGauge = new("ObservableGauge", "System.Diagnostics.Metrics");
			public static readonly TypeIdentity ObservableUpDownCounter = new(
				"ObservableUpDownCounter",
				"System.Diagnostics.Metrics"
			);
		}

		public static readonly TypeIdentity MeterGenerationAttribute = new(
			"MeterGenerationAttribute",
			"Purview.Telemetry"
		);
		public static readonly TypeIdentity MeterAttribute = new("MeterAttribute", "Purview.Telemetry");
		public static readonly TypeIdentity MeterNameGenerationType = new(
			"MeterNameGenerationType",
			"Purview.Telemetry"
		);
		public static readonly TypeIdentity InstrumentMeasurementAttribute = new(
			"InstrumentMeasurementAttribute",
			"Purview.Telemetry"
		);
		public static readonly TypeIdentity AutoCounterAttribute = new("AutoCounterAttribute", "Purview.Telemetry");
		public static readonly TypeIdentity CounterAttribute = new("CounterAttribute", "Purview.Telemetry");
		public static readonly TypeIdentity UpDownCounterAttribute = new("UpDownCounterAttribute", "Purview.Telemetry");
		public static readonly TypeIdentity HistogramAttribute = new("HistogramAttribute", "Purview.Telemetry");
		public static readonly TypeIdentity ObservableCounterAttribute = new(
			"ObservableCounterAttribute",
			"Purview.Telemetry"
		);
		public static readonly TypeIdentity ObservableUpDownCounterAttribute = new(
			"ObservableUpDownCounterAttribute",
			"Purview.Telemetry"
		);
		public static readonly TypeIdentity ObservableGaugeAttribute = new(
			"ObservableGaugeAttribute",
			"Purview.Telemetry"
		);

		public static readonly ImmutableDictionary<InstrumentTypes, TypeIdentity> InstrumentTypeMap = new Dictionary<
			InstrumentTypes,
			TypeIdentity
		>
		{
			{ InstrumentTypes.Counter, SystemDiagnostics.Counter },
			{ InstrumentTypes.UpDownCounter, SystemDiagnostics.UpDownCounter },
			{ InstrumentTypes.Histogram, SystemDiagnostics.Histogram },
			{ InstrumentTypes.ObservableCounter, SystemDiagnostics.ObservableCounter },
			{ InstrumentTypes.ObservableGauge, SystemDiagnostics.ObservableGauge },
			{ InstrumentTypes.ObservableUpDownCounter, SystemDiagnostics.ObservableUpDownCounter },
		}.ToImmutableDictionary();
	}

	public static class DependencyInjection
	{
		public static readonly TypeIdentity IServiceCollection = new(
			"IServiceCollection",
			"Microsoft.Extensions.DependencyInjection"
		);
		public static readonly TypeIdentity ServiceDescriptor = new(
			"ServiceDescriptor",
			"Microsoft.Extensions.DependencyInjection"
		);
		public static readonly TypeIdentity ServiceLifetime = new(
			"ServiceLifetime",
			"Microsoft.Extensions.DependencyInjection"
		);
	}

	public static class Telemetry
	{
		public static readonly TypeIdentity TagAttribute = new("TagAttribute", "Purview.Telemetry");
		public static readonly TypeIdentity ExcludeAttribute = new("ExcludeAttribute", "Purview.Telemetry");
		public static readonly TypeIdentity TelemetryGenerationAttribute = new(
			"TelemetryGenerationAttribute",
			"Purview.Telemetry"
		);
		public static readonly TypeIdentity TargetsEnum = new("Targets", "Purview.Telemetry");
		public static readonly TypeIdentity NamingConventionEnum = new("NamingConvention", "Purview.Telemetry");
		public static readonly TypeIdentity ExcludeTargetsAttribute = new(
			"ExcludeTargetsAttribute",
			"Purview.Telemetry"
		);
	}
}
