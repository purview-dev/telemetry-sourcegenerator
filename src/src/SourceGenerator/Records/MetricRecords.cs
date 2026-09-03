namespace Purview.Telemetry.SourceGenerator.Records;

sealed record MeterTarget(
	TelemetryGenerationAttributeData TelemetryGeneration,
	GenerationType GenerationType,
	string ClassNameToGenerate,
	EquatableArray<string> ParentClasses,
	TypeReference InterfaceType,
	string? MeterName,
	MeterGenerationAttributeData? MeterGeneration,
	EquatableArray<InstrumentTarget> InstrumentationMethods
)
{
	public string? ClassNamespace => TelemetryGeneration.TelemetryNamesNamespace ?? InterfaceType.Identity.Namespace;

	public string? FullNamespace
	{
		get
		{
			var telemetryNamesNamespace = TelemetryGeneration.TelemetryNamesNamespace;
			if (telemetryNamesNamespace != null)
				return telemetryNamesNamespace + ".";

			var ns = InterfaceType.Identity.Namespace;
			if (ns == null && ParentClasses.IsEmpty)
				return null;

			var builder = new System.Text.StringBuilder();
			if (ns != null)
				builder.Append(ns).Append('.');

			for (var i = ParentClasses.Count - 1; i >= 0; i--)
				builder.Append(ParentClasses[i]).Append('.');

			return builder.ToString();
		}
	}

	public string? FullyQualifiedName =>
		FullNamespace is null ? ClassNameToGenerate : FullNamespace + ClassNameToGenerate;
}

sealed record InstrumentTarget(
	string MethodName,
	TypeReference ReturnType,
	bool ReturnsBool,
	bool IsNullableReturn,
	string FieldName,
	TypeReference InstrumentMeasurementType,
	bool IsObservable,
	string MetricName,
	InstrumentAttributeRecord? InstrumentAttribute,
	EquatableArray<InstrumentParameterTarget> Parameters,
	EquatableArray<InstrumentParameterTarget> Tags,
	InstrumentParameterTarget? MeasurementParameter,
	TargetGeneration TargetGenerationState
)
{
	public string TagPopulateMethodName { get; } = $"Populate{MethodName}Tags";
}

sealed record InstrumentParameterTarget(
	string ParameterName,
	TypeReference ParameterType,
	bool IsFunc,
	bool IsIEnumerable,
	bool IsMeasurement,
	bool IsValidInstrumentType,
	TypeReference? InstrumentType,
	string GeneratedName,
	InstrumentParameterDestination ParamDestination,
	bool SkipOnNullOrEmpty,
	GenerationType ExcludedTargets
);

enum InstrumentTypes
{
	Counter,
	UpDownCounter,
	Histogram,

	ObservableCounter,
	ObservableGauge,
	ObservableUpDownCounter,
}

enum InstrumentParameterDestination
{
	Tag,
	Measurement,
	Unknown,
}
