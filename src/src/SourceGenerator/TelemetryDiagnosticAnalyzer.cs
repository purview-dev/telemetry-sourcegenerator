using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Purview.Telemetry.SourceGenerator.Helpers;

namespace Purview.Telemetry.SourceGenerator;

/// <summary>
/// Raises all telemetry diagnostics. The generator only collects interface-level diagnostics to decide
/// <see cref="GeneratorResult{T}.ShouldProcess"/>; this analyzer is the
/// single source that reports them to the user.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TelemetryDiagnosticAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
		TelemetryRules.GetAllSupportedDescriptors();

	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Design",
		"CA1062:Validate arguments of public methods",
		Justification = "Contract states `context` is never null"
	)]
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
	}

	static void AnalyzeNamedType(SymbolAnalysisContext context)
	{
		if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Interface } interfaceSymbol)
			return;

		var token = context.CancellationToken;

		var hasActivitySource = Utilities.ContainsAttribute(
			interfaceSymbol,
			TemplateLibrary.Activities.ActivitySourceAttribute,
			token
		);
		var hasLogger = Utilities.ContainsAttribute(interfaceSymbol, TemplateLibrary.Logging.LoggerAttribute, token);
		var hasMeter = Utilities.ContainsAttribute(interfaceSymbol, TemplateLibrary.Metrics.MeterAttribute, token);

		if (!hasActivitySource && !hasLogger && !hasMeter)
			return;

		var compilation = context.Compilation;

		// Structural diagnostics (interface + method level).
		var structural = TelemetryRules.GetStructuralDiagnostics(interfaceSymbol, compilation, token);
		foreach (var diagnostic in structural)
			context.ReportDiagnostic(diagnostic.ToDiagnostic());

		// Domain diagnostics. The activity and meter rules reuse the pipeline transforms so the
		// parameter/instrument inference matches generation exactly.
		if (hasLogger)
		{
			var loggerDiagnostics = TelemetryRules.GetLoggerDiagnostics(interfaceSymbol, compilation, token);
			foreach (var diagnostic in loggerDiagnostics)
				context.ReportDiagnostic(diagnostic.ToDiagnostic());
		}

		if (hasActivitySource)
		{
			var result = PipelineHelpers.BuildActivityTarget(interfaceSymbol, compilation, null, token);
			if (result.HasValue && result.Value is { } activityTarget)
			{
				var diagnostics = TelemetryRules.GetActivityDiagnostics(activityTarget, interfaceSymbol, token);
				foreach (var diagnostic in diagnostics)
					context.ReportDiagnostic(diagnostic.ToDiagnostic());
			}
		}

		if (hasMeter)
		{
			var result = PipelineHelpers.BuildMeterTarget(interfaceSymbol, compilation, null, token);
			if (result.HasValue && result.Value is { } meterTarget)
			{
				var diagnostics = TelemetryRules.GetMeterDiagnostics(meterTarget, interfaceSymbol, token);
				foreach (var diagnostic in diagnostics)
					context.ReportDiagnostic(diagnostic.ToDiagnostic());
			}
		}
	}
}
