using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator;

/// <summary>
/// Shared diagnostic rules used by both the pipeline (to decide <see cref="GeneratorResult{T}.ShouldProcess"/>)
/// and the <see cref="TelemetryDiagnosticAnalyzer"/> (to raise the diagnostics). The generator collects the
/// interface-level results but never reports them; the analyzer is the sole raiser.
/// </summary>
static partial class TelemetryRules
{
	public static DiagnosticDescriptor ToDescriptor(TelemetryDiagnosticDescriptor descriptor) =>
		new(
			id: descriptor.Id,
			title: descriptor.Title,
			messageFormat: descriptor.Description,
			category: descriptor.Category,
			defaultSeverity: descriptor.Severity,
			isEnabledByDefault: descriptor.EnabledByDefault
		);

	public static ImmutableArray<DiagnosticDescriptor> GetAllSupportedDescriptors() =>
		[
			ToDescriptor(DiagnosticLibrary.General.FatalExecutionDuringExecution),
			ToDescriptor(DiagnosticLibrary.General.InferenceNotSupportedWithMultiTargeting),
			ToDescriptor(DiagnosticLibrary.General.MultiGenerationTargetsNotSupported),
			ToDescriptor(DiagnosticLibrary.General.DuplicateMethodNamesAreNotSupported),
			ToDescriptor(DiagnosticLibrary.General.GenericInterfacesNotSupported),
			ToDescriptor(DiagnosticLibrary.General.GenericMethodsNotSupported),
			ToDescriptor(DiagnosticLibrary.General.ExcludeTargetsTargetNotPresent),
			ToDescriptor(DiagnosticLibrary.General.ExcludeTargetsResultsInEmptyParameterSet),
			ToDescriptor(DiagnosticLibrary.General.ActivityParameterWithoutActivityTarget),
			ToDescriptor(DiagnosticLibrary.General.MethodTargetNotRegisteredOnInterface),
			ToDescriptor(DiagnosticLibrary.General.UnsupportedTargetFramework),
			ToDescriptor(DiagnosticLibrary.Logging.MultipleExceptionsDefined),
			ToDescriptor(DiagnosticLibrary.Logging.MaximumLogEntryParametersExceeded),
			ToDescriptor(DiagnosticLibrary.Logging.InferringErrorLogLevel),
			ToDescriptor(DiagnosticLibrary.Logging.MSLoggingNotReferenced),
			ToDescriptor(DiagnosticLibrary.Logging.MixedOrdinalAndNamedProperties),
			ToDescriptor(DiagnosticLibrary.Logging.OrdinalsExceedParameters),
			ToDescriptor(DiagnosticLibrary.Logging.ExpandEnumerableAndLogPropertiesNotSupported),
			ToDescriptor(DiagnosticLibrary.Logging.ScopedMethodShouldNotHaveLevel),
			ToDescriptor(DiagnosticLibrary.Logging.UnboundedIEnumerableMaxCount),
			ToDescriptor(DiagnosticLibrary.Logging.LogMustReturnVoidOrAsync),
			ToDescriptor(DiagnosticLibrary.Activities.BaggageParameterShouldBeString),
			ToDescriptor(DiagnosticLibrary.Activities.NoActivitySourceSpecified),
			ToDescriptor(DiagnosticLibrary.Activities.InvalidReturnType),
			ToDescriptor(DiagnosticLibrary.Activities.DuplicateParameterTypes),
			ToDescriptor(DiagnosticLibrary.Activities.ActivityParameterNotAllowed),
			ToDescriptor(DiagnosticLibrary.Activities.TimestampParameterNotAllowed),
			ToDescriptor(DiagnosticLibrary.Activities.StartTimeParameterNotAllowed),
			ToDescriptor(DiagnosticLibrary.Activities.ParentContextOrIdParameterNotAllowed),
			ToDescriptor(DiagnosticLibrary.Activities.LinksParameterNotAllowed),
			ToDescriptor(DiagnosticLibrary.Activities.TagsParameterNotAllowed),
			ToDescriptor(DiagnosticLibrary.Activities.EscapedParameterInvalidType),
			ToDescriptor(DiagnosticLibrary.Activities.EscapedParameterIsOnlyValidOnEvent),
			ToDescriptor(DiagnosticLibrary.Activities.NoActivityMethodsDefined),
			ToDescriptor(DiagnosticLibrary.Activities.DoesNotReturnActivity),
			ToDescriptor(DiagnosticLibrary.Activities.DoesNotAcceptActivityParameter),
			ToDescriptor(DiagnosticLibrary.Activities.ActivityShouldBeTheFirstParameter),
			ToDescriptor(DiagnosticLibrary.Activities.StatusDescriptionMustBeString),
			ToDescriptor(DiagnosticLibrary.Activities.StatusDescriptionParameterInvalidType),
			ToDescriptor(DiagnosticLibrary.Activities.ExceptionEventNotStandardName),
			ToDescriptor(DiagnosticLibrary.Activities.ActivityReturnTypeShouldBeNullable),
			ToDescriptor(DiagnosticLibrary.Metrics.NoInstrumentDefined),
			ToDescriptor(DiagnosticLibrary.Metrics.DoesNotReturnVoid),
			ToDescriptor(DiagnosticLibrary.Metrics.AutoIncrementCountAndMeasurementParam),
			ToDescriptor(DiagnosticLibrary.Metrics.MoreThanOneMeasurementValueDefined),
			ToDescriptor(DiagnosticLibrary.Metrics.NoMeasurementValueDefined),
			ToDescriptor(DiagnosticLibrary.Metrics.ObservableRequiredFunc),
			ToDescriptor(DiagnosticLibrary.Metrics.InvalidMeasurementType),
			ToDescriptor(DiagnosticLibrary.Metrics.ObservableCannotReturnBool),
			ToDescriptor(DiagnosticLibrary.Metrics.AutoCounterMustReturnVoid),
			ToDescriptor(DiagnosticLibrary.Metrics.InstrumentNameMatchesType),
		];

