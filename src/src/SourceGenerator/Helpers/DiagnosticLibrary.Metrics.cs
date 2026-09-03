using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;

namespace Purview.Telemetry.SourceGenerator;

partial class DiagnosticLibrary
{
	// Starts at 4000
	public static class Metrics
	{
		public static readonly DiagnosticInfo NoInstrumentDefined = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG4000",
				title: "No instrument defined",
				messageFormat: "Either exclude this method, or define an instrument.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Metrics.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo DoesNotReturnVoid = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG4001",
				title: "Must return void or bool",
				messageFormat: "Instrument methods can only return void or boolean.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Metrics.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo AutoIncrementCountAndMeasurementParam = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG4002",
				title: "Auto increment counter and measurement defined",
				messageFormat: "Auto increment counter and a measurement parameter are defined, either remove the parameter/ attribute or change to a normal counter.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Metrics.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo MoreThanOneMeasurementValueDefined = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG4003",
				title: "Multiple measurement values defined",
				messageFormat: "More than one measurement parameters are defined.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Metrics.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo NoMeasurementValueDefined = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG4004",
				title: "No measurement value defined",
				messageFormat: "Either define a measurement parameter, or provide a supported type parameter that is not a tag to enable inferring.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Metrics.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo ObservableRequiredFunc = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG4005",
				title: "Observable instrument requires Func<T>",
				messageFormat: "Observable instruments require a Func<T> where T is a supported instrument result type.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Metrics.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo InvalidMeasurementType = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG4006",
				title: "Invalid measurement type",
				messageFormat: $"Invalid measurement type used, valid types are {string.Join(", ", PropertyLibrary.Metrics.ValidMeasurementKeywordTypes)}, Measurement<T> or IEnumerable<MeasurementT>>.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Metrics.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo ObservableCannotReturnBool = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG4007",
				title: "Observable metrics cannot return bool",
				messageFormat: "Observable metrics can only return void or Activity? (when combined with Activity attribute). Boolean returns are not supported for observables.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Metrics.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo AutoCounterMustReturnVoid = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG4008",
				title: "AutoCounter must return void",
				messageFormat: "AutoCounter methods must return void. Boolean or other return types are not supported.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Metrics.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo InstrumentNameMatchesType = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG4009",
				title: "Instrument name matches the instrument type name",
				messageFormat: "Instrument name '{0}' matches the instrument type name. Use a name that describes what is being measured, not the instrument type.",
				defaultSeverity: DiagnosticSeverity.Warning,
				category: Categories.Metrics.Usage,
				isEnabledByDefault: true
			)
		);
	}
}
