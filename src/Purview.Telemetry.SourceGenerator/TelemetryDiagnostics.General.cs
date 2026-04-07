using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

partial class TelemetryDiagnostics
{
	public static class General
	{
		public static readonly TelemetryDiagnosticDescriptor FatalExecutionDuringExecution = new(
			Id: "TSG1000",
			Title: "Fatal execution error occurred",
			Description: "Failed to execute the generation stage: {0}",
			Severity: DiagnosticSeverity.Error,
			Category: Constants.Diagnostics.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor InferenceNotSupportedWithMultiTargeting =
			new(
				Id: "TSG1001",
				Title: "Inferring generation targets is not supported when using multi-target generation",
				Description: "When using multiple generation targets - Activities, Logs or Metrics, each method must be either excluded or have an explicit generation target: "
					+ $"{Constants.Activities.ActivityAttribute.Name}, {Constants.Activities.EventAttribute.Name}, {Constants.Activities.ContextAttribute.Name}, {Constants.Logging.LogAttribute.Name}, "
					+ $"{Constants.Logging.WarningAttribute.Name}, "
					+ $"{Constants.Metrics.CounterAttribute.Name}, {Constants.Metrics.HistogramAttribute.Name}, {Constants.Metrics.UpDownCounterAttribute.Name}, "
					+ $"{Constants.Metrics.ObservableCounterAttribute.Name}, {Constants.Metrics.ObservableGaugeAttribute.Name} or {Constants.Metrics.ObservableUpDownCounterAttribute.Name}.",
				Severity: DiagnosticSeverity.Error,
				Category: Constants.Diagnostics.Usage
			);

		public static readonly TelemetryDiagnosticDescriptor MultiGenerationTargetsNotSupported =
			new(
				Id: "TSG1002",
				Title: "Multiple attributes from the same target family are not supported",
				Description: "Only a single attribute per target family (Activities, Logs, or Metrics) is allowed on each method. "
					+ "For Activities: use one of ActivityAttribute, EventAttribute, or ContextAttribute. "
					+ "For Logging: use one of LogAttribute or a semantic level attribute (TraceAttribute, DebugAttribute, etc.). "
					+ "For Metrics: use one instrument attribute (CounterAttribute, HistogramAttribute, etc.).",
				Severity: DiagnosticSeverity.Error,
				Category: Constants.Diagnostics.Usage
			);

		public static readonly TelemetryDiagnosticDescriptor DuplicateMethodNamesAreNotSupported =
			new(
				Id: "TSG1003",
				Title: "Duplicate method names are not supported",
				Description: "Two or more methods named '{0}' are defined. Keep method names unique as they're used to generate other members on the implementation class.",
				Severity: DiagnosticSeverity.Error,
				Category: Constants.Diagnostics.Usage
			);

		public static readonly TelemetryDiagnosticDescriptor GenericInterfacesNotSupported = new(
			Id: "TSG1004",
			Title: "Generic interfaces are not supported",
			Description: "Remove the generic type(s) from the interface, this type of generation is not supported.",
			Severity: DiagnosticSeverity.Error,
			Category: Constants.Diagnostics.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor GenericMethodsNotSupported = new(
			Id: "TSG1005",
			Title: "Generic methods are not supported",
			Description: "Remove the generic type(s) from the method, this type of generation is not supported.",
			Severity: DiagnosticSeverity.Error,
			Category: Constants.Diagnostics.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor ExcludeTargetsTargetNotPresent = new(
			Id: "TSG1006",
			Title: "ExcludeTargets references a target not present on this method",
			Description: "The parameter has [ExcludeTargets] excluding '{0}', but this method does not have any attributes for that target family.",
			Severity: DiagnosticSeverity.Warning,
			Category: Constants.Diagnostics.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor ExcludeTargetsResultsInEmptyParameterSet =
			new(
				Id: "TSG1007",
				Title: "ExcludeTargets results in an empty or invalid parameter set for a target",
				Description: "Excluding parameters results in an invalid configuration for the '{0}' target: {1}",
				Severity: DiagnosticSeverity.Warning,
				Category: Constants.Diagnostics.Usage
			);

		public static readonly TelemetryDiagnosticDescriptor ActivityParameterWithoutActivityTarget =
			new(
				Id: "TSG1008",
				Title: "Activity parameter has no Activity target",
				Description: "Parameter '{0}' of type Activity is present, but this method has no Activity attribute ([Activity], [Event], or [Context]). The parameter will be ignored.",
				Severity: DiagnosticSeverity.Warning,
				Category: Constants.Diagnostics.Usage
			);

		public static readonly TelemetryDiagnosticDescriptor CompoundKeyWithoutSeparator = new(
			Id: "TSG1009",
			Title: "Tag/baggage key appears to be a compound word without separators",
			Description: "Tag/baggage key '{0}' appears to be a compound word without separators. Consider providing an explicit name using snake_case or dot.notation for better observability.",
			Severity: DiagnosticSeverity.Info,
			Category: Constants.Diagnostics.Usage
		);

		public static readonly TelemetryDiagnosticDescriptor MethodTargetNotRegisteredOnInterface =
			new(
				Id: "TSG1010",
				Title: "Method target not registered on interface",
				Description: "Method has attribute(s) for a generation target that is not registered on the interface. "
					+ $"Add the corresponding source attribute ([{Constants.Activities.ActivitySourceAttribute.Name}], [{Constants.Logging.LoggerAttribute.Name}], or [{Constants.Metrics.MeterAttribute.Name}]) "
					+ "to the interface to enable generation for the target(s) used by this method.",
				Severity: DiagnosticSeverity.Error,
				Category: Constants.Diagnostics.Usage
			);
		public static readonly TelemetryDiagnosticDescriptor UnsupportedTargetFramework = new(
			Id: "TSG1011",
			Title: "Unsupported target framework",
			Description: "Purview Telemetry Source Generator requires .NET 8.0 or higher, or .NET Framework 4.8 or higher. "
				+ "The current target framework is not supported. "
				+ "If this is incorrect, define PURVIEW_TELEMETRY_NON_NULLABLE to suppress this error and opt out of nullable reference type annotations.",
			Severity: DiagnosticSeverity.Error,
			Category: Constants.Diagnostics.Usage
		);
	}
}
