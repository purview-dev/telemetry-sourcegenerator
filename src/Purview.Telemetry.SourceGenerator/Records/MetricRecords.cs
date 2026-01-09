using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Records;

record MeterTarget(
	TelemetryGenerationAttributeRecord TelemetryGeneration,
	GenerationType GenerationType,
	string ClassNameToGenerate,
	string? ClassNamespace,
	string[] ParentClasses,
	string? FullNamespace,
	string? FullyQualifiedName,
	PurviewTypeInfo InterfaceType,
	string? MeterName,
	MeterGenerationAttributeRecord? MeterGeneration,
	ImmutableArray<InstrumentTarget> InstrumentationMethods,
	ImmutableDictionary<string, Location[]> DuplicateMethods,
	ImmutableArray<(TelemetryDiagnosticDescriptor, ImmutableArray<Location>)>? Failures
)
{
	public static MeterTarget Failed(
		TelemetryDiagnosticDescriptor diagnostic,
		ImmutableArray<Location> locations
	) =>
		new(
			null!,
			GenerationType.None,
			null!,
			null,
			null!,
			null,
			null,
			Constants.Empty,
			null,
			null,
			[],
			null!,
			[(diagnostic, locations)]
		);
}

record InstrumentTarget(
	string MethodName,
	PurviewTypeInfo ReturnType,
	bool ReturnsBool,
	bool IsNullableReturn,
	string FieldName,
	PurviewTypeInfo InstrumentMeasurementType,
	bool IsObservable,
	string MetricName,
	ImmutableArray<Location> Locations,
	InstrumentAttributeRecord? InstrumentAttribute,
	ImmutableArray<InstrumentParameterTarget> Parameters,
	ImmutableArray<InstrumentParameterTarget> Tags,
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
	ImmutableArray<Location> Locations,
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
