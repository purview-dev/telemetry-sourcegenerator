namespace Purview.Telemetry.SourceGenerator.Records;

sealed record MeterTarget(
	TelemetryGenerationAttributeData TelemetryGeneration,
	GenerationType GenerationType,
	string ClassNameToGenerate,
	string? ClassNamespace,
	EquatableArray<string> ParentClasses,
	string? FullNamespace,
	string? FullyQualifiedName,
	TypeReference InterfaceType,
	string? MeterName,
	MeterGenerationAttributeData? MeterGeneration,
	EquatableArray<InstrumentTarget> InstrumentationMethods
);

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
