using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

static partial class DiagnosticLibrary
{
	public static class Categories
	{
		public const string Usage = nameof(Usage);

		public static class Activity
		{
			public const string Usage = nameof(Activity) + "." + nameof(Usage);
		}

		public static class Logging
		{
			public const string Usage = nameof(Logging) + "." + nameof(Usage);
			public const string Performance = nameof(Logging) + "." + nameof(Performance);
		}

		public static class Metrics
		{
			public const string Usage = nameof(Metrics) + "." + nameof(Usage);
		}
	}

	public static void Report(
		Action<Diagnostic> report,
		TelemetryDiagnosticDescriptor telemetryDiagnostic,
		params object?[] args
	) => Report(report, telemetryDiagnostic, locations: null, args);

	public static void Report(
		Action<Diagnostic> report,
		TelemetryDiagnosticDescriptor telemetryDiagnostic,
		Location? location,
		params object?[] args
	) => Report(report, telemetryDiagnostic, location == null ? null : [location], args);

	public static void Report(
		Action<Diagnostic> report,
		TelemetryDiagnosticDescriptor telemetryDiagnostic,
		IEnumerable<Location>? locations,
		params object?[] args
	) => Report(report, telemetryDiagnostic, locations?.ToArray(), args);

	public static void Report(
		Action<Diagnostic> report,
		TelemetryDiagnosticDescriptor telemetryDiagnostic,
		Location[]? locations,
		params object?[] args
	)
	{
		var location = locations?.Length > 0 ? locations[0] : null;
		var additionalLocations = locations?.Length > 1 ? locations.AsSpan().Slice(1) : null;

		var diagnostic = Diagnostic.Create(
			new(
				id: telemetryDiagnostic.Id,
				title: telemetryDiagnostic.Title,
				messageFormat: telemetryDiagnostic.Description,
				category: telemetryDiagnostic.Category,
				defaultSeverity: telemetryDiagnostic.Severity,
				isEnabledByDefault: telemetryDiagnostic.EnabledByDefault
			),
			location,
			additionalLocations.ToArray(),
			args
		);

		report(diagnostic);
	}
}
