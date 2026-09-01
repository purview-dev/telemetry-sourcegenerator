using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Helpers;

/// <summary>
/// Registry of the shipped marker-attribute templates injected into consuming compilations.
/// </summary>
static class TemplateLibrary
{
	public static class Shared
	{
		public static readonly TemplateInfo TagAttribute = TemplateInfo.Create(TypeLibrary.Telemetry.TagAttribute);
		public static readonly TemplateInfo ExcludeAttribute = TemplateInfo.Create(
			TypeLibrary.Telemetry.ExcludeAttribute
		);
		public static readonly TemplateInfo TelemetryGenerationAttribute = TemplateInfo.Create(
			TypeLibrary.Telemetry.TelemetryGenerationAttribute
		);
		public static readonly TemplateInfo TargetsEnum = TemplateInfo.Create(TypeLibrary.Telemetry.TargetsEnum);
		public static readonly TemplateInfo NamingConventionEnum = TemplateInfo.Create(
			TypeLibrary.Telemetry.NamingConventionEnum
		);
		public static readonly TemplateInfo ExcludeTargetsAttribute = TemplateInfo.Create(
			TypeLibrary.Telemetry.ExcludeTargetsAttribute
		);

		public static TemplateInfo[] GetTemplates() =>
			[
				TagAttribute,
				ExcludeAttribute,
				TelemetryGenerationAttribute,
				TargetsEnum,
				NamingConventionEnum,
				ExcludeTargetsAttribute,
			];
	}

	public static class Activities
	{
		public static readonly TemplateInfo ActivitySourceGenerationAttribute = TemplateInfo.Create(
			TypeLibrary.Activities.ActivitySourceGenerationAttribute
		);
		public static readonly TemplateInfo ActivitySourceAttribute = TemplateInfo.Create(
			TypeLibrary.Activities.ActivitySourceAttribute
		);
		public static readonly TemplateInfo ActivityAttribute = TemplateInfo.Create(
			TypeLibrary.Activities.ActivityAttribute
		);
		public static readonly TemplateInfo EventAttribute = TemplateInfo.Create(TypeLibrary.Activities.EventAttribute);
		public static readonly TemplateInfo ContextAttribute = TemplateInfo.Create(
			TypeLibrary.Activities.ContextAttribute
		);
		public static readonly TemplateInfo BaggageAttribute = TemplateInfo.Create(
			TypeLibrary.Activities.BaggageAttribute
		);
		public static readonly TemplateInfo EscapeAttribute = TemplateInfo.Create(
			TypeLibrary.Activities.EscapeAttribute
		);
		public static readonly TemplateInfo StatusDescriptionAttribute = TemplateInfo.Create(
			TypeLibrary.Activities.StatusDescriptionAttribute
		);

		public static TemplateInfo[] GetTemplates() =>
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
		public static readonly TemplateInfo LoggerGenerationAttribute = TemplateInfo.Create(
			TypeLibrary.Logging.LoggerGenerationAttribute
		);
		public static readonly TemplateInfo LoggerAttribute = TemplateInfo.Create(TypeLibrary.Logging.LoggerAttribute);
		public static readonly TemplateInfo LogAttribute = TemplateInfo.Create(TypeLibrary.Logging.LogAttribute);
		public static readonly TemplateInfo LogPrefixType = TemplateInfo.Create(TypeLibrary.Logging.LogPrefixType);
		public static readonly TemplateInfo LoggerGenerationMode = TemplateInfo.Create(
			TypeLibrary.Logging.LoggerGenerationMode
		);
		public static readonly TemplateInfo ExpandEnumerableAttribute = TemplateInfo.Create(
			TypeLibrary.Logging.ExpandEnumerableAttribute
		);
		public static readonly TemplateInfo TraceAttribute = TemplateInfo.Create(TypeLibrary.Logging.TraceAttribute);
		public static readonly TemplateInfo DebugAttribute = TemplateInfo.Create(TypeLibrary.Logging.DebugAttribute);
		public static readonly TemplateInfo InfoAttribute = TemplateInfo.Create(TypeLibrary.Logging.InfoAttribute);
		public static readonly TemplateInfo WarningAttribute = TemplateInfo.Create(
			TypeLibrary.Logging.WarningAttribute
		);
		public static readonly TemplateInfo ErrorAttribute = TemplateInfo.Create(TypeLibrary.Logging.ErrorAttribute);
		public static readonly TemplateInfo CriticalAttribute = TemplateInfo.Create(
			TypeLibrary.Logging.CriticalAttribute
		);

		public static readonly TemplateInfo[] SpecificLogAttributes =
		[
			TraceAttribute,
			DebugAttribute,
			InfoAttribute,
			WarningAttribute,
			ErrorAttribute,
			CriticalAttribute,
		];

		public static readonly Dictionary<TemplateInfo, int> SpecificLogAttributesToLevel = new()
		{
			{ TraceAttribute, 0 },
			{ DebugAttribute, 1 },
			{ InfoAttribute, 2 },
			{ WarningAttribute, 3 },
			{ ErrorAttribute, 4 },
			{ CriticalAttribute, 5 },
		};

		public static TemplateInfo[] GetTemplates() =>
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
	}

	public static class Metrics
	{
		public static readonly TemplateInfo MeterGenerationAttribute = TemplateInfo.Create(
			TypeLibrary.Metrics.MeterGenerationAttribute
		);
		public static readonly TemplateInfo MeterAttribute = TemplateInfo.Create(TypeLibrary.Metrics.MeterAttribute);
		public static readonly TemplateInfo MeterNameGenerationType = TemplateInfo.Create(
			TypeLibrary.Metrics.MeterNameGenerationType
		);
		public static readonly TemplateInfo InstrumentMeasurementAttribute = TemplateInfo.Create(
			TypeLibrary.Metrics.InstrumentMeasurementAttribute
		);
		public static readonly TemplateInfo AutoCounterAttribute = TemplateInfo.Create(
			TypeLibrary.Metrics.AutoCounterAttribute
		);
		public static readonly TemplateInfo CounterAttribute = TemplateInfo.Create(
			TypeLibrary.Metrics.CounterAttribute
		);
		public static readonly TemplateInfo UpDownCounterAttribute = TemplateInfo.Create(
			TypeLibrary.Metrics.UpDownCounterAttribute
		);
		public static readonly TemplateInfo HistogramAttribute = TemplateInfo.Create(
			TypeLibrary.Metrics.HistogramAttribute
		);
		public static readonly TemplateInfo ObservableCounterAttribute = TemplateInfo.Create(
			TypeLibrary.Metrics.ObservableCounterAttribute
		);
		public static readonly TemplateInfo ObservableUpDownCounterAttribute = TemplateInfo.Create(
			TypeLibrary.Metrics.ObservableUpDownCounterAttribute
		);
		public static readonly TemplateInfo ObservableGaugeAttribute = TemplateInfo.Create(
			TypeLibrary.Metrics.ObservableGaugeAttribute
		);

		public static readonly TemplateInfo[] ValidInstrumentAttributes =
		[
			AutoCounterAttribute,
			CounterAttribute,
			UpDownCounterAttribute,
			HistogramAttribute,
			ObservableCounterAttribute,
			ObservableUpDownCounterAttribute,
			ObservableGaugeAttribute,
		];

		public static TemplateInfo[] GetTemplates() =>
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
				ObservableGaugeAttribute,
				ObservableUpDownCounterAttribute,
			];
	}

	public static TemplateInfo[] GetAllTemplates() =>
		[.. Activities.GetTemplates(), .. Logging.GetTemplates(), .. Metrics.GetTemplates(), .. Shared.GetTemplates()];
}
