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
			Category: Constants.Diagnostics.Usage,
			Severity: DiagnosticSeverity.Error
		);

		public static readonly TelemetryDiagnosticDescriptor InferenceNotSupportedWithMultiTargeting =
			new(
				Id: "TSG1001",
				Title: "Inferring generation targets is not supported when using multi-target generation",
				Description: $"When using multiple generation targets - Activities, Logs or Metrics, each method must be either excluded or have an explicit generation target: "
					+ $"{Constants.Activities.ActivityAttribute.TypeName}, {Constants.Activities.EventAttribute.TypeName}, {Constants.Activities.ContextAttribute.TypeName}, {Constants.Logging.LogAttribute.TypeName}, "
					+ $"{Constants.Logging.WarningAttribute.TypeName}, "
					+ $"{Constants.Metrics.CounterAttribute.TypeName}, {Constants.Metrics.HistogramAttribute.TypeName}, {Constants.Metrics.UpDownCounterAttribute.TypeName}, "
					+ $"{Constants.Metrics.ObservableCounterAttribute.TypeName}, {Constants.Metrics.ObservableGaugeAttribute.TypeName} or {Constants.Metrics.ObservableUpDownCounterAttribute.TypeName}.",
				Category: Constants.Diagnostics.Usage,
				Severity: DiagnosticSeverity.Error
			);

		public static readonly TelemetryDiagnosticDescriptor MultiGenerationTargetsNotSupported =
			new(
				Id: "TSG1002",
				Title: "Multiple generation types are not supported",
				Description: $"Only a single generation target types (Activities, Logs or Metrics) are supported. Use one of the following: "
					+ $"{Constants.Activities.ActivityAttribute.TypeName}, {Constants.Activities.EventAttribute.TypeName}, {Constants.Activities.ContextAttribute.TypeName}, {Constants.Logging.LogAttribute.TypeName}, "
					+ $"{Constants.Logging.WarningAttribute.TypeName}, "
					+ $"{Constants.Metrics.CounterAttribute.TypeName}, {Constants.Metrics.HistogramAttribute.TypeName}, {Constants.Metrics.UpDownCounterAttribute.TypeName}, "
					+ $"{Constants.Metrics.ObservableCounterAttribute.TypeName}, {Constants.Metrics.ObservableGaugeAttribute.TypeName} or {Constants.Metrics.ObservableUpDownCounterAttribute.TypeName}.",
				Category: Constants.Diagnostics.Usage,
				Severity: DiagnosticSeverity.Error
			);

		public static readonly TelemetryDiagnosticDescriptor DuplicateMethodNamesAreNotSupported =
			new(
				Id: "TSG1003",
				Title: "Duplicate method names are not supported",
				Description: "Two or more methods named '{0}' are defined. Keep method names unique as they're used to generate other members on the implementation class.",
				Category: Constants.Diagnostics.Usage,
				Severity: DiagnosticSeverity.Error
			);

		public static readonly TelemetryDiagnosticDescriptor GenericInterfacesNotSupported = new(
			Id: "TSG1004",
			Title: "Generic interfaces are not supported",
			Description: "Remove the generic type(s) from the interface, this type of generation is not supported.",
			Category: Constants.Diagnostics.Usage,
			Severity: DiagnosticSeverity.Error
		);

		public static readonly TelemetryDiagnosticDescriptor GenericMethodsNotSupported = new(
			Id: "TSG1005",
			Title: "Generic methods are not supported",
			Description: "Remove the generic type(s) from the method, this type of generation is not supported.",
			Category: Constants.Diagnostics.Usage,
			Severity: DiagnosticSeverity.Error
		);
	}
}
