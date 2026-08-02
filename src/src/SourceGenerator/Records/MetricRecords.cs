using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Records;

record MeterTarget(
	TelemetryGenerationAttributeRecord TelemetryGeneration,
	GenerationType GenerationType,
	string ClassNameToGenerate,
	string? ClassNamespace,
	EquatableArray<string> ParentClasses,
	string? FullNamespace,
	string? FullyQualifiedName,
	PurviewTypeInfo InterfaceType,
	string? MeterName,
	MeterGenerationAttributeRecord? MeterGeneration,
	EquatableArray<InstrumentTarget> InstrumentationMethods
);

record InstrumentTarget(
	string MethodName,
	PurviewTypeInfo ReturnType,
	bool ReturnsBool,
	bool IsNullableReturn,
	string FieldName,
	PurviewTypeInfo InstrumentMeasurementType,
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

record InstrumentParameterTarget(
	string ParameterName,
	PurviewTypeInfo ParameterType,
	bool IsFunc,
	bool IsIEnumerable,
	bool IsMeasurement,
	bool IsValidInstrumentType,
	PurviewTypeInfo? InstrumentType,
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
