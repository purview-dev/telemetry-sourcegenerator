using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

partial class TelemetryDiagnostics
{
	// Start at 3000
	public static class Activities
	{
		public static readonly TelemetryDiagnosticDescriptor BaggageParameterShouldBeString = new(
			Id: "TSG3000",
			Title: "Baggage parameter types only accept strings",
			Description: "Baggage parameter types only accept strings, be aware this parameter will have ToString() called.",
			Severity: DiagnosticSeverity.Warning,
			Category: Constants.Diagnostics.Activity.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor NoActivitySourceSpecified = new(
			Id: "TSG3001",
			Title: "No activity source specified",
			Description: $"An activity source helps to identify your application and it's telemetry. Defaulting to '{Constants.Activities.DefaultActivitySourceName}'.",
			Severity: DiagnosticSeverity.Warning,
			Category: Constants.Diagnostics.Activity.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor InvalidReturnType = new(
			Id: "TSG3002",
			Title: "Invalid return type",
			Description: $"An activity or event must return either void or an {Constants.Activities.SystemDiagnostics.Activity.ToString(includeGlobal: false)}.",
			Severity: DiagnosticSeverity.Error,
			Category: Constants.Diagnostics.Activity.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor DuplicateParameterTypes = new(
			Id: "TSG3003",
			Title: "Duplicate reserved parameters defined",
			Description: "{0} are all the same type of parameter ({1}), a maximum or one is allowed. Explicitly define them as either a Tag or Baggage.",
			Severity: DiagnosticSeverity.Error,
			Category: Constants.Diagnostics.Activity.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor ActivityParameterNotAllowed = new(
			Id: "TSG3004",
			Title: "Activity parameter is not valid",
			Description: "The {0} parameter is not allowed when defining an activity, only an event.",
			Severity: DiagnosticSeverity.Error,
			Category: Constants.Diagnostics.Activity.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor TimestampParameterNotAllowed = new(
			Id: "TSG3005",
			Title: "Timestamp parameter is not valid",
			Description: "The {0} parameter is not allowed when defining an activity, only an event. You can specify this as a Tag or as Baggage to stop the inference.",
			Severity: DiagnosticSeverity.Error,
			Category: Constants.Diagnostics.Activity.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor StartTimeParameterNotAllowed = new(
			Id: "TSG3006",
			Title: "Start time parameter is not valid on Create activity or Event method",
			Description: "The {0} parameter is not allowed when defining an activity create or activity event method, only when starting an activity.",
			Severity: DiagnosticSeverity.Error,
			Category: Constants.Diagnostics.Activity.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor ParentContextOrIdParameterNotAllowed =
			new(
				Id: "TSG3007",
				Title: "Parent context or Parent Id parameter is not valid on event",
				Description: "The {0} parameter is not allowed when defining an activity event, only on the activity start/ create method.",
				Severity: DiagnosticSeverity.Error,
				Category: Constants.Diagnostics.Activity.Usage
			);

		public static readonly TelemetryDiagnosticDescriptor LinksParameterNotAllowed = new(
			Id: "TSG3008",
			Title: "Activity links parameters are not valid on events or context methods",
			Description: "The {0} parameter is not allowed when defining an activity event or context, only on the activity start/ create method.",
			Severity: DiagnosticSeverity.Error,
			Category: Constants.Diagnostics.Activity.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor TagsParameterNotAllowed = new(
			Id: "TSG3009",
			Title: "Activity tags parameter are not valid on context methods",
			Description: "The {0} parameter is not allowed when defining an activity context, only on the activity start/ create methods or events.",
			Severity: DiagnosticSeverity.Error,
			Category: Constants.Diagnostics.Activity.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor EscapedParameterInvalidType = new(
			Id: "TSG3010",
			Title: "Escaped parameters must be a boolean",
			Description: "Only boolean parameter types are valid for the escape parameter.",
			Severity: DiagnosticSeverity.Error,
			Category: Constants.Diagnostics.Activity.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor EscapedParameterIsOnlyValidOnEvent =
			new(
				Id: "TSG3011",
				Title: "Escaped parameters are only valid on Events, not Activity or Context methods",
				Description: "The parameters {0} is not valid on Activity or Context methods, only on Events.",
				Severity: DiagnosticSeverity.Error,
				Category: Constants.Diagnostics.Activity.Usage
			);

		public static readonly TelemetryDiagnosticDescriptor NoActivityMethodsDefined = new(
			Id: "TSG3012",
			Title: "There are no Activity methods defined, assumed use of Activity.Current",
			Description: "As Event and/ or Context methods are defined, it's best practice to create a specific Activity otherwise the Activity will belong to another operation.",
			Severity: DiagnosticSeverity.Info,
			Category: Constants.Diagnostics.Activity.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor DoesNotReturnActivity = new(
			Id: "TSG3013",
			Title: "Should return the created Activity",
			Description: "It's best practice to return the created Activity so you can dispose of it, and use it for Event or Context methods.",
			Severity: DiagnosticSeverity.Warning,
			Category: Constants.Diagnostics.Activity.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor DoesNotAcceptActivityParameter = new(
			Id: "TSG3014",
			Title: "Should accept an Activity to apply the Event/ Tags/ Baggage too",
			Description: "It's best practice to accept an Activity parameter to ensure the Event, Tags and/ or Baggage are applied to the Activity you intended.",
			Severity: DiagnosticSeverity.Warning,
			Category: Constants.Diagnostics.Activity.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor ActivityShouldBeTheFirstParameter =
			new(
				Id: "TSG3015",
				Title: "Activity should be the first parameter",
				Description: "For readability, ensure the Activity parameter is the first one defined.",
				Severity: DiagnosticSeverity.Info,
				Category: Constants.Diagnostics.Activity.Usage
			);

		public static readonly TelemetryDiagnosticDescriptor StatusDescriptionMustBeString = new(
			Id: "TSG3016",
			Title: "Status description parameter should be a string",
			Description: "Status descriptions can only be of type string.",
			Severity: DiagnosticSeverity.Error,
			Category: Constants.Diagnostics.Activity.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor StatusDescriptionParameterInvalidType =
			new(
				Id: "TSG3017",
				Title: "Status Description parameters are only valid on Events, not Activity or Context methods",
				Description: "The parameters {0} is not valid on Activity or Context methods, only on Events.",
				Severity: DiagnosticSeverity.Error,
				Category: Constants.Diagnostics.Activity.Usage
			);
	}
}
