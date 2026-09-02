using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;

namespace Purview.Telemetry.SourceGenerator;

partial class DiagnosticLibrary
{
	// Start at 3000
	public static class Activities
	{
		public static readonly DiagnosticInfo BaggageParameterShouldBeString = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG3000",
				title: "Baggage parameter types only accept strings",
				messageFormat: "Baggage parameter types only accept strings, be aware this parameter will have ToString() called.",
				defaultSeverity: DiagnosticSeverity.Warning,
				category: Categories.Activity.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo NoActivitySourceSpecified = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG3001",
				title: "No activity source specified",
				messageFormat: $"An activity source helps to identify your application and it's telemetry. Defaulting to '{PropertyLibrary.Activities.DefaultActivitySourceName}'.",
				defaultSeverity: DiagnosticSeverity.Warning,
				category: Categories.Activity.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo InvalidReturnType = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG3002",
				title: "Invalid return type",
				messageFormat: $"An activity or event must return either void or an {TypeLibrary.Activities.SystemDiagnostics.Activity.Name}.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Activity.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo DuplicateParameterTypes = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG3003",
				title: "Duplicate reserved parameters defined",
				messageFormat: "{0} are all the same type of parameter ({1}), a maximum or one is allowed. Explicitly define them as either a Tag or Baggage.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Activity.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo ActivityParameterNotAllowed = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG3004",
				title: "Activity parameter is not valid",
				messageFormat: "The {0} parameter is not allowed when defining an activity, only an event.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Activity.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo TimestampParameterNotAllowed = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG3005",
				title: "Timestamp parameter is not valid",
				messageFormat: "The {0} parameter is not allowed when defining an activity, only an event. You can specify this as a Tag or as Baggage to stop the inference.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Activity.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo StartTimeParameterNotAllowed = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG3006",
				title: "Start time parameter is not valid on Create activity or Event method",
				messageFormat: "The {0} parameter is not allowed when defining an activity create or activity event method, only when starting an activity.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Activity.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo ParentContextOrIdParameterNotAllowed = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG3007",
				title: "Parent context or Parent Id parameter is not valid on event",
				messageFormat: "The {0} parameter is not allowed when defining an activity event, only on the activity start/ create method.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Activity.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo LinksParameterNotAllowed = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG3008",
				title: "Activity links parameters are not valid on events or context methods",
				messageFormat: "The {0} parameter is not allowed when defining an activity event or context, only on the activity start/ create method.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Activity.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo TagsParameterNotAllowed = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG3009",
				title: "Activity tags parameter are not valid on context methods",
				messageFormat: "The {0} parameter is not allowed when defining an activity context, only on the activity start/ create methods or events.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Activity.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo EscapedParameterInvalidType = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG3010",
				title: "Escaped parameters must be a boolean",
				messageFormat: "Only boolean parameter types are valid for the escape parameter.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Activity.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo EscapedParameterIsOnlyValidOnEvent = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG3011",
				title: "Escaped parameters are only valid on Events, not Activity or Context methods",
				messageFormat: "The parameters {0} is not valid on Activity or Context methods, only on Events.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Activity.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo NoActivityMethodsDefined = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG3012",
				title: "There are no Activity methods defined, assumed use of Activity.Current",
				messageFormat: "As Event and/ or Context methods are defined, it's best practice to create a specific Activity otherwise the Activity will belong to another operation.",
				defaultSeverity: DiagnosticSeverity.Info,
				category: Categories.Activity.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo DoesNotReturnActivity = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG3013",
				title: "Should return the created Activity",
				messageFormat: "It's best practice to return the created Activity so you can dispose of it, and use it for Event or Context methods.",
				defaultSeverity: DiagnosticSeverity.Warning,
				category: Categories.Activity.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo DoesNotAcceptActivityParameter = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG3014",
				title: "Should accept an Activity to apply the Event/ Tags/ Baggage too",
				messageFormat: "It's best practice to accept an Activity parameter to ensure the Event, Tags and/ or Baggage are applied to the Activity you intended.",
				defaultSeverity: DiagnosticSeverity.Warning,
				category: Categories.Activity.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo ActivityShouldBeTheFirstParameter = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG3015",
				title: "Activity should be the first parameter",
				messageFormat: "For readability, ensure the Activity parameter is the first one defined.",
				defaultSeverity: DiagnosticSeverity.Info,
				category: Categories.Activity.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo StatusDescriptionMustBeString = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG3016",
				title: "Status description parameter should be a string",
				messageFormat: "Status descriptions can only be of type string.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Activity.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo StatusDescriptionParameterInvalidType = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG3017",
				title: "Status Description parameters are only valid on Events, not Activity or Context methods",
				messageFormat: "The parameters {0} is not valid on Activity or Context methods, only on Events.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Activity.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo ActivityReturnTypeShouldBeNullable = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG3022",
				title: "Activity return type should be nullable",
				messageFormat: "Activity can be null when no listeners are active. Consider using 'Activity?' as the return type to make this explicit.",
				defaultSeverity: DiagnosticSeverity.Warning,
				category: Categories.Activity.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo ExceptionEventNotStandardName = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG3021",
				title: "Exception event does not use OpenTelemetry standard name",
				messageFormat: "Event '{0}' records an exception but does not use the OpenTelemetry standard name 'exception'. Consider using [Event(Name = \"exception\")] for better observability.",
				defaultSeverity: DiagnosticSeverity.Info,
				category: Categories.Activity.Usage,
				isEnabledByDefault: true
			)
		);
	}
}