	static Location GetLocation(ISymbol symbol) =>
		symbol.Locations.FirstOrDefault(static location => location.IsInSource) ?? Location.None;

	static IMethodSymbol? FindMethod(INamedTypeSymbol interfaceSymbol, string methodName) =>
		interfaceSymbol.GetMembers(methodName).OfType<IMethodSymbol>().FirstOrDefault();

	static IParameterSymbol? FindParameter(IMethodSymbol method, string parameterName) =>
		method.Parameters.FirstOrDefault(p => string.Equals(p.Name, parameterName, StringComparison.Ordinal));

	static ImmutableArray<Location> GetLocations(ISymbol symbol) =>
		symbol.Locations.Where(static location => location.IsInSource).ToImmutableArray();

	/// <summary>
	/// Determines whether the target framework of the compilation is supported (net8.0+/net48+).
	/// A compilation with no target-framework symbols is treated as supported.
	/// </summary>
	public static bool IsUnsupportedTargetFramework(Compilation compilation)
	{
		foreach (var tree in compilation.SyntaxTrees)
		{
			if (tree.Options is CSharpParseOptions options)
			{
				var symbols = options.PreprocessorSymbolNames;
				return !symbols.Contains("NET8_0_OR_GREATER")
					&& !symbols.Contains("NET48_OR_GREATER")
					&& symbols.Any(static s => s.StartsWith("NET", StringComparison.Ordinal));
			}
		}

		return false;
	}

