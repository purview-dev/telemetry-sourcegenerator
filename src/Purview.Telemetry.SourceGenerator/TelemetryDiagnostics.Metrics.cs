using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

partial class TelemetryDiagnostics
{
	// Starts at 4000
	public static class Metrics
	{
		public static readonly TelemetryDiagnosticDescriptor NoInstrumentDefined = new(
			Id: "TSG4000",
			Title: "No instrument defined",
			Description: "Either exclude this method, or define an instrument.",
			Severity: DiagnosticSeverity.Error,
			Category: Constants.Diagnostics.Metrics.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor DoesNotReturnVoid = new(
			Id: "TSG4001",
			Title: "Must return void or bool",
			Description: "Instrument methods can only return void or boolean.",
			Severity: DiagnosticSeverity.Error,
			Category: Constants.Diagnostics.Metrics.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor AutoIncrementCountAndMeasurementParam =
			new(
				Id: "TSG4002",
				Title: "Auto increment counter and measurement defined",
				Description: "Auto increment counter and a measurement parameter are defined, either remove the parameter/ attribute or change to a normal counter.",
				Severity: DiagnosticSeverity.Error,
				Category: Constants.Diagnostics.Metrics.Usage
			);

		public static readonly TelemetryDiagnosticDescriptor MoreThanOneMeasurementValueDefined =
			new(
				Id: "TSG4003",
				Title: "Multiple measurement values defined",
				Description: "More than one measurement parameters are defined.",
				Severity: DiagnosticSeverity.Error,
				Category: Constants.Diagnostics.Metrics.Usage
			);

		public static readonly TelemetryDiagnosticDescriptor NoMeasurementValueDefined = new(
			Id: "TSG4004",
			Title: "No measurement value defined",
			Description: "Either define a measurement parameter, or provide a supported type parameter that is not a tag to enable inferring.",
			Severity: DiagnosticSeverity.Error,
			Category: Constants.Diagnostics.Metrics.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor ObservableRequiredFunc = new(
			Id: "TSG4005",
			Title: "Observable instrument requires Func<T>",
			Description: "Observable instruments require a Func<T> where T is a supported instrument result type.",
			Severity: DiagnosticSeverity.Error,
			Category: Constants.Diagnostics.Metrics.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor InvalidMeasurementType = new(
			Id: "TSG4006",
			Title: "Invalid measurement type",
			Description: $"Invalid measurement type used, valid types are {string.Join(", ", Constants.Metrics.ValidMeasurementKeywordTypes)}, Measurement<T> or IEnumerable<MeasurementT>>.",
			Severity: DiagnosticSeverity.Error,
			Category: Constants.Diagnostics.Metrics.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor ObservableCannotReturnBool = new(
			Id: "TSG4007",
			Title: "Observable metrics cannot return bool",
			Description: "Observable metrics can only return void or Activity? (when combined with Activity attribute). Boolean returns are not supported for observables.",
			Severity: DiagnosticSeverity.Error,
			Category: Constants.Diagnostics.Metrics.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor AutoCounterMustReturnVoid = new(
			Id: "TSG4008",
			Title: "AutoCounter must return void",
			Description: "AutoCounter methods must return void. Boolean or other return types are not supported.",
			Severity: DiagnosticSeverity.Error,
			Category: Constants.Diagnostics.Metrics.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor InstrumentNameMatchesType = new(
			Id: "TSG4009",
			Title: "Instrument name matches the instrument type name",
			Description: "Instrument name '{0}' matches the instrument type name. Use a name that describes what is being measured, not the instrument type.",
			Severity: DiagnosticSeverity.Warning,
			Category: Constants.Diagnostics.Metrics.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor InstrumentNameLikelyCompound = new(
			Id: "TSG4010",
			Title: "Instrument name appears to contain compound words without separators",
			Description: "Instrument name '{0}' appears to contain compound words without separators. Consider using dot.notation or explicit naming for better observability.",
			Severity: DiagnosticSeverity.Warning,
			Category: Constants.Diagnostics.Metrics.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor MeterNameDefaultsToInterface = new(
			Id: "TSG4011",
			Title: "Meter name is defaulting to interface name",
			Description: "Meter name is defaulting to interface name '{0}'. Consider using assembly name or explicit naming with [Meter(Name = \"...\")] for stable meter identity.",
			Severity: DiagnosticSeverity.Info,
			Category: Constants.Diagnostics.Metrics.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor InstrumentNameStartsWithType = new(
			Id: "TSG4012",
			Title: "Instrument name starts with instrument type verb",
			Description: "Instrument name '{0}' starts with the instrument type verb. Consider naming after the measured concept instead (e.g., 'request.duration' instead of 'RecordRequestDuration').",
			Severity: DiagnosticSeverity.Info,
			Category: Constants.Diagnostics.Metrics.Usage
		);
	}
}
