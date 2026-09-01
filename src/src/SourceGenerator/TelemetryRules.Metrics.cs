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
	public static ImmutableArray<DiagnosticInfo> GetMeterDiagnostics(
		MeterTarget target,
		INamedTypeSymbol interfaceSymbol,
		CancellationToken token
	)
	{
		var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

		foreach (var instrument in target.InstrumentationMethods)
		{
			token.ThrowIfCancellationRequested();

			if (!instrument.TargetGenerationState.IsValid)
				continue;

			var methodSymbol = FindMethod(interfaceSymbol, instrument.MethodName);
			if (methodSymbol is null)
				continue;

			// TSG4000: no instrument attribute defined.
			if (instrument.InstrumentAttribute is null)
			{
				diagnostics.Add(
					DiagnosticInfo.Create(ToDescriptor(DiagnosticLibrary.Metrics.NoInstrumentDefined), methodSymbol)
				);
				continue;
			}

			var methodTargets = instrument.TargetGenerationState.MethodTargets;
			var activityOwnsPublicMethod = methodTargets.HasFlag(GenerationType.Activities);
			var loggingOwnsPublicMethod = !activityOwnsPublicMethod && methodTargets.HasFlag(GenerationType.Logging);
			var metricsOwnsPublicMethod = !activityOwnsPublicMethod && !loggingOwnsPublicMethod;

			var isVoid = instrument.ReturnType.Identity.SpecialType == SpecialType.System_Void;

			// TSG4001: metrics-owned public method must return void or bool.
			if (metricsOwnsPublicMethod && !isVoid && !instrument.ReturnsBool)
				diagnostics.Add(
					DiagnosticInfo.Create(ToDescriptor(DiagnosticLibrary.Metrics.DoesNotReturnVoid), methodSymbol)
				);

			// TSG4007: observable instruments cannot return bool.
			if (instrument.IsObservable && instrument.ReturnsBool)
				diagnostics.Add(
					DiagnosticInfo.Create(
						ToDescriptor(DiagnosticLibrary.Metrics.ObservableCannotReturnBool),
						methodSymbol
					)
				);

			// TSG4008: auto-counter instruments must return void.
			if (instrument.InstrumentAttribute.IsAutoIncrement && instrument.ReturnsBool)
				diagnostics.Add(
					DiagnosticInfo.Create(
						ToDescriptor(DiagnosticLibrary.Metrics.AutoCounterMustReturnVoid),
						methodSymbol
					)
				);

			// TSG4002: auto-increment counter cannot also have a measurement parameter.
			if (instrument.InstrumentAttribute.IsAutoIncrement && instrument.MeasurementParameter != null)
				diagnostics.Add(
					DiagnosticInfo.Create(
						ToDescriptor(DiagnosticLibrary.Metrics.AutoIncrementCountAndMeasurementParam),
						methodSymbol
					)
				);

			// TSG4003: more than one measurement value defined.
			var measurementParameters = instrument.Parameters.Where(static p => p.IsMeasurement).ToImmutableArray();
			if (measurementParameters.Length > 1)
			{
				var secondMeasurement = FindParameter(methodSymbol, measurementParameters[1].ParameterName);
				var location =
					secondMeasurement?.Locations.FirstOrDefault(static l => l.IsInSource)
					?? methodSymbol.Locations.FirstOrDefault(static l => l.IsInSource)
					?? Location.None;
				diagnostics.Add(
					DiagnosticInfo.Create(
						ToDescriptor(DiagnosticLibrary.Metrics.MoreThanOneMeasurementValueDefined),
						location
					)
				);
			}

			// TSG4004: no measurement value defined for a non-auto-increment instrument.
			if (!instrument.InstrumentAttribute.IsAutoIncrement && instrument.MeasurementParameter is null)
				diagnostics.Add(
					DiagnosticInfo.Create(
						ToDescriptor(DiagnosticLibrary.Metrics.NoMeasurementValueDefined),
						methodSymbol
					)
				);

			// TSG4005: observable instruments require a Func<T> parameter.
			if (instrument.IsObservable && !instrument.Parameters.Any(static p => p.IsFunc))
				diagnostics.Add(
					DiagnosticInfo.Create(ToDescriptor(DiagnosticLibrary.Metrics.ObservableRequiredFunc), methodSymbol)
				);

			// TSG4006: the measurement parameter has an invalid measurement type.
			if (instrument.MeasurementParameter is { IsValidInstrumentType: false } measurement)
			{
				var measurementSymbol = FindParameter(methodSymbol, measurement.ParameterName);
				var location =
					measurementSymbol?.Locations.FirstOrDefault(static l => l.IsInSource)
					?? methodSymbol.Locations.FirstOrDefault(static l => l.IsInSource)
					?? Location.None;
				diagnostics.Add(
					DiagnosticInfo.Create(ToDescriptor(DiagnosticLibrary.Metrics.InvalidMeasurementType), location)
				);
			}

			// TSG4009: instrument name matches the instrument type name.
			var instrumentTypeName = GetInstrumentTypeName(instrument.InstrumentAttribute.InstrumentType);
			if (
				!string.IsNullOrWhiteSpace(instrument.MetricName)
				&& string.Equals(instrument.MetricName, instrumentTypeName, StringComparison.OrdinalIgnoreCase)
			)
				diagnostics.Add(
					DiagnosticInfo.Create(
						ToDescriptor(DiagnosticLibrary.Metrics.InstrumentNameMatchesType),
						methodSymbol,
						instrument.MetricName
					)
				);
		}

		return diagnostics.ToImmutable();
	}

	static string GetInstrumentTypeName(InstrumentTypes type) =>
		type switch
		{
			InstrumentTypes.Counter => "Counter",
			InstrumentTypes.UpDownCounter => "UpDownCounter",
			InstrumentTypes.Histogram => "Histogram",
			InstrumentTypes.ObservableCounter => "ObservableCounter",
			InstrumentTypes.ObservableGauge => "ObservableGauge",
			InstrumentTypes.ObservableUpDownCounter => "ObservableUpDownCounter",
			_ => type.ToString(),
		};
}