	/// <summary>
	/// Structural diagnostics for an interface and its methods. Includes interface-level rules
	/// (generic interface, duplicate names, unsupported framework, missing ILogger) and per-method
	/// rules (generic method, multi-target inference, missing interface source).
	/// </summary>
	public static ImmutableArray<DiagnosticInfo> GetStructuralDiagnostics(
		INamedTypeSymbol interfaceSymbol,
		Compilation compilation,
		CancellationToken token
	)
	{
		var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

		// TSG1011: unsupported target framework.
		if (IsUnsupportedTargetFramework(compilation))
			diagnostics.Add(
				DiagnosticInfo.Create(
					ToDescriptor(DiagnosticLibrary.General.UnsupportedTargetFramework),
					interfaceSymbol
				)
			);

		// TSG1004: generic interface - nothing further is meaningful.
		if (interfaceSymbol.Arity > 0)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					ToDescriptor(DiagnosticLibrary.General.GenericInterfacesNotSupported),
					interfaceSymbol
				)
			);
			return diagnostics.ToImmutable();
		}

		var hasActivitySource = Utilities.ContainsAttribute(
			interfaceSymbol,
			TemplateLibrary.Activities.ActivitySourceAttribute,
			token
		);
		var hasLogger = Utilities.ContainsAttribute(interfaceSymbol, TemplateLibrary.Logging.LoggerAttribute, token);
		var hasMeter = Utilities.ContainsAttribute(interfaceSymbol, TemplateLibrary.Metrics.MeterAttribute, token);

		if (!hasActivitySource && !hasLogger && !hasMeter)
			return diagnostics.ToImmutable();

		// TSG2003: MS logging not referenced.
		if (hasLogger)
		{
			var iLoggerSymbol = compilation.GetTypeByMetadataName(
				TypeLibrary.Logging.MicrosoftExtensions.ILogger.MetadataFullName
			);
			if (iLoggerSymbol is null)
				diagnostics.Add(
					DiagnosticInfo.Create(
						ToDescriptor(DiagnosticLibrary.Logging.MSLoggingNotReferenced),
						interfaceSymbol
					)
				);
		}

		// Gather methods grouped by name (for TSG1003 and per-method checks).
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

		// TSG1003: duplicate method names.
		foreach (var kvp in methodsByName)
		{
			var methods = kvp.Value;
			if (methods.Count <= 1)
				continue;

			var locations = methods.SelectMany(static m => m.Locations).ToImmutableArray();
			diagnostics.Add(
				DiagnosticInfo.Create(
					ToDescriptor(DiagnosticLibrary.General.DuplicateMethodNamesAreNotSupported),
					locations,
					kvp.Key
				)
			);
		}

		// Determine interface generation type for multi-target validation.
		var generationType =
			(hasActivitySource ? GenerationType.Activities : GenerationType.None)
			| (hasLogger ? GenerationType.Logging : GenerationType.None)
			| (hasMeter ? GenerationType.Metrics : GenerationType.None);

		// Per-method validation.
		foreach (var kvp in methodsByName)
		{
			var method = kvp.Value[0];

			token.ThrowIfCancellationRequested();

			if (Utilities.ContainsAttribute(method, TemplateLibrary.Shared.ExcludeAttribute, token))
				continue;

			// TSG1005: generic method.
			if (method.Arity > 0)
			{
				diagnostics.Add(
					DiagnosticInfo.Create(ToDescriptor(DiagnosticLibrary.General.GenericMethodsNotSupported), method)
				);
				continue;
			}

			var targetState = Utilities.IsValidGenerationTarget(method, generationType, generationType);

			if (targetState.RaiseInferenceNotSupportedWithMultiTargeting)
				diagnostics.Add(
					DiagnosticInfo.Create(
						ToDescriptor(DiagnosticLibrary.General.InferenceNotSupportedWithMultiTargeting),
						method
					)
				);

			if (targetState.RaiseMultiGenerationTargetsNotSupported)
				diagnostics.Add(
					DiagnosticInfo.Create(
						ToDescriptor(DiagnosticLibrary.General.MultiGenerationTargetsNotSupported),
						method
					)
				);

			if (targetState.RaiseMissingInterfaceSource)
				diagnostics.Add(
					DiagnosticInfo.Create(
						ToDescriptor(DiagnosticLibrary.General.MethodTargetNotRegisteredOnInterface),
						method
					)
				);

			// TSG1008: an Activity parameter on a method with no Activity target will be ignored.
			// Skip when the method is a valid inferred Activity method (single-target Activity
			// interface, no explicit attributes) - there the parameter is used, not ignored.
			if (
				targetState.ActivityParameterWithoutTarget is { } activityParameterName
				&& !(targetState.MethodTargets == GenerationType.None && generationType == GenerationType.Activities)
			)
			{
				var parameterSymbol = FindParameter(method, activityParameterName);
				var activityParameterLocation =
					parameterSymbol?.Locations.FirstOrDefault(static l => l.IsInSource)
					?? method.Locations.FirstOrDefault(static l => l.IsInSource)
					?? Location.None;

				diagnostics.Add(
					DiagnosticInfo.Create(
						ToDescriptor(DiagnosticLibrary.General.ActivityParameterWithoutActivityTarget),
						activityParameterLocation,
						activityParameterName
					)
				);
			}

			// TSG1006/TSG1007: [ExcludeTargets] validation against the method's target families.
			ApplyExcludeTargetsRules(method, targetState.MethodTargets, diagnostics, token);
		}

		return diagnostics.ToImmutable();
	}

	static void ApplyExcludeTargetsRules(
		IMethodSymbol method,
		GenerationType methodTargets,
		ImmutableArray<DiagnosticInfo>.Builder diagnostics,
		CancellationToken token
	)
	{
		if (methodTargets == GenerationType.None)
			return;

		var parameters = method.Parameters;
		var excludedPerParameter = parameters
			.Select(p =>
				(
					Parameter: p,
					Excluded: SharedHelpers.GetExcludeTargetsAttribute(p, null, null, token)?.ExcludedTargets
						?? GenerationType.None
				)
			)
			.ToImmutableArray();

		foreach (var target in Enum.GetValues(typeof(GenerationType)).Cast<GenerationType>())
		{
			token.ThrowIfCancellationRequested();

			if (target is GenerationType.None or GenerationType.All)
				continue;

			var methodTargetsFamily = methodTargets.HasFlag(target);

			// TSG1006: the exclusion references a family the method does not target.
			if (!methodTargetsFamily)
			{
				foreach (var (parameter, excluded) in excludedPerParameter)
				{
					if (!excluded.HasFlag(target))
						continue;

					var location = parameter.Locations.FirstOrDefault(static l => l.IsInSource) ?? Location.None;
					diagnostics.Add(
						DiagnosticInfo.Create(
							ToDescriptor(DiagnosticLibrary.General.ExcludeTargetsTargetNotPresent),
							location,
							GetGenerationTypeName(target)
						)
					);
				}

				continue;
			}

			// TSG1007: excluding every parameter from a targeted family yields an empty parameter set.
			if (parameters.Length > 0 && excludedPerParameter.All(ep => ep.Excluded.HasFlag(target)))
			{
				diagnostics.Add(
					DiagnosticInfo.Create(
						ToDescriptor(DiagnosticLibrary.General.ExcludeTargetsResultsInEmptyParameterSet),
						method,
						GetGenerationTypeName(target),
						method.Name
					)
				);
			}
		}
	}

	static string GetGenerationTypeName(GenerationType target) =>
		target switch
		{
			GenerationType.Activities => "Activities",
			GenerationType.Logging => "Logging",
			GenerationType.Metrics => "Metrics",
			_ => target.ToString(),
		};

	/// <summary>
	/// The subset of structural diagnostics that apply to the whole interface (used by the pipeline to
	/// gate <see cref="GeneratorResult{T}.ShouldProcess"/>): generic interface, duplicate method names and
	/// an unsupported target framework.
	/// </summary>
	public static ImmutableArray<DiagnosticInfo> GetInterfaceLevelDiagnostics(
		INamedTypeSymbol interfaceSymbol,
		Compilation compilation,
		CancellationToken token
	)
	{
		var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

		if (IsUnsupportedTargetFramework(compilation))
			diagnostics.Add(
				DiagnosticInfo.Create(
					ToDescriptor(DiagnosticLibrary.General.UnsupportedTargetFramework),
					interfaceSymbol
				)
			);

		if (interfaceSymbol.Arity > 0)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					ToDescriptor(DiagnosticLibrary.General.GenericInterfacesNotSupported),
					interfaceSymbol
				)
			);
			return diagnostics.ToImmutable();
		}

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

		foreach (var kvp in methodsByName)
		{
			token.ThrowIfCancellationRequested();

			if (kvp.Value.Count <= 1)
				continue;

			var locations = kvp.Value.SelectMany(static m => m.Locations).ToImmutableArray();
			diagnostics.Add(
				DiagnosticInfo.Create(
					ToDescriptor(DiagnosticLibrary.General.DuplicateMethodNamesAreNotSupported),
					locations,
					kvp.Key
				)
			);
		}

		return diagnostics.ToImmutable();
	}
}
