using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class PipelineHelpers
{
	public static bool HasActivityTargetAttribute(SyntaxNode _, CancellationToken __) => true;

	public static GeneratorResult<ActivitySourceTarget?> BuildActivityTransform(
		GeneratorAttributeSyntaxContext context,
		ISourceGenLogger? logger,
		CancellationToken token
	) => BuildActivityTarget(context.TargetSymbol as INamedTypeSymbol, context.SemanticModel, logger, token);

	public static GeneratorResult<ActivitySourceTarget?> BuildActivityTarget(
		INamedTypeSymbol? interfaceSymbol,
		SemanticModel semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		if (interfaceSymbol is null)
		{
			logger?.Fatal($"Could not find the interface symbol for an Activity target.");
			return GeneratorResult<ActivitySourceTarget?>.Empty;
		}

		if (interfaceSymbol.Arity > 0)
		{
			logger?.Diagnostic($"Cannot generate a Activity target for a generic interface '{interfaceSymbol.Name}'.");

			return GeneratorResult<ActivitySourceTarget?>.Create(
				DiagnosticInfo.Create(
					TelemetryRules.ToDescriptor(DiagnosticLibrary.General.GenericInterfacesNotSupported),
					interfaceSymbol
				)
			);
		}

		var activitySourceAttribute = SharedHelpers.GetActivitySourceAttribute(
			interfaceSymbol,
			semanticModel,
			logger,
			token
		);
		if (activitySourceAttribute == null)
		{
			logger?.Fatal(
				$"Could not find {TemplateLibrary.Activities.ActivitySourceAttribute} when one was expected '{interfaceSymbol.Name}'."
			);
			return GeneratorResult<ActivitySourceTarget?>.Empty;
		}

		var telemetryGeneration = SharedHelpers.GetTelemetryGenerationAttribute(
			interfaceSymbol,
			semanticModel,
			logger,
			token
		);
		var className = telemetryGeneration.ClassName.IsSet
			? telemetryGeneration.ClassName.Value!
			: GenerateClassName(interfaceSymbol.Name);

		var activitySourceGenerationAttribute = SharedHelpers.GetActivitySourceGenerationAttribute(
			semanticModel,
			logger,
			token
		);
		var activitySourceName =
			activitySourceGenerationAttribute?.Name.IsSet == true ? activitySourceGenerationAttribute.Name.Value!
			: activitySourceAttribute.Name.IsSet ? activitySourceAttribute.Name.Value!
			: null;

		// Get naming convention from TelemetryGenerationAttribute (default to OpenTelemetry = 1)
		var namingConvention = telemetryGeneration.NamingConvention.Value ?? 1;
		var isLegacy = namingConvention == 0;

		if (activitySourceName == null)
		{
			var assemblyName = semanticModel.Compilation.AssemblyName;
			if (!string.IsNullOrWhiteSpace(assemblyName))
			{
				// Legacy mode: lowercase the assembly name
				// OpenTelemetry mode: preserve casing
#pragma warning disable CA1308 // Intentional lowercase for legacy compatibility
				activitySourceName = isLegacy ? assemblyName!.ToLowerInvariant() : assemblyName;
#pragma warning restore CA1308
			}
		}

		var interfaceDeclaration =
			interfaceSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(token) as InterfaceDeclarationSyntax;
		if (interfaceDeclaration is null)
		{
			logger?.Fatal($"Could not locate the declaring syntax for '{interfaceSymbol.Name}'.");
			return GeneratorResult<ActivitySourceTarget?>.Empty;
		}

		var fullNamespace = Utilities.GetFullNamespace(interfaceDeclaration, true);
		var generationType = SharedHelpers.GetGenerationTypes(interfaceSymbol, token);
		var activityMethods = BuildActivityMethods(
			generationType,
			activitySourceAttribute,
			activitySourceGenerationAttribute,
			telemetryGeneration,
			semanticModel,
			interfaceSymbol,
			logger,
			token
		);

		return GeneratorResult<ActivitySourceTarget?>.Create(
			new(
				TelemetryGeneration: telemetryGeneration,
				GenerationType: generationType,
				ClassNameToGenerate: className,
				ClassNamespace: Utilities.GetNamespace(interfaceDeclaration),
				ParentClasses: Utilities.GetParentClasses(interfaceDeclaration),
				FullNamespace: fullNamespace,
				FullyQualifiedName: fullNamespace + className,
				InterfaceType: TypeReference.Create(interfaceSymbol),
				ActivitySourceGenerationAttribute: activitySourceGenerationAttribute,
				ActivitySourceName: activitySourceName,
				ActivityMethods: activityMethods,
				ActivityTargetAttributeRecord: activitySourceAttribute
			),
			TelemetryRules.GetInterfaceLevelDiagnostics(interfaceSymbol, semanticModel.Compilation, token)
		);
	}

	static ImmutableArray<ActivityBasedGenerationTarget> BuildActivityMethods(
		GenerationType generationType,
		ActivitySourceAttributeRecord activitySourceAttribute,
		ActivitySourceGenerationAttributeRecord? activitySourceGenerationAttribute,
		TelemetryGenerationAttributeRecord telemetryGeneration,
		SemanticModel semanticModel,
		INamedTypeSymbol interfaceSymbol,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		// Get naming convention from TelemetryGenerationAttribute (default to OpenTelemetry = 1)
		var namingConvention = telemetryGeneration?.NamingConvention.Value ?? 1;

		var prefix = GeneratePrefix(activitySourceGenerationAttribute, activitySourceAttribute, token);
		var defaultToTags =
			activitySourceGenerationAttribute?.DefaultToTags.IsSet == true
				? activitySourceGenerationAttribute.DefaultToTags.Value!.Value
				: activitySourceAttribute.DefaultToTags?.IsSet != true
					|| activitySourceAttribute.DefaultToTags.Value!.Value; // Default value

		var lowercaseBaggageAndTagKeys =
			activitySourceAttribute.LowercaseBaggageAndTagKeys?.IsSet != true
			|| activitySourceAttribute.LowercaseBaggageAndTagKeys.Value!.Value; // Default value

		List<ActivityBasedGenerationTarget> methodTargets = [];
		foreach (var method in GetAllInterfaceMethods(interfaceSymbol, semanticModel.Compilation, token))
		{
			token.ThrowIfCancellationRequested();

			if (method.Arity > 0)
			{
				methodTargets.Add(
					new(
						MethodName: method.Name,
						ReturnType: TypeReference.Create(method.ReturnType),
						ActivityOrEventName: method.Name,
						HasActivityParameter: false,
						ActivityAttribute: null,
						EventAttribute: null,
						MethodType: ActivityMethodType.Activity,
						Parameters: ImmutableArray<ActivityBasedParameterTarget>.Empty,
						Baggage: ImmutableArray<ActivityBasedParameterTarget>.Empty,
						Tags: ImmutableArray<ActivityBasedParameterTarget>.Empty,
						TargetGenerationState: new TargetGeneration(
							IsValid: false,
							RaiseInferenceNotSupportedWithMultiTargeting: false,
							RaiseMultiGenerationTargetsNotSupported: false
						),
						TypeParameters: ImmutableArray.CreateRange(method.TypeParameters, tp => tp.Name)
					)
				);
				continue;
			}

			if (Utilities.ContainsAttribute(method, TemplateLibrary.Shared.ExcludeAttribute, token))
			{
				logger?.Debug($"Skipping {interfaceSymbol.Name}.{method.Name}, explicitly excluded.");
				continue;
			}

			var (methodType, isInferred) = GetMethodType(
				method,
				semanticModel,
				logger,
				token,
				out var activityAttribute,
				out var eventAttribute
			);
			var activityOrEventName =
				activityAttribute?.Name.IsSet == true ? activityAttribute.Value.Name.Value : eventAttribute?.Name.Value;

			if (string.IsNullOrWhiteSpace(activityOrEventName))
				activityOrEventName = method.Name;

			logger?.Debug($"Found {methodType} method {interfaceSymbol.Name}.{method.Name}.");

			var parameters = GetActivityParameters(
				method,
				prefix,
				defaultToTags,
				lowercaseBaggageAndTagKeys,
				namingConvention,
				semanticModel,
				logger,
				token
			);
			var baggageParameters = parameters
				.Where(m => m.ParamDestination == ActivityParameterDestination.Baggage)
				.ToImmutableArray();
			var tagParameters = parameters
				.Where(m => m.ParamDestination == ActivityParameterDestination.Tag)
				.ToImmutableArray();

			var targetGenerationState = Utilities.IsValidGenerationTarget(
				method,
				generationType,
				GenerationType.Activities
			);
			if (targetGenerationState.RaiseMissingInterfaceSource)
			{
				logger?.Debug(
					$"Identified {interfaceSymbol.Name}.{method.Name} as problematic as the interface is missing source attribute(s) for the method's target(s)."
				);
			}

			methodTargets.Add(
				new(
					MethodName: method.Name,
					ReturnType: TypeReference.Create(method.ReturnType),
					ActivityOrEventName: activityOrEventName!,
					HasActivityParameter: parameters.Any(m =>
						m.ParameterType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.Activity)
					),
					ActivityAttribute: activityAttribute,
					EventAttribute: eventAttribute,
					MethodType: methodType,
					Parameters: parameters,
					Baggage: baggageParameters,
					Tags: tagParameters,
					TargetGenerationState: targetGenerationState
				)
			);
		}

		return [.. methodTargets];
	}

	static ImmutableArray<ActivityBasedParameterTarget> GetActivityParameters(
		IMethodSymbol method,
		string? prefix,
		bool defaultToTags,
		bool lowercaseBaggageAndTagKeys,
		int namingConvention,
		SemanticModel semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		List<ActivityBasedParameterTarget> parameterTargets = [];
		foreach (var parameter in method.Parameters)
		{
			token.ThrowIfCancellationRequested();

			var parameterType = TypeReference.Create(parameter.Type);
			var destination = defaultToTags ? ActivityParameterDestination.Tag : ActivityParameterDestination.Baggage;
			if (
				Utilities.TryContainsAttribute(parameter, TemplateLibrary.Shared.TagAttribute, token, out var attribute)
			)
			{
				logger?.Debug($"Found explicit tag: {parameter.Name}.");
				destination = ActivityParameterDestination.Tag;
			}
			else if (
				Utilities.TryContainsAttribute(
					parameter,
					TemplateLibrary.Activities.BaggageAttribute,
					token,
					out attribute
				)
			)
			{
				logger?.Debug($"Found explicit baggage: {parameter.Name}.");
				destination = ActivityParameterDestination.Baggage;
			}
			else if (Utilities.ContainsAttribute(parameter, TemplateLibrary.Activities.EscapeAttribute, token))
			{
				logger?.Debug($"Found escape parameter: {parameter.Name}.");
				destination = ActivityParameterDestination.Escape;
			}
			else if (
				Utilities.ContainsAttribute(parameter, TemplateLibrary.Activities.StatusDescriptionAttribute, token)
			)
			{
				logger?.Debug($"Found status description parameter: {parameter.Name}.");
				destination = ActivityParameterDestination.StatusDescription;
			}
			else if (parameterType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.Activity))
			{
				destination = ActivityParameterDestination.Activity;
			}
			else if (
				parameterType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.ActivityTagsCollection)
				|| TypeLibrary.Activities.SystemDiagnostics.ActivityTagIEnumerable.Equals(parameterType)
				|| parameterType.Identity.Equals(TypeLibrary.System.TagList)
			)
			{
				destination = ActivityParameterDestination.TagsEnumerable;
			}
			else if (
				parameterType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.ActivityContext)
				|| (
					parameter.Name == PropertyLibrary.Activities.ParentIdParameterName
					&& parameterType.Identity.SpecialType == SpecialType.System_String
				)
			)
			{
				destination = ActivityParameterDestination.ParentContextOrId;
			}
			else if (
				parameterType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.ActivityLinkArray)
				|| TypeLibrary.Activities.SystemDiagnostics.ActivityLinkIEnumerable.Equals(parameterType)
			)
			{
				destination = ActivityParameterDestination.LinksEnumerable;
			}
			else if (
				parameter.Name == PropertyLibrary.Activities.StartTimeParameterName
				&& parameterType.Identity.Equals(TypeLibrary.System.DateTimeOffset)
			)
			{
				destination = ActivityParameterDestination.StartTime;
			}
			else if (
				parameter.Name == PropertyLibrary.Activities.TimeStampParameterName
				&& parameterType.Identity.Equals(TypeLibrary.System.DateTimeOffset)
			)
			{
				destination = ActivityParameterDestination.Timestamp;
			}
			else
			{
				// destination is already set to default.
				logger?.Debug($"Inferring {(defaultToTags ? "tag" : "baggage")}: {parameter.Name}.");
			}

			TagOrBaggageAttributeRecord? tagOrBaggageAttribute = null;
			if (attribute != null)
			{
				tagOrBaggageAttribute = SharedHelpers.GetTagOrBaggageAttribute(attribute, semanticModel, logger, token);
			}

			// Check for ExcludeTargetsAttribute
			var excludeTargets = SharedHelpers.GetExcludeTargetsAttribute(parameter, semanticModel, logger, token);

			var parameterName = parameter.Name;
			var generatedName = GenerateParameterName(
				tagOrBaggageAttribute?.Name.Value ?? parameterName,
				prefix,
				lowercaseBaggageAndTagKeys,
				namingConvention
			);

			parameterTargets.Add(
				new(
					ParameterName: parameterName,
					ParameterType: parameterType,
					GeneratedName: generatedName,
					ParamDestination: destination,
					SkipOnNullOrEmpty: GetSkipOnNullOrEmptyValue(tagOrBaggageAttribute),
					IsException: parameter.Type.IsExceptionType(),
					ExcludedTargets: excludeTargets?.ExcludedTargets ?? GenerationType.None
				)
			);
		}

		return [.. parameterTargets];
	}

	static (ActivityMethodType, bool) GetMethodType(
		IMethodSymbol method,
		SemanticModel semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token,
		out ActivityAttributeRecord? activityAttribute,
		out EventAttributeRecord? eventAttribute
	)
	{
		eventAttribute = null;

		token.ThrowIfCancellationRequested();

		activityAttribute = SharedHelpers.GetActivityGenAttribute(method, semanticModel, logger, token);
		if (activityAttribute != null)
		{
			logger?.Debug($"Found explicit activity: {method.Name}.");
			return (ActivityMethodType.Activity, false);
		}

		eventAttribute = SharedHelpers.GetActivityEventAttribute(method, semanticModel, logger, token);
		if (eventAttribute != null)
		{
			logger?.Debug($"Found explicit event: {method.Name}.");
			return (ActivityMethodType.Event, false);
		}

		if (Utilities.ContainsAttribute(method, TemplateLibrary.Activities.ContextAttribute, token))
		{
			logger?.Debug($"Found explicit context: {method.Name}.");
			return (ActivityMethodType.Context, false);
		}

		var returnType = method.ReturnType;
		if (TypeLibrary.Activities.SystemDiagnostics.Activity.Equals(returnType))
		{
			logger?.Debug($"Inferring activity due to return type ({returnType.ToDisplayString()}): {method.Name}.");
			return (ActivityMethodType.Activity, true);
		}

		if (method.Name.EndsWith("Event", StringComparison.Ordinal))
		{
			logger?.Debug($"Inferring event as the method name ends in 'Event': {method.Name}.");
			return (ActivityMethodType.Event, true);
		}
		else
		{
			if (
				method.Parameters.Length > 0
				&& TypeLibrary.Activities.SystemDiagnostics.Activity.Equals(method.Parameters[0].Type)
			)
			{
				logger?.Debug($"Inferring event as the method's first parameter is an Activity: {method.Name}.");

				return (ActivityMethodType.Event, true);
			}
		}

		if (method.Name.EndsWith("Context", StringComparison.Ordinal))
		{
			logger?.Debug($"Inferring context as the method name ends in 'Context': {method.Name}.");
			return (ActivityMethodType.Context, true);
		}

		logger?.Debug($"Defaulting to activity: {method.Name}.");
		return (ActivityMethodType.Activity, true);
	}

	static string? GeneratePrefix(
		ActivitySourceGenerationAttributeRecord? activitySourceGenerationRecord,
		ActivitySourceAttributeRecord activitySourceRecord,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		string? prefix = null;
		var separator =
			activitySourceGenerationRecord?.BaggageAndTagSeparator.IsSet == true
				? activitySourceGenerationRecord.BaggageAndTagSeparator.Or(".")
				: ".";

		var activitySourceGenPrefix = activitySourceGenerationRecord?.BaggageAndTagPrefix.Value;
		var activitySourcePrefix = activitySourceRecord.BaggageAndTagPrefix.Value;
		var includeActivitySource = activitySourceRecord.IncludeActivitySourcePrefix.Value ?? true;

		if (!string.IsNullOrWhiteSpace(activitySourceGenPrefix))
			prefix = activitySourceGenPrefix + separator;

		if (!string.IsNullOrWhiteSpace(activitySourcePrefix))
		{
			prefix = includeActivitySource
				? prefix + activitySourcePrefix + separator
				: activitySourcePrefix + separator;
		}

		return prefix;
	}
}
