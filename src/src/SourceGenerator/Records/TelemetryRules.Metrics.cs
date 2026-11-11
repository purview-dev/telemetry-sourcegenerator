using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

static partial class TelemetryRules
{
	/// <summary>
	/// Metrics-specific diagnostics derived from the pipeline's <see cref="MeterTarget"/> so the instrument
	/// inference matches generation exactly.
	/// </summary>
	public static ImmutableArray<ReportableDiagnostic> GetMeterDiagnostics(
		MeterTarget target,
		INamedTypeSymbol interfaceSymbol,
		Compilation compilation,
		CancellationToken token
	)
	{
		var diagnostics = ImmutableArray.CreateBuilder<ReportableDiagnostic>();

		var supportsGauge = SharedHelpers.IsTypeAvailable(compilation, TypeLibrary.System.Diagnostics.Metrics.Gauge);
		var supportsInstrumentAdvice = SharedHelpers.IsTypeAvailable(
			compilation,
			TypeLibrary.System.Diagnostics.Metrics.InstrumentAdvice
		);

		foreach (var instrument in target.InstrumentationMethods)
		{
			token.ThrowIfCancellationRequested();

			if (!instrument.TargetGenerationState.IsValid)
				continue;

			var methodSymbol = FindMethod(interfaceSymbol, instrument.MethodName);
			if (methodSymbol is null)
				continue;

			// TSG4010: a gauge needs System.Diagnostics.Metrics.Gauge<T>, which an older analyzer-only
			// System.Diagnostics.DiagnosticSource does not carry.
			if (instrument.InstrumentAttribute is GaugeInstrumentAttributeRecord && !supportsGauge)
				diagnostics.Add(
					ReportableDiagnostic.Create(
						DiagnosticLibrary.Metrics.GaugeNotAvailable.Descriptor,
						isBlocking: false,
						methodSymbol
					)
				);

			// TSG4011: histogram bucket boundaries need InstrumentAdvice<T>.
			if (
				instrument.InstrumentAttribute
					is HistogramInstrumentAttributeRecord { HistogramBucketBoundaries.Count: > 0 }
				&& !supportsInstrumentAdvice
			)
				diagnostics.Add(
					ReportableDiagnostic.Create(
						DiagnosticLibrary.Metrics.InstrumentAdviceNotAvailable.Descriptor,
						isBlocking: false,
						methodSymbol
					)
				);

			// TSG4012: the boundaries were supplied but could not be parsed.
			if (
				instrument.InstrumentAttribute is HistogramInstrumentAttributeRecord
				{
					InvalidHistogramBucketBoundaries: true,
				}
			)
				diagnostics.Add(
					ReportableDiagnostic.Create(
						DiagnosticLibrary.Metrics.InvalidHistogramBucketBoundaries.Descriptor,
						isBlocking: false,
						methodSymbol
					)
				);

			ApplyInstrumentRules(instrument, methodSymbol, diagnostics);
		}

		return diagnostics.ToImmutable();
	}

