using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TelemetryDiagnosticAnalyzer : DiagnosticAnalyzer
{
	static readonly DiagnosticDescriptor _genericInterfacesNotSupported = ToDescriptor(
		TelemetryDiagnostics.General.GenericInterfacesNotSupported
	);
	static readonly DiagnosticDescriptor _genericMethodsNotSupported = ToDescriptor(
		TelemetryDiagnostics.General.GenericMethodsNotSupported
	);
	static readonly DiagnosticDescriptor _duplicateMethodNames = ToDescriptor(
		TelemetryDiagnostics.General.DuplicateMethodNamesAreNotSupported
	);
	static readonly DiagnosticDescriptor _inferenceNotSupportedWithMultiTargeting = ToDescriptor(
		TelemetryDiagnostics.General.InferenceNotSupportedWithMultiTargeting
	);
	static readonly DiagnosticDescriptor _multiGenerationTargetsNotSupported = ToDescriptor(
		TelemetryDiagnostics.General.MultiGenerationTargetsNotSupported
	);
	static readonly DiagnosticDescriptor _methodTargetNotRegisteredOnInterface = ToDescriptor(
		TelemetryDiagnostics.General.MethodTargetNotRegisteredOnInterface
	);
	static readonly DiagnosticDescriptor _msLoggingNotReferenced = ToDescriptor(
		TelemetryDiagnostics.Logging.MSLoggingNotReferenced
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
		[
			_genericInterfacesNotSupported,
			_genericMethodsNotSupported,
			_duplicateMethodNames,
			_inferenceNotSupportedWithMultiTargeting,
			_multiGenerationTargetsNotSupported,
			_methodTargetNotRegisteredOnInterface,
			_msLoggingNotReferenced,
		];

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
				context.ReportDiagnostic(Diagnostic.Create(_genericInterfacesNotSupported, location));
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
				Diagnostic.Create(_duplicateMethodNames, primary, additional, kvp.Key)
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
					context.ReportDiagnostic(Diagnostic.Create(_msLoggingNotReferenced, location));
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
					context.ReportDiagnostic(Diagnostic.Create(_genericMethodsNotSupported, location));
				continue;
			}

			// Multi-target validation (TSG1001, TSG1002, TSG1010)
			var targetState = Utilities.IsValidGenerationTarget(method, generationType, generationType);

			if (targetState.RaiseInferenceNotSupportedWithMultiTargeting)
			{
				foreach (var location in method.Locations)
					context.ReportDiagnostic(
						Diagnostic.Create(_inferenceNotSupportedWithMultiTargeting, location)
					);
			}

			if (targetState.RaiseMultiGenerationTargetsNotSupported)
			{
				foreach (var location in method.Locations)
					context.ReportDiagnostic(
						Diagnostic.Create(_multiGenerationTargetsNotSupported, location)
					);
			}

			if (targetState.RaiseMissingInterfaceSource)
			{
				foreach (var location in method.Locations)
					context.ReportDiagnostic(
						Diagnostic.Create(_methodTargetNotRegisteredOnInterface, location)
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
