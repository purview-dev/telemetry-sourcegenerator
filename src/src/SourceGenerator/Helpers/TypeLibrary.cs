using System.Collections.Concurrent;
using System.Collections.Immutable;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

/// <summary>
/// Registry of the type identities and references used during generation.
/// </summary>
static class TypeLibrary
{
	public const string PurviewTelemetryNamespace = "Purview.Telemetry";

	public static class System
	{
		public static readonly TypeIdentity Exception = TypeIdentity.Create<Exception>();
		public static readonly TypeIdentity IDisposable = TypeIdentity.Create<IDisposable>();
		public static readonly TypeIdentity DateTimeOffset = TypeIdentity.Create<DateTimeOffset>();
		public static readonly TypeIdentity IEnumerable = TypeIdentity.Create<global::System.Collections.IEnumerable>();
		public static readonly TypeIdentity GenericIEnumerable = new(typeof(IEnumerable<>));
		public static readonly TypeIdentity List = new(typeof(List<>));
		public static readonly TypeIdentity Dictionary = new(typeof(Dictionary<,>));
		public static readonly TypeIdentity ConcurrentDictionary = new(typeof(ConcurrentDictionary<,>));
		public static readonly TypeIdentity TagList = new(nameof(TagList), "System.Diagnostics");
	}

	public static class Activities
	{
		public static class SystemDiagnostics
		{
			public const string DiagnosticsNamespace = "System.Diagnostics";

			public static readonly TypeIdentity Activity = new(nameof(Activity), DiagnosticsNamespace);
			public static readonly TypeIdentity ActivitySource = new(nameof(ActivitySource), DiagnosticsNamespace);
			public static readonly TypeIdentity ActivityEvent = new(nameof(ActivityEvent), DiagnosticsNamespace);
			public static readonly TypeIdentity ActivityContext = new(nameof(ActivityContext), DiagnosticsNamespace);
			public static readonly TypeIdentity ActivityKind = new(nameof(ActivityKind), DiagnosticsNamespace);
			public static readonly TypeIdentity ActivityStatusCode = new(
				nameof(ActivityStatusCode),
				DiagnosticsNamespace
			);
			public static readonly TypeIdentity ActivityTagsCollection = new(
				nameof(ActivityTagsCollection),
				DiagnosticsNamespace
			);
			public static readonly TypeIdentity ActivityLink = new(nameof(ActivityLink), DiagnosticsNamespace);

			public static readonly TypeReference ActivityTagIEnumerable = System.GenericIEnumerable.MakeGeneric(
				new TypeReference(
					new TypeIdentity("KeyValuePair", "System.Collections.Generic").MakeGeneric(
						PurviewTypeLibrary.System.String.AsTypeReference(),
						PurviewTypeLibrary.System.Object.AsTypeReference()
					)
				)
			);

			public static readonly TypeReference ActivityLinkIEnumerable = System.GenericIEnumerable.MakeGeneric(
				ActivityLink.AsTypeReference()
			);

			public static readonly TypeReference ActivityLinkArray = new TypeReference(ActivityLink).MakeArray();
		}

		public static readonly TypeIdentity ActivitySourceGenerationAttribute = new(
			nameof(ActivitySourceGenerationAttribute),
			PurviewTelemetryNamespace
		);
		public static readonly TypeIdentity ActivitySourceAttribute = new(
			nameof(ActivitySourceAttribute),
			PurviewTelemetryNamespace
		);
		public static readonly TypeIdentity ActivityAttribute = new(
			nameof(ActivityAttribute),
			PurviewTelemetryNamespace
		);
		public static readonly TypeIdentity EventAttribute = new(nameof(EventAttribute), PurviewTelemetryNamespace);
		public static readonly TypeIdentity ContextAttribute = new(nameof(ContextAttribute), PurviewTelemetryNamespace);
		public static readonly TypeIdentity BaggageAttribute = new(nameof(BaggageAttribute), PurviewTelemetryNamespace);
		public static readonly TypeIdentity EscapeAttribute = new(nameof(EscapeAttribute), PurviewTelemetryNamespace);
		public static readonly TypeIdentity StatusDescriptionAttribute = new(
			nameof(StatusDescriptionAttribute),
			PurviewTelemetryNamespace
		);

		public static ImmutableArray<TypeIdentity> GetGeneratedTypes() =>
			[
				ActivitySourceGenerationAttribute,
				ActivitySourceAttribute,
				ActivityAttribute,
				EventAttribute,
				ContextAttribute,
				BaggageAttribute,
				EscapeAttribute,
				StatusDescriptionAttribute,
			];
	}

	public static class Logging
	{
		public static class MicrosoftExtensions
		{
			public const string LoggingNamespace = "Microsoft.Extensions.Logging";

