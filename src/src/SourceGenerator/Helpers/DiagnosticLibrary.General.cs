using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;

namespace Purview.Telemetry.SourceGenerator;

partial class DiagnosticLibrary
{
	public static class General
	{
		public static readonly DiagnosticInfo FatalExecutionDuringExecution = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG1000",
				title: "Fatal execution error occurred",
				messageFormat: "Failed to execute the generation stage: {0}",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo InferenceNotSupportedWithMultiTargeting = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG1001",
				title: "Inferring generation targets is not supported when using multi-target generation",
				messageFormat: "When using multiple generation targets - Activities, Logs or Metrics, each method must be either excluded or have an explicit generation target: "
					+ $"{TypeLibrary.Activities.ActivityAttribute.Name}, {TypeLibrary.Activities.EventAttribute.Name}, {TypeLibrary.Activities.ContextAttribute.Name}, {TypeLibrary.Logging.LogAttribute.Name}, "
					+ $"{TypeLibrary.Logging.WarningAttribute.Name}, "
					+ $"{TypeLibrary.Metrics.CounterAttribute.Name}, {TypeLibrary.Metrics.HistogramAttribute.Name}, {TypeLibrary.Metrics.UpDownCounterAttribute.Name}, "
					+ $"{TypeLibrary.Metrics.ObservableCounterAttribute.Name}, {TypeLibrary.Metrics.ObservableGaugeAttribute.Name} or {TypeLibrary.Metrics.ObservableUpDownCounterAttribute.Name}.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo MultiGenerationTargetsNotSupported = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG1002",
				title: "Multiple attributes from the same target family are not supported",
				messageFormat: "Only a single attribute per target family (Activities, Logs, or Metrics) is allowed on each method. "
					+ "For Activities: use one of ActivityAttribute, EventAttribute, or ContextAttribute. "
					+ "For Logging: use one of LogAttribute or a semantic level attribute (TraceAttribute, DebugAttribute, etc.). "
					+ "For Metrics: use one instrument attribute (CounterAttribute, HistogramAttribute, etc.).",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo DuplicateMethodNamesAreNotSupported = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG1003",
				title: "Duplicate method names are not supported",
				messageFormat: "Two or more methods named '{0}' are defined. Keep method names unique as they're used to generate other members on the implementation class.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo GenericInterfacesNotSupported = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG1004",
				title: "Generic interfaces are not supported",
				messageFormat: "Remove the generic type(s) from the interface, this type of generation is not supported.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo GenericMethodsNotSupported = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG1005",
				title: "Generic methods are not supported",
				messageFormat: "Remove the generic type(s) from the method, this type of generation is not supported.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo ExcludeTargetsTargetNotPresent = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG1006",
				title: "ExcludeTargets references a target not present on this method",
				messageFormat: "The parameter has [ExcludeTargets] excluding '{0}', but this method does not have any attributes for that target family.",
				defaultSeverity: DiagnosticSeverity.Warning,
				category: Categories.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo ExcludeTargetsResultsInEmptyParameterSet = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG1007",
				title: "ExcludeTargets results in an empty or invalid parameter set for a target",
				messageFormat: "Excluding parameters results in an invalid configuration for the '{0}' target: {1}",
				defaultSeverity: DiagnosticSeverity.Warning,
				category: Categories.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo ActivityParameterWithoutActivityTarget = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG1008",
				title: "Activity parameter has no Activity target",
				messageFormat: "Parameter '{0}' of type Activity is present, but this method has no Activity attribute ([Activity], [Event], or [Context]). The parameter will be ignored.",
				defaultSeverity: DiagnosticSeverity.Warning,
				category: Categories.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo MethodTargetNotRegisteredOnInterface = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG1010",
				title: "Method target not registered on interface",
				messageFormat: "Method has attribute(s) for a generation target that is not registered on the interface. "
					+ $"Add the corresponding source attribute ([{TypeLibrary.Activities.ActivitySourceAttribute.Name}], [{TypeLibrary.Logging.LoggerAttribute.Name}], or [{TypeLibrary.Metrics.MeterAttribute.Name}]) "
					+ "to the interface to enable generation for the target(s) used by this method.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Usage,
				isEnabledByDefault: true
			)
		);
		public static readonly DiagnosticInfo UnsupportedTargetFramework = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG1011",
				title: "Unsupported target framework",
				messageFormat: "Purview Telemetry Source Generator requires .NET 8.0 or higher, or .NET Framework 4.8 or higher. "
					+ "The current target framework is not supported. "
					+ "If this is incorrect, define PURVIEW_TELEMETRY_NON_NULLABLE to suppress this error and opt out of nullable reference type annotations.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Usage,
				isEnabledByDefault: true
			)
		);
	}
}
