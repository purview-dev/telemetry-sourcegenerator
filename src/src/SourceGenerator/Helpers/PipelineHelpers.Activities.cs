using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class PipelineHelpers
{
	public static bool HasActivityTargetAttribute(SyntaxNode _, CancellationToken __) => true;

	public static GeneratorResult<ActivitySourceTarget?> BuildActivityTransform(
		GeneratorAttributeSyntaxContext context,
		CancellationToken token
	) => BuildActivityTarget(context.TargetSymbol as INamedTypeSymbol, context.SemanticModel.Compilation, token);

	public static GeneratorResult<ActivitySourceTarget?> BuildActivityTarget(
		INamedTypeSymbol? interfaceSymbol,
		Compilation compilation,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		if (interfaceSymbol is null)
			return GeneratorResult<ActivitySourceTarget?>.Empty;

		if (interfaceSymbol.Arity > 0)
		{
			return GeneratorResult<ActivitySourceTarget?>.Create(
				DiagnosticInfo.Create(
					DiagnosticLibrary.General.GenericInterfacesNotSupported.Descriptor,
					interfaceSymbol
				)
			);
		}

		var activitySourceData = SharedHelpers.GetActivitySourceAttribute(interfaceSymbol, token);
		if (activitySourceData is not { } activitySourceAttribute)
			return GeneratorResult<ActivitySourceTarget?>.Empty;

		var telemetryGeneration = SharedHelpers.GetTelemetryGenerationAttribute(interfaceSymbol, compilation, token);
		var className = telemetryGeneration.ClassName ?? GenerateClassName(interfaceSymbol.Name);

		var activitySourceGenerationAttribute = SharedHelpers.GetActivitySourceGenerationAttribute(compilation, token);
		var activitySourceName = activitySourceGenerationAttribute?.Name ?? activitySourceAttribute.Name;

		// Get naming convention from TelemetryGenerationAttribute (default to OpenTelemetry = 1)
		var namingConvention = telemetryGeneration.NamingConvention;
		var isLegacy = namingConvention == 0;

		if (activitySourceName == null)
		{
			var assemblyName = compilation.AssemblyName;
			if (!string.IsNullOrWhiteSpace(assemblyName))
			{
				// Legacy mode: lowercase the assembly name
				// OpenTelemetry mode: preserve casing
#pragma warning disable CA1308 // Intentional lowercase for legacy compatibility
				activitySourceName = isLegacy ? assemblyName!.ToLowerInvariant() : assemblyName;
#pragma warning restore CA1308
			}
		}

		if (
			interfaceSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(token)
			is not InterfaceDeclarationSyntax interfaceDeclaration
		)
			return GeneratorResult<ActivitySourceTarget?>.Empty;

		var generationType = SharedHelpers.GetGenerationTypes(interfaceSymbol, token);
		var activityMethods = BuildActivityMethods(
			generationType,
			activitySourceAttribute,
			activitySourceGenerationAttribute,
			telemetryGeneration,
			interfaceSymbol,
			token
		);

		return GeneratorResult<ActivitySourceTarget?>.Create(
			new(
				TelemetryGeneration: telemetryGeneration,
				GenerationType: generationType,
				ClassNameToGenerate: className,
				ParentClasses: Utilities.GetParentClasses(interfaceDeclaration),
				InterfaceType: TypeReference.Create(interfaceSymbol),
				ActivitySourceGenerationAttribute: activitySourceGenerationAttribute,
				ActivitySourceName: activitySourceName,
				ActivityMethods: activityMethods,
				ActivityTargetAttributeRecord: activitySourceAttribute
			),
			TelemetryRules.GetInterfaceLevelDiagnostics(interfaceSymbol, compilation, token)
		);
	}

	static ImmutableArray<ActivityBasedGenerationTarget> BuildActivityMethods(
		GenerationType generationType,
		ActivitySourceAttributeData activitySourceAttribute,
		ActivitySourceGenerationAttributeData? activitySourceGenerationAttribute,
		TelemetryGenerationAttributeData telemetryGeneration,
		INamedTypeSymbol interfaceSymbol,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		// Get naming convention from TelemetryGenerationAttribute (default to OpenTelemetry = 1)
		var namingConvention = telemetryGeneration.NamingConvention;

		var prefix = GeneratePrefix(activitySourceGenerationAttribute, activitySourceAttribute, token);
		var defaultToTags = activitySourceGenerationAttribute?.DefaultToTags ?? activitySourceAttribute.DefaultToTags;

		var lowercaseBaggageAndTagKeys = activitySourceAttribute.LowercaseBaggageAndTagKeys;

		List<ActivityBasedGenerationTarget> methodTargets = [];
		foreach (var method in GetAllInterfaceMethods(interfaceSymbol, token))
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

			if (TypeHelpers.HasAttribute(method, TypeLibrary.TelemetryShared.ExcludeAttribute))
				continue;

			var (methodType, isInferred) = GetMethodType(
				method,
				token,
				out var activityAttribute,
				out var eventAttribute
			);
			var activityOrEventName = activityAttribute?.Name ?? eventAttribute?.Name;

			if (string.IsNullOrWhiteSpace(activityOrEventName))
				activityOrEventName = method.Name;

			var parameters = GetActivityParameters(
				method,
				prefix,
				defaultToTags,
				lowercaseBaggageAndTagKeys,
				namingConvention,
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
		CancellationToken token
	)
	{
		List<ActivityBasedParameterTarget> parameterTargets = [];
		foreach (var parameter in method.Parameters)
		{
			token.ThrowIfCancellationRequested();

			var parameterType = TypeReference.Create(parameter.Type);
			var (destination, attribute) = GetParameterDestination(parameter, parameterType, defaultToTags, token);

			TagOrBaggageAttributeRecord? tagOrBaggageAttribute = null;
			if (attribute != null)
				tagOrBaggageAttribute = SharedHelpers.GetTagOrBaggageAttribute(attribute, token);

			// Check for ExcludeTargetsAttribute
			var excludeTargets = SharedHelpers.GetExcludeTargetsAttribute(parameter, token);

			var parameterName = parameter.Name;
			var generatedName = GenerateParameterName(
				tagOrBaggageAttribute?.Name ?? parameterName,
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
		CancellationToken token,
		out ActivityAttributeData? activityAttribute,
		out EventAttributeData? eventAttribute
	)
	{
		eventAttribute = null;

		token.ThrowIfCancellationRequested();

		activityAttribute = SharedHelpers.GetActivityGenAttribute(method, token);
		if (activityAttribute != null)
			return (ActivityMethodType.Activity, false);

		eventAttribute = SharedHelpers.GetActivityEventAttribute(method, token);
		if (eventAttribute != null)
			return (ActivityMethodType.Event, false);

		if (Utilities.ContainsAttribute(method, TypeLibrary.Activities.ContextAttribute, token))
			return (ActivityMethodType.Context, false);

		var returnType = method.ReturnType;
		if (TypeLibrary.Activities.SystemDiagnostics.Activity.Equals(returnType))
			return (ActivityMethodType.Activity, true);

		if (method.Name.EndsWith("Event", StringComparison.Ordinal))
			return (ActivityMethodType.Event, true);
		else
		{
			if (
				method.Parameters.Length > 0
				&& TypeLibrary.Activities.SystemDiagnostics.Activity.Equals(method.Parameters[0].Type)
			)
				return (ActivityMethodType.Event, true);
		}

		if (method.Name.EndsWith("Context", StringComparison.Ordinal))
			return (ActivityMethodType.Context, true);

		return (ActivityMethodType.Activity, true);
	}

	static (ActivityParameterDestination Destination, AttributeData? Attribute) GetParameterDestination(
		IParameterSymbol parameter,
		TypeReference parameterType,
		bool defaultToTags,
		CancellationToken token
	)
	{
		var destination = defaultToTags ? ActivityParameterDestination.Tag : ActivityParameterDestination.Baggage;

		if (
			Utilities.TryContainsAttribute(
				parameter,
				TypeLibrary.TelemetryShared.TagAttribute,
				token,
				out var attribute
			)
		)
			return (ActivityParameterDestination.Tag, attribute);

		if (Utilities.TryContainsAttribute(parameter, TypeLibrary.Activities.BaggageAttribute, token, out attribute))
			return (ActivityParameterDestination.Baggage, attribute);

		if (Utilities.ContainsAttribute(parameter, TypeLibrary.Activities.EscapeAttribute, token))
			return (ActivityParameterDestination.Escape, null);

		if (Utilities.ContainsAttribute(parameter, TypeLibrary.Activities.StatusDescriptionAttribute, token))
			return (ActivityParameterDestination.StatusDescription, null);

		if (parameterType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.Activity))
			return (ActivityParameterDestination.Activity, null);

		if (
			parameterType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.ActivityTagsCollection)
			|| TypeLibrary.Activities.SystemDiagnostics.ActivityTagIEnumerable.Similar(parameterType)
			|| parameterType.Identity.Equals(TypeLibrary.System.TagList)
		)
			return (ActivityParameterDestination.TagsEnumerable, null);

		if (
			parameterType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.ActivityContext)
			|| (
				parameter.Name == PropertyLibrary.Activities.ParentIdParameterName
				&& parameterType.Identity.SpecialType == SpecialType.System_String
			)
		)
			return (ActivityParameterDestination.ParentContextOrId, null);

		if (
			parameterType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.ActivityLinkArray)
			|| TypeLibrary.Activities.SystemDiagnostics.ActivityLinkIEnumerable.Equals(parameterType)
		)
			return (ActivityParameterDestination.LinksEnumerable, null);

		if (
			parameter.Name == PropertyLibrary.Activities.StartTimeParameterName
			&& parameterType.Identity.Equals(TypeLibrary.System.DateTimeOffset)
		)
			return (ActivityParameterDestination.StartTime, null);

		if (
			parameter.Name == PropertyLibrary.Activities.TimeStampParameterName
			&& parameterType.Identity.Equals(TypeLibrary.System.DateTimeOffset)
		)
			return (ActivityParameterDestination.Timestamp, null);

		// Infer tag/baggage based on the interface default.
		return (destination, null);
	}

	static string? GeneratePrefix(
		ActivitySourceGenerationAttributeData? activitySourceGenerationRecord,
		ActivitySourceAttributeData activitySourceRecord,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		string? prefix = null;
		var separator = activitySourceGenerationRecord?.BaggageAndTagSeparator ?? ".";

		var activitySourceGenPrefix = activitySourceGenerationRecord?.BaggageAndTagPrefix;
		var activitySourcePrefix = activitySourceRecord.BaggageAndTagPrefix;
		var includeActivitySource = activitySourceRecord.IncludeActivitySourcePrefix;

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