			public static readonly TypeIdentity ILogger = new(nameof(ILogger), LoggingNamespace);
			public static readonly TypeIdentity LoggerMessage = new(nameof(LoggerMessage), LoggingNamespace);
			public static readonly TypeIdentity LogLevel = new(nameof(LogLevel), LoggingNamespace);
			public static readonly TypeIdentity EventId = new(nameof(EventId), LoggingNamespace);
			public static readonly TypeIdentity LoggerMessageHelper = new(
				nameof(LoggerMessageHelper),
				LoggingNamespace
			);
			public static readonly TypeIdentity LogPropertiesAttribute = new(
				nameof(LogPropertiesAttribute),
				LoggingNamespace
			);
			public static readonly TypeIdentity LogPropertyIgnoreAttribute = new(
				nameof(LogPropertyIgnoreAttribute),
				LoggingNamespace
			);
		}

		public static readonly TypeIdentity LoggerGenerationAttribute = new(
			nameof(LoggerGenerationAttribute),
			PurviewTelemetryNamespace
		);
		public static readonly TypeIdentity LoggerAttribute = new(nameof(LoggerAttribute), PurviewTelemetryNamespace);
		public static readonly TypeIdentity LogAttribute = new(nameof(LogAttribute), PurviewTelemetryNamespace);
		public static readonly TypeIdentity LogPrefixType = new(nameof(LogPrefixType), PurviewTelemetryNamespace);
		public static readonly TypeIdentity LoggerGenerationMode = new(
			nameof(LoggerGenerationMode),
			PurviewTelemetryNamespace
		);
		public static readonly TypeIdentity ExpandEnumerableAttribute = new(
			nameof(ExpandEnumerableAttribute),
			PurviewTelemetryNamespace
		);
		public static readonly TypeIdentity TraceAttribute = new(nameof(TraceAttribute), PurviewTelemetryNamespace);
		public static readonly TypeIdentity DebugAttribute = new(nameof(DebugAttribute), PurviewTelemetryNamespace);
		public static readonly TypeIdentity InfoAttribute = new(nameof(InfoAttribute), PurviewTelemetryNamespace);
		public static readonly TypeIdentity WarningAttribute = new(nameof(WarningAttribute), PurviewTelemetryNamespace);
		public static readonly TypeIdentity ErrorAttribute = new(nameof(ErrorAttribute), PurviewTelemetryNamespace);
		public static readonly TypeIdentity CriticalAttribute = new(
			nameof(CriticalAttribute),
			PurviewTelemetryNamespace
		);

		public static readonly ImmutableArray<TypeIdentity> LogAttributeTargets =
		[
			LogAttribute,
			TraceAttribute,
			DebugAttribute,
			InfoAttribute,
			WarningAttribute,
			ErrorAttribute,
			CriticalAttribute,
		];

		public static ImmutableArray<TypeIdentity> GetGeneratedTypes() =>
			[
				LoggerGenerationAttribute,
				LoggerAttribute,
				LogAttribute,
				LogPrefixType,
				LoggerGenerationMode,
				ExpandEnumerableAttribute,
				TraceAttribute,
				DebugAttribute,
				InfoAttribute,
				WarningAttribute,
				ErrorAttribute,
				CriticalAttribute,
			];

		public static ImmutableDictionary<TypeIdentity, LogLevelDetails> LogLevelMap = new Dictionary<
			TypeIdentity,
			LogLevelDetails
		>
		{
			{ TraceAttribute, new(TraceAttribute, 0, "Trace") },
			{ DebugAttribute, new(DebugAttribute, 1, "Debug") },
			{ InfoAttribute, new(InfoAttribute, 2, "Info") },
			{ WarningAttribute, new(WarningAttribute, 3, "Warning") },
			{ ErrorAttribute, new(ErrorAttribute, 4, "Error") },
			{ CriticalAttribute, new(CriticalAttribute, 5, "Critical") },
		}.ToImmutableDictionary();
	}

	public static class Metrics
	{
		public static class SystemDiagnostics
		{
			public const string SystemDiagnosticsMetricsNamespace = "System.Diagnostics.Metrics";

