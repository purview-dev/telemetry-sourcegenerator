using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TelemetryDiagnosticAnalyzer : DiagnosticAnalyzer
{
	static readonly DiagnosticDescriptor GenericInterfacesNotSupported = ToDescriptor(
		TelemetryDiagnostics.General.GenericInterfacesNotSupported
	);
	static readonly DiagnosticDescriptor GenericMethodsNotSupported = ToDescriptor(
		TelemetryDiagnostics.General.GenericMethodsNotSupported
	);
	static readonly DiagnosticDescriptor DuplicateMethodNames = ToDescriptor(
		TelemetryDiagnostics.General.DuplicateMethodNamesAreNotSupported
	);
	static readonly DiagnosticDescriptor InferenceNotSupportedWithMultiTargeting = ToDescriptor(
		TelemetryDiagnostics.General.InferenceNotSupportedWithMultiTargeting
	);
	static readonly DiagnosticDescriptor MultiGenerationTargetsNotSupported = ToDescriptor(
		TelemetryDiagnostics.General.MultiGenerationTargetsNotSupported
	);
	static readonly DiagnosticDescriptor MethodTargetNotRegisteredOnInterface = ToDescriptor(
		TelemetryDiagnostics.General.MethodTargetNotRegisteredOnInterface
	);
	static readonly DiagnosticDescriptor MsLoggingNotReferenced = ToDescriptor(
		TelemetryDiagnostics.Logging.MSLoggingNotReferenced
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
	[
		GenericInterfacesNotSupported,
		GenericMethodsNotSupported,
		DuplicateMethodNames,
		InferenceNotSupportedWithMultiTargeting,
		MultiGenerationTargetsNotSupported,
		MethodTargetNotRegisteredOnInterface,
		MsLoggingNotReferenced,
	];

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
			Constants.Activities.ActivitySourceAttribute,
			token
		);
		var hasLogger = Utilities.ContainsAttribute(
			interfaceSymbol,
			Constants.Logging.LoggerAttribute,
			token
		);
		var hasMeter = Utilities.ContainsAttribute(
			interfaceSymbol,
			Constants.Metrics.MeterAttribute,
			token
		);

		if (!hasActivitySource && !hasLogger && !hasMeter)
			return;

		// TSG1004: Generic interface — report and bail; nothing further is meaningful
		if (interfaceSymbol.Arity > 0)
		{
			foreach (var location in interfaceSymbol.Locations)
				context.ReportDiagnostic(
					Diagnostic.Create(GenericInterfacesNotSupported, location)
				);
			return;
		}

		// Gather all methods grouped by name (needed by TSG1003 and per-method checks below)
		var methodsByName = new Dictionary<string, List<IMethodSymbol>>(StringComparer.Ordinal);
		foreach (var member in interfaceSymbol.GetMembers())
		{
			if (member is not IMethodSymbol method)
				continue;

			if (!methodsByName.TryGetValue(method.Name, out var list))
			{
				list = [];
				methodsByName[method.Name] = list;
			}

			list.Add(method);
		}

		// TSG1003: Duplicate method names (structural, independent of ILogger availability)
		foreach (var kvp in methodsByName)
		{
			var methods = kvp.Value;

			if (methods.Count <= 1)
				continue;

			var allLocations = methods.SelectMany(m => m.Locations).ToArray();
			var primary = allLocations.Length > 0 ? allLocations[0] : null;
			var additional = allLocations.Length > 1 ? allLocations.Skip(1).ToArray() : [];

			context.ReportDiagnostic(
				Diagnostic.Create(DuplicateMethodNames, primary, additional, kvp.Key)
			);
		}

		// TSG2003: MS Logging not referenced — report but do NOT return early so that
		// structural diagnostics (TSG1003, TSG1005, etc.) are still produced.
		if (hasLogger)
		{
			var iLoggerSymbol = context.Compilation.GetTypeByMetadataName(
				Constants.Logging.MicrosoftExtensions.ILogger.FullyQualifiedName
			);
			if (iLoggerSymbol is null)
			{
				foreach (var location in interfaceSymbol.Locations)
					context.ReportDiagnostic(Diagnostic.Create(MsLoggingNotReferenced, location));
			}
		}

		// Determine interface generation type for multi-target validation
		var generationType =
			(hasActivitySource ? GenerationType.Activities : GenerationType.None)
			| (hasLogger ? GenerationType.Logging : GenerationType.None)
			| (hasMeter ? GenerationType.Metrics : GenerationType.None);

		// Per-method validation
		foreach (var kvp in methodsByName)
		{
			var methods = kvp.Value;

			// Use first method for validation (duplicates already reported above)
			var method = methods[0];

			token.ThrowIfCancellationRequested();

			if (Utilities.ContainsAttribute(method, Constants.Shared.ExcludeAttribute, token))
				continue;

			// TSG1005: Generic method
			if (method.Arity > 0)
			{
				foreach (var location in method.Locations)
					context.ReportDiagnostic(
						Diagnostic.Create(GenericMethodsNotSupported, location)
					);
				continue;
			}

			// Multi-target validation (TSG1001, TSG1002, TSG1010)
			var targetState = Utilities.IsValidGenerationTarget(
				method,
				generationType,
				generationType
			);

			if (targetState.RaiseInferenceNotSupportedWithMultiTargeting)
			{
				foreach (var location in method.Locations)
					context.ReportDiagnostic(
						Diagnostic.Create(InferenceNotSupportedWithMultiTargeting, location)
					);
			}

			if (targetState.RaiseMultiGenerationTargetsNotSupported)
			{
				foreach (var location in method.Locations)
					context.ReportDiagnostic(
						Diagnostic.Create(MultiGenerationTargetsNotSupported, location)
					);
			}

			if (targetState.RaiseMissingInterfaceSource)
			{
				foreach (var location in method.Locations)
					context.ReportDiagnostic(
						Diagnostic.Create(MethodTargetNotRegisteredOnInterface, location)
					);
			}
		}
	}

	static DiagnosticDescriptor ToDescriptor(TelemetryDiagnosticDescriptor descriptor) =>
		new(
			id: descriptor.Id,
			title: descriptor.Title,
			messageFormat: descriptor.Description,
			category: descriptor.Category,
			defaultSeverity: descriptor.Severity,
			isEnabledByDefault: descriptor.EnabledByDefault
		);
}
