using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Purview.Telemetry.SourceGenerator.Analyzers;
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
	public static ImmutableArray<DiagnosticDescriptor> GetAllSupportedDescriptors() =>
		[
			DiagnosticLibrary.General.FatalExecutionDuringExecution.Descriptor,
			DiagnosticLibrary.General.InferenceNotSupportedWithMultiTargeting.Descriptor,
			DiagnosticLibrary.General.MultiGenerationTargetsNotSupported.Descriptor,
			DiagnosticLibrary.General.DuplicateMethodNamesAreNotSupported.Descriptor,
			DiagnosticLibrary.General.GenericInterfacesNotSupported.Descriptor,
			DiagnosticLibrary.General.GenericMethodsNotSupported.Descriptor,
			DiagnosticLibrary.General.ExcludeTargetsTargetNotPresent.Descriptor,
			DiagnosticLibrary.General.ExcludeTargetsResultsInEmptyParameterSet.Descriptor,
			DiagnosticLibrary.General.ActivityParameterWithoutActivityTarget.Descriptor,
			DiagnosticLibrary.General.MethodTargetNotRegisteredOnInterface.Descriptor,
			DiagnosticLibrary.General.UnsupportedTargetFramework.Descriptor,
			DiagnosticLibrary.Logging.MultipleExceptionsDefined.Descriptor,
			DiagnosticLibrary.Logging.MaximumLogEntryParametersExceeded.Descriptor,
			DiagnosticLibrary.Logging.InferringErrorLogLevel.Descriptor,
			DiagnosticLibrary.Logging.MSLoggingNotReferenced.Descriptor,
			DiagnosticLibrary.Logging.MixedOrdinalAndNamedProperties.Descriptor,
			DiagnosticLibrary.Logging.OrdinalsExceedParameters.Descriptor,
			DiagnosticLibrary.Logging.ExpandEnumerableAndLogPropertiesNotSupported.Descriptor,
			DiagnosticLibrary.Logging.ScopedMethodShouldNotHaveLevel.Descriptor,
			DiagnosticLibrary.Logging.UnboundedIEnumerableMaxCount.Descriptor,
			DiagnosticLibrary.Logging.LogMustReturnVoidOrAsync.Descriptor,
			DiagnosticLibrary.Activities.BaggageParameterShouldBeString.Descriptor,
			DiagnosticLibrary.Activities.NoActivitySourceSpecified.Descriptor,
			DiagnosticLibrary.Activities.InvalidReturnType.Descriptor,
			DiagnosticLibrary.Activities.DuplicateParameterTypes.Descriptor,
			DiagnosticLibrary.Activities.ActivityParameterNotAllowed.Descriptor,
			DiagnosticLibrary.Activities.TimestampParameterNotAllowed.Descriptor,
			DiagnosticLibrary.Activities.StartTimeParameterNotAllowed.Descriptor,
			DiagnosticLibrary.Activities.ParentContextOrIdParameterNotAllowed.Descriptor,
			DiagnosticLibrary.Activities.LinksParameterNotAllowed.Descriptor,
			DiagnosticLibrary.Activities.TagsParameterNotAllowed.Descriptor,
			DiagnosticLibrary.Activities.EscapedParameterInvalidType.Descriptor,
			DiagnosticLibrary.Activities.EscapedParameterIsOnlyValidOnEvent.Descriptor,
			DiagnosticLibrary.Activities.NoActivityMethodsDefined.Descriptor,
			DiagnosticLibrary.Activities.DoesNotReturnActivity.Descriptor,
			DiagnosticLibrary.Activities.DoesNotAcceptActivityParameter.Descriptor,
			DiagnosticLibrary.Activities.ActivityShouldBeTheFirstParameter.Descriptor,
			DiagnosticLibrary.Activities.StatusDescriptionMustBeString.Descriptor,
			DiagnosticLibrary.Activities.StatusDescriptionParameterInvalidType.Descriptor,
			DiagnosticLibrary.Activities.ExceptionEventNotStandardName.Descriptor,
			DiagnosticLibrary.Activities.ActivityReturnTypeShouldBeNullable.Descriptor,
			DiagnosticLibrary.Metrics.NoInstrumentDefined.Descriptor,
			DiagnosticLibrary.Metrics.DoesNotReturnVoid.Descriptor,
			DiagnosticLibrary.Metrics.AutoIncrementCountAndMeasurementParam.Descriptor,
			DiagnosticLibrary.Metrics.MoreThanOneMeasurementValueDefined.Descriptor,
			DiagnosticLibrary.Metrics.NoMeasurementValueDefined.Descriptor,
			DiagnosticLibrary.Metrics.ObservableRequiredFunc.Descriptor,
			DiagnosticLibrary.Metrics.InvalidMeasurementType.Descriptor,
			DiagnosticLibrary.Metrics.ObservableCannotReturnBool.Descriptor,
			DiagnosticLibrary.Metrics.AutoCounterMustReturnVoid.Descriptor,
			DiagnosticLibrary.Metrics.InstrumentNameMatchesType.Descriptor,
		];

	static IMethodSymbol? FindMethod(INamedTypeSymbol interfaceSymbol, string methodName) =>
		interfaceSymbol.GetMembers(methodName).OfType<IMethodSymbol>().FirstOrDefault();

	static IParameterSymbol? FindParameter(IMethodSymbol method, string parameterName) =>
		method.Parameters.FirstOrDefault(p => string.Equals(p.Name, parameterName, StringComparison.Ordinal));

	static Location GetParameterLocation(IMethodSymbol method, string? parameterName) =>
		(parameterName is null ? null : FindParameter(method, parameterName))?.Locations.FirstOrDefault(static l =>
			l.IsInSource
		)
		?? method.Locations.FirstOrDefault(static l => l.IsInSource)
		?? Location.None;

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
				DiagnosticInfo.Create(DiagnosticLibrary.General.UnsupportedTargetFramework.Descriptor, interfaceSymbol)
			);

		// TSG1004: generic interface - nothing further is meaningful.
		if (interfaceSymbol.Arity > 0)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.General.GenericInterfacesNotSupported.Descriptor,
					interfaceSymbol
				)
			);
			return diagnostics.ToImmutable();
		}

		var hasActivitySource = Utilities.ContainsAttribute(
			interfaceSymbol,
			TypeLibrary.Activities.ActivitySourceAttribute,
			token
		);
		var hasLogger = Utilities.ContainsAttribute(interfaceSymbol, TypeLibrary.Logging.LoggerAttribute, token);
		var hasMeter = Utilities.ContainsAttribute(interfaceSymbol, TypeLibrary.Metrics.MeterAttribute, token);

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
					DiagnosticInfo.Create(DiagnosticLibrary.Logging.MSLoggingNotReferenced.Descriptor, interfaceSymbol)
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
					DiagnosticLibrary.General.DuplicateMethodNamesAreNotSupported.Descriptor,
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
			token.ThrowIfCancellationRequested();
			ApplyPerMethodRules(kvp.Value[0], generationType, diagnostics, token);
		}

		return diagnostics.ToImmutable();
	}

	static void ApplyPerMethodRules(
		IMethodSymbol method,
		GenerationType generationType,
		ImmutableArray<DiagnosticInfo>.Builder diagnostics,
		CancellationToken token
	)
	{
		if (TypeHelpers.HasAttribute(method, TypeLibrary.TelemetryShared.ExcludeAttribute))
			return;

		// TSG1005: generic method.
		if (method.Arity > 0)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(DiagnosticLibrary.General.GenericMethodsNotSupported.Descriptor, method)
			);
			return;
		}

		var targetState = Utilities.IsValidGenerationTarget(method, generationType, generationType);

		if (targetState.RaiseInferenceNotSupportedWithMultiTargeting)
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.General.InferenceNotSupportedWithMultiTargeting.Descriptor,
					method
				)
			);

		if (targetState.RaiseMultiGenerationTargetsNotSupported)
			diagnostics.Add(
				DiagnosticInfo.Create(DiagnosticLibrary.General.MultiGenerationTargetsNotSupported.Descriptor, method)
			);

		if (targetState.RaiseMissingInterfaceSource)
			diagnostics.Add(
				DiagnosticInfo.Create(DiagnosticLibrary.General.MethodTargetNotRegisteredOnInterface.Descriptor, method)
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
					DiagnosticLibrary.General.ActivityParameterWithoutActivityTarget.Descriptor,
					activityParameterLocation,
					activityParameterName
				)
			);
		}

		// TSG1006/TSG1007: [ExcludeTargets] validation against the method's target families.
		ApplyExcludeTargetsRules(method, targetState.MethodTargets, diagnostics, token);
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
					Excluded: SharedHelpers.GetExcludeTargetsAttribute(p, token)?.ExcludedTargets ?? GenerationType.None
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
							DiagnosticLibrary.General.ExcludeTargetsTargetNotPresent.Descriptor,
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
						DiagnosticLibrary.General.ExcludeTargetsResultsInEmptyParameterSet.Descriptor,
						method,
						GetGenerationTypeName(target),
						method.Name
					)
				);
			}
		}
	}

#pragma warning disable IDE0072 // Add missing cases
	static string GetGenerationTypeName(GenerationType target) =>
		target switch
		{
			GenerationType.Activities => "Activities",
			GenerationType.Logging => "Logging",
			GenerationType.Metrics => "Metrics",
			_ => target.ToString(),
		};
#pragma warning restore IDE0072 // Add missing cases

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
				DiagnosticInfo.Create(DiagnosticLibrary.General.UnsupportedTargetFramework.Descriptor, interfaceSymbol)
			);

		if (interfaceSymbol.Arity > 0)
		{
			diagnostics.Add(
				DiagnosticInfo.Create(
					DiagnosticLibrary.General.GenericInterfacesNotSupported.Descriptor,
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
					DiagnosticLibrary.General.DuplicateMethodNamesAreNotSupported.Descriptor,
					locations,
					kvp.Key
				)
			);
		}

		return diagnostics.ToImmutable();
	}
}
