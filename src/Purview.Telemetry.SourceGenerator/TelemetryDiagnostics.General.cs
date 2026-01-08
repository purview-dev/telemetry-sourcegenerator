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
			Severity: DiagnosticSeverity.Error
,
			Category: Constants.Diagnostics.Usage);

		public static readonly TelemetryDiagnosticDescriptor InferenceNotSupportedWithMultiTargeting =
			new(
				Id: "TSG1001",
				Title: "Inferring generation targets is not supported when using multi-target generation",
				Description: "When using multiple generation targets - Activities, Logs or Metrics, each method must be either excluded or have an explicit generation target: "
					+ $"{Constants.Activities.ActivityAttribute.Name}, {Constants.Activities.EventAttribute.Name}, {Constants.Activities.ContextAttribute.Name}, {Constants.Logging.LogAttribute.Name}, "
					+ $"{Constants.Logging.WarningAttribute.Name}, "
					+ $"{Constants.Metrics.CounterAttribute.Name}, {Constants.Metrics.HistogramAttribute.Name}, {Constants.Metrics.UpDownCounterAttribute.Name}, "
					+ $"{Constants.Metrics.ObservableCounterAttribute.Name}, {Constants.Metrics.ObservableGaugeAttribute.Name} or {Constants.Metrics.ObservableUpDownCounterAttribute.Name}.",
				Severity: DiagnosticSeverity.Error
,
				Category: Constants.Diagnostics.Usage);

		public static readonly TelemetryDiagnosticDescriptor MultiGenerationTargetsNotSupported =
			new(
				Id: "TSG1002",
				Title: "Multiple generation types are not supported",
				Description: "Only a single generation target types (Activities, Logs or Metrics) are supported. Use one of the following: "
					+ $"{Constants.Activities.ActivityAttribute.Name}, {Constants.Activities.EventAttribute.Name}, {Constants.Activities.ContextAttribute.Name}, {Constants.Logging.LogAttribute.Name}, "
					+ $"{Constants.Logging.WarningAttribute.Name}, "
					+ $"{Constants.Metrics.CounterAttribute.Name}, {Constants.Metrics.HistogramAttribute.Name}, {Constants.Metrics.UpDownCounterAttribute.Name}, "
					+ $"{Constants.Metrics.ObservableCounterAttribute.Name}, {Constants.Metrics.ObservableGaugeAttribute.Name} or {Constants.Metrics.ObservableUpDownCounterAttribute.Name}.",
				Severity: DiagnosticSeverity.Error
,
				Category: Constants.Diagnostics.Usage);

		public static readonly TelemetryDiagnosticDescriptor DuplicateMethodNamesAreNotSupported =
			new(
				Id: "TSG1003",
				Title: "Duplicate method names are not supported",
				Description: "Two or more methods named '{0}' are defined. Keep method names unique as they're used to generate other members on the implementation class.",
				Severity: DiagnosticSeverity.Error
,
				Category: Constants.Diagnostics.Usage);

		public static readonly TelemetryDiagnosticDescriptor GenericInterfacesNotSupported = new(
			Id: "TSG1004",
			Title: "Generic interfaces are not supported",
			Description: "Remove the generic type(s) from the interface, this type of generation is not supported.",
			Severity: DiagnosticSeverity.Error
,
			Category: Constants.Diagnostics.Usage);

		public static readonly TelemetryDiagnosticDescriptor GenericMethodsNotSupported = new(
			Id: "TSG1005",
			Title: "Generic methods are not supported",
			Description: "Remove the generic type(s) from the method, this type of generation is not supported.",
			Severity: DiagnosticSeverity.Error
,
			Category: Constants.Diagnostics.Usage);
	}
}
