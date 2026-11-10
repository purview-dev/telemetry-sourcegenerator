namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class TypeLibraryGenerator
{
	[TypeRef(SystemDiagnosticsMetricsNamespace)]
	static readonly TypeIdentity Meter = default;

	[TypeRef(SystemDiagnosticsMetricsNamespace)]
	static readonly TypeIdentity IMeterFactory = default;

	[TypeRef(SystemDiagnosticsMetricsNamespace)]
	static readonly TypeIdentity MeterOptions = default;

	[TypeRef(SystemDiagnosticsMetricsNamespace, 1)]
	static readonly TypeIdentity Measurement = default;

	[TypeRef(SystemDiagnosticsMetricsNamespace)]
	static readonly TypeIdentity Counter = default;

	[TypeRef(SystemDiagnosticsMetricsNamespace)]
	static readonly TypeIdentity UpDownCounter = default;

	[TypeRef(SystemDiagnosticsMetricsNamespace)]
	static readonly TypeIdentity Histogram = default;

	[TypeRef(SystemDiagnosticsMetricsNamespace, 1)]
	static readonly TypeIdentity Gauge = default;

	[TypeRef(SystemDiagnosticsMetricsNamespace, 1)]
	static readonly TypeIdentity InstrumentAdvice = default;

	[TypeRef(SystemDiagnosticsMetricsNamespace)]
	static readonly TypeIdentity ObservableCounter = default;

	[TypeRef(SystemDiagnosticsMetricsNamespace)]
	static readonly TypeIdentity ObservableGauge = default;

	[TypeRef(SystemDiagnosticsMetricsNamespace)]
	static readonly TypeIdentity ObservableUpDownCounter = default;
}
