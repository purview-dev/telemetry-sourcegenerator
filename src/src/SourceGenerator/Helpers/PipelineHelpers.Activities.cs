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

		if (interfaceSymbol.Arity > 0 && !SharedHelpers.AreInterfaceTypeParametersSupported(interfaceSymbol))
		{
			return GeneratorResult<ActivitySourceTarget?>.Create(
				ReportableDiagnostic.Create(
					DiagnosticLibrary.General.GenericInterfacesNotSupported.Descriptor,
					isBlocking: true,
					interfaceSymbol,
					interfaceSymbol.Name
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
				InterfaceType: SharedHelpers.GetInterfaceTypeReference(interfaceSymbol),
				ActivitySourceGenerationAttribute: activitySourceGenerationAttribute,
				ActivitySourceName: activitySourceName,
				ActivityMethods: activityMethods,
				ActivityTargetAttributeRecord: activitySourceAttribute,
				InterfaceTypeParameters: TypeParameterHelpers.FromNamedType(interfaceSymbol)
			),
			TelemetryRules.GetInterfaceLevelDiagnostics(interfaceSymbol, compilation, token)
		);
	}

	static ImmutableArray<ActivityMethodTarget> BuildActivityMethods(
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

		// The assembly-level value is the default and the interface-level value overrides it, which is the same
		// precedence DefaultToTags uses. Before this, the assembly-level property was declared and modelled but
		// never read, so setting it changed nothing.
		var lowercaseBaggageAndTagKeys = activitySourceGenerationAttribute?.LowercaseBaggageAndTagKeys
			?? activitySourceAttribute.LowercaseBaggageAndTagKeys;

		List<ActivityMethodTarget> methodTargets = [];
		foreach (var method in GetAllInterfaceMethods(interfaceSymbol, token))
		{
			token.ThrowIfCancellationRequested();

			if (TypeHelpers.HasAttribute(method, TypeLibrary.Purview.Telemetry.ExcludeAttribute))
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
			// Parameters can opt out of the Activities target to be used by the other target families
			// only: they remain part of the generated method signature but are not applied to the
			// Activity or ActivityEvent.
			var baggageParameters = parameters
				.Where(m => m.ParamDestination == ActivityParameterDestination.Baggage && !m.ExcludedFromActivities)
				.ToImmutableArray();
			var tagParameters = parameters
				.Where(m => m.ParamDestination == ActivityParameterDestination.Tag && !m.ExcludedFromActivities)
				.ToImmutableArray();

			var targetGenerationState = Utilities.IsValidGenerationTarget(
				method,
				generationType,
				GenerationType.Activities
			);

			var returnType = TypeReference.Create(method.ReturnType);
			var hasActivityParameter = parameters.Any(m =>
				m.ParameterType.Identity.Equals(TypeLibrary.System.Diagnostics.Activity)
			);

			// The payload decides the case, so an activity attribute and an event attribute can never both be set,
			// and a method that has neither is a context method rather than an activity with a null attribute.
			var typeParameters = TypeParameterHelpers.FromMethod(method);

			// TSG1005: a type parameter whose constraint the generator cannot re-declare stops this method; the
			// emitter writes a throw stub so the interface stays implemented.
			if (!TypeParameterHelpers.AreSupported(method.TypeParameters))
				targetGenerationState = targetGenerationState with { IsValid = false };

			ActivityMethodTarget methodTarget =
				methodType == ActivityMethodType.Activity
					? new StartActivityMethod(
						MethodName: method.Name,
						ReturnType: returnType,
						ActivityOrEventName: activityOrEventName!,
						HasActivityParameter: hasActivityParameter,
						Parameters: parameters,
						Baggage: baggageParameters,
						Tags: tagParameters,
						TargetGenerationState: targetGenerationState,
						ActivityAttribute: activityAttribute,
						TypeParameters: typeParameters
					)
				: methodType == ActivityMethodType.Event
					? new ActivityEventMethod(
						MethodName: method.Name,
						ReturnType: returnType,
						ActivityOrEventName: activityOrEventName!,
						HasActivityParameter: hasActivityParameter,
						Parameters: parameters,
						Baggage: baggageParameters,
						Tags: tagParameters,
						TargetGenerationState: targetGenerationState,
						EventAttribute: eventAttribute,
						TypeParameters: typeParameters
					)
				: new ActivityContextMethod(
					MethodName: method.Name,
					ReturnType: returnType,
					ActivityOrEventName: activityOrEventName!,
					HasActivityParameter: hasActivityParameter,
					Parameters: parameters,
					Baggage: baggageParameters,
					Tags: tagParameters,
					TargetGenerationState: targetGenerationState,
					TypeParameters: typeParameters
				);

			methodTargets.Add(methodTarget);
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

			var parameterType = SharedHelpers.ToTypeReference(parameter.Type);
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
					ExcludedTargets: excludeTargets?.ExcludedTargets ?? GenerationType.None,
					Union: UnionHelpers.GetUnionTarget(parameter.Type, token)
				)
			);
		}

		return [.. parameterTargets];
	}

	/// <summary>
	/// How a method participates in an activity interface. This is a construction-time decision: the closed
	/// <see cref="ActivityMethodTarget"/> hierarchy carries it in the type, so nothing downstream switches on it.
	/// </summary>
	enum ActivityMethodType
	{
		Activity,
		Event,
		Context,
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

		if (Utilities.ContainsAttribute(method, TypeLibrary.Purview.Telemetry.ContextAttribute, token))
			return (ActivityMethodType.Context, false);

		var returnType = method.ReturnType;
		if (TypeLibrary.System.Diagnostics.Activity.Equals(returnType))
			return (ActivityMethodType.Activity, true);

		if (method.Name.EndsWith("Event", StringComparison.Ordinal))
			return (ActivityMethodType.Event, true);
		else
		{
			if (
				method.Parameters.Length > 0
				&& TypeLibrary.System.Diagnostics.Activity.Equals(method.Parameters[0].Type)
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
				TypeLibrary.Purview.Telemetry.TagAttribute,
				token,
				out var attribute
			)
		)
			return (ActivityParameterDestination.Tag, attribute);

		if (
			Utilities.TryContainsAttribute(
				parameter,
				TypeLibrary.Purview.Telemetry.BaggageAttribute,
				token,
				out attribute
			)
		)
			return (ActivityParameterDestination.Baggage, attribute);

		if (Utilities.ContainsAttribute(parameter, TypeLibrary.Purview.Telemetry.EscapeAttribute, token))
			return (ActivityParameterDestination.Escape, null);

		if (Utilities.ContainsAttribute(parameter, TypeLibrary.Purview.Telemetry.StatusDescriptionAttribute, token))
			return (ActivityParameterDestination.StatusDescription, null);

		if (parameterType.Identity.Equals(TypeLibrary.System.Diagnostics.Activity))
			return (ActivityParameterDestination.Activity, null);

		if (
			parameterType.Identity.Equals(TypeLibrary.System.Diagnostics.ActivityTagsCollection)
			|| TypeLibrary.System.Diagnostics.ActivityTagIEnumerable.Similar(parameterType)
			|| parameterType.Identity.Equals(TypeLibrary.System.Diagnostics.TagList)
		)
			return (ActivityParameterDestination.TagsEnumerable, null);

		if (
			parameterType.Identity.Equals(TypeLibrary.System.Diagnostics.ActivityContext)
			|| (
				parameter.Name == PropertyLibrary.Activities.ParentIdParameterName
				&& parameterType.Identity.SpecialType == SpecialType.System_String
			)
		)
			return (ActivityParameterDestination.ParentContextOrId, null);

		if (
			parameterType.Identity.Equals(TypeLibrary.System.Diagnostics.ActivityLinkArray)
			|| TypeLibrary.System.Diagnostics.ActivityLinkIEnumerable.Equals(parameterType)
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