			public static readonly TypeIdentity Meter = new(nameof(Meter), SystemDiagnosticsMetricsNamespace);
			public static readonly TypeIdentity IMeterFactory = new(
				nameof(IMeterFactory),
				SystemDiagnosticsMetricsNamespace
			);
			public static readonly TypeIdentity MeterOptions = new(
				nameof(MeterOptions),
				SystemDiagnosticsMetricsNamespace
			);
			public static readonly TypeIdentity Measurement = new(
				nameof(Measurement),
				SystemDiagnosticsMetricsNamespace
			)
			{
				GenericArity = 1,
			};
			public static readonly TypeIdentity Counter = new(nameof(Counter), SystemDiagnosticsMetricsNamespace);
			public static readonly TypeIdentity UpDownCounter = new(
				nameof(UpDownCounter),
				SystemDiagnosticsMetricsNamespace
			);
			public static readonly TypeIdentity Histogram = new(nameof(Histogram), SystemDiagnosticsMetricsNamespace);
			public static readonly TypeIdentity ObservableCounter = new(
				nameof(ObservableCounter),
				SystemDiagnosticsMetricsNamespace
			);
			public static readonly TypeIdentity ObservableGauge = new(
				nameof(ObservableGauge),
				SystemDiagnosticsMetricsNamespace
			);
			public static readonly TypeIdentity ObservableUpDownCounter = new(
				nameof(ObservableUpDownCounter),
				SystemDiagnosticsMetricsNamespace
			);
		}

		public static readonly TypeIdentity MeterGenerationAttribute = new(
			nameof(MeterGenerationAttribute),
			PurviewTelemetryNamespace
		);
		public static readonly TypeIdentity MeterAttribute = new(nameof(MeterAttribute), PurviewTelemetryNamespace);
		public static readonly TypeIdentity MeterNameGenerationType = new(
			nameof(MeterNameGenerationType),
			PurviewTelemetryNamespace
		);
		public static readonly TypeIdentity InstrumentMeasurementAttribute = new(
			nameof(InstrumentMeasurementAttribute),
			PurviewTelemetryNamespace
		);
		public static readonly TypeIdentity AutoCounterAttribute = new(
			nameof(AutoCounterAttribute),
			PurviewTelemetryNamespace
		);
		public static readonly TypeIdentity CounterAttribute = new(nameof(CounterAttribute), PurviewTelemetryNamespace);
		public static readonly TypeIdentity UpDownCounterAttribute = new(
			nameof(UpDownCounterAttribute),
			PurviewTelemetryNamespace
		);
		public static readonly TypeIdentity HistogramAttribute = new(
			nameof(HistogramAttribute),
			PurviewTelemetryNamespace
		);
		public static readonly TypeIdentity ObservableCounterAttribute = new(
			nameof(ObservableCounterAttribute),
			PurviewTelemetryNamespace
		);
		public static readonly TypeIdentity ObservableUpDownCounterAttribute = new(
			nameof(ObservableUpDownCounterAttribute),
			PurviewTelemetryNamespace
		);
		public static readonly TypeIdentity ObservableGaugeAttribute = new(
			nameof(ObservableGaugeAttribute),
			PurviewTelemetryNamespace
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

		public static ImmutableArray<TypeIdentity> GetGeneratedTypes() =>
			[
				MeterGenerationAttribute,
				MeterAttribute,
				MeterNameGenerationType,
				InstrumentMeasurementAttribute,
				AutoCounterAttribute,
				CounterAttribute,
				UpDownCounterAttribute,
				HistogramAttribute,
				ObservableCounterAttribute,
				ObservableUpDownCounterAttribute,
				ObservableGaugeAttribute,
			];
	}

	public static class DependencyInjection
	{
		public const string DependencyInjectionNamespace = "Microsoft.Extensions.DependencyInjection";

		public static readonly TypeIdentity IServiceCollection = new(
			nameof(IServiceCollection),
			DependencyInjectionNamespace
		);
		public static readonly TypeIdentity ServiceDescriptor = new(
			nameof(ServiceDescriptor),
			DependencyInjectionNamespace
		);
		public static readonly TypeIdentity ServiceLifetime = new(
			nameof(ServiceLifetime),
			DependencyInjectionNamespace
		);
	}

	public static class TelemetryShared
	{
		public static readonly TypeIdentity TagAttribute = new(nameof(TagAttribute), PurviewTelemetryNamespace);
		public static readonly TypeIdentity ExcludeAttribute = new(nameof(ExcludeAttribute), PurviewTelemetryNamespace);
		public static readonly TypeIdentity TelemetryGenerationAttribute = new(
			nameof(TelemetryGenerationAttribute),
			PurviewTelemetryNamespace
		);
		public static readonly TypeIdentity Targets = new(nameof(Targets), PurviewTelemetryNamespace);
		public static readonly TypeIdentity NamingConvention = new(nameof(NamingConvention), PurviewTelemetryNamespace);
		public static readonly TypeIdentity ExcludeTargetsAttribute = new(
			nameof(ExcludeTargetsAttribute),
			PurviewTelemetryNamespace
		);

		public static ImmutableArray<TypeIdentity> GetGeneratedTypes() =>
			[
				TagAttribute,
				ExcludeAttribute,
				TelemetryGenerationAttribute,
				Targets,
				NamingConvention,
				ExcludeTargetsAttribute,
			];
	}
}