	static void ApplyInstrumentRules(
		InstrumentTarget instrument,
		IMethodSymbol methodSymbol,
		ImmutableArray<ReportableDiagnostic>.Builder diagnostics
	)
	{
		// TSG4000: no instrument attribute defined.
		if (instrument.InstrumentAttribute is null)
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					DiagnosticLibrary.Metrics.NoInstrumentDefined.Descriptor,
					isBlocking: false,
					methodSymbol
				)
			);
			return;
		}

		var methodTargets = instrument.TargetGenerationState.MethodTargets;
		var activityOwnsPublicMethod = methodTargets.HasFlag(GenerationType.Activities);
		var loggingOwnsPublicMethod = !activityOwnsPublicMethod && methodTargets.HasFlag(GenerationType.Logging);
		var metricsOwnsPublicMethod = !activityOwnsPublicMethod && !loggingOwnsPublicMethod;

		var isVoid = instrument.ReturnType.Identity.SpecialType == SpecialType.System_Void;

		// TSG4001: metrics-owned public method must return void or bool.
		if (metricsOwnsPublicMethod && !isVoid && !instrument.ReturnsBool)
			diagnostics.Add(
				ReportableDiagnostic.Create(
					DiagnosticLibrary.Metrics.DoesNotReturnVoid.Descriptor,
					isBlocking: false,
					methodSymbol
				)
			);

		// TSG4007: observable instruments cannot return bool.
		if (instrument.IsObservable && instrument.ReturnsBool)
			diagnostics.Add(
				ReportableDiagnostic.Create(
					DiagnosticLibrary.Metrics.ObservableCannotReturnBool.Descriptor,
					isBlocking: false,
					methodSymbol
				)
			);

		// TSG4008: auto-counter instruments must return void.
		if (instrument.InstrumentAttribute.IsAutoIncrement && instrument.ReturnsBool)
			diagnostics.Add(
				ReportableDiagnostic.Create(
					DiagnosticLibrary.Metrics.AutoCounterMustReturnVoid.Descriptor,
					isBlocking: false,
					methodSymbol
				)
			);

		// TSG4002: auto-increment counter cannot also have a measurement parameter.
		if (instrument.InstrumentAttribute.IsAutoIncrement && instrument.MeasurementParameter != null)
			diagnostics.Add(
				ReportableDiagnostic.Create(
					DiagnosticLibrary.Metrics.AutoIncrementCountAndMeasurementParam.Descriptor,
					isBlocking: false,
					methodSymbol
				)
			);

		// TSG4003: more than one measurement value defined.
		var measurementParameters = instrument.Parameters.Where(static p => p.IsMeasurement).ToImmutableArray();
		if (measurementParameters.Length > 1)
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					DiagnosticLibrary.Metrics.MoreThanOneMeasurementValueDefined.Descriptor,
					isBlocking: false,
					GetParameterLocation(methodSymbol, measurementParameters[1].ParameterName)
				)
			);
		}

		// TSG4004: no measurement value defined for a non-auto-increment instrument.
		if (!instrument.InstrumentAttribute.IsAutoIncrement && instrument.MeasurementParameter is null)
			diagnostics.Add(
				ReportableDiagnostic.Create(
					DiagnosticLibrary.Metrics.NoMeasurementValueDefined.Descriptor,
					isBlocking: false,
					methodSymbol
				)
			);

		// TSG4005: observable instruments require a Func<T> parameter.
		if (instrument.IsObservable && !instrument.Parameters.Any(static p => p.IsFunc))
			diagnostics.Add(
				ReportableDiagnostic.Create(
					DiagnosticLibrary.Metrics.ObservableRequiredFunc.Descriptor,
					isBlocking: false,
					methodSymbol
				)
			);

		// TSG4006: the measurement parameter has an invalid measurement type.
		if (instrument.MeasurementParameter is { IsValidInstrumentType: false } measurement)
		{
			diagnostics.Add(
				ReportableDiagnostic.Create(
					DiagnosticLibrary.Metrics.InvalidMeasurementType.Descriptor,
					isBlocking: false,
					GetParameterLocation(methodSymbol, measurement.ParameterName)
				)
			);
		}

		// TSG4009: instrument name matches the instrument type name.
		var instrumentTypeName = instrument.InstrumentAttribute.TypeName;
		if (
			!string.IsNullOrWhiteSpace(instrument.MetricName)
			&& string.Equals(instrument.MetricName, instrumentTypeName, StringComparison.OrdinalIgnoreCase)
		)
			diagnostics.Add(
				ReportableDiagnostic.Create(
					DiagnosticLibrary.Metrics.InstrumentNameMatchesType.Descriptor,
					isBlocking: false,
					methodSymbol,
					instrument.MetricName
				)
			);
	}
}
