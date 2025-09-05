using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class PipelineHelpers
{
	public static bool HasActivityTargetAttribute(SyntaxNode _, CancellationToken __) => true;

	public static ActivitySourceTarget? BuildActivityTransform(
		GeneratorAttributeSyntaxContext context,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		if (context.TargetNode is not InterfaceDeclarationSyntax interfaceDeclaration)
		{
			logger?.Error(
				$"Could not find interface syntax from the target node '{context.TargetNode.Flatten()}'."
			);
			return null;
		}

		if (context.TargetSymbol is not INamedTypeSymbol interfaceSymbol)
		{
			logger?.Error($"Could not find interface symbol '{interfaceDeclaration.Flatten()}'.");
			return null;
		}

		if (interfaceSymbol.Arity > 0)
		{
			logger?.Diagnostic(
				$"Cannot generate a Activity target for a generic interface '{interfaceDeclaration.Flatten()}'."
			);
			return ActivitySourceTarget.Failed(
				TelemetryDiagnostics.General.GenericInterfacesNotSupported,
				interfaceSymbol.Locations
			);
		}

		// If multi-target generation is enabled and this interface contains [Telemetry] methods,
		// skip single-target activity generation to avoid mixed outputs.
		var assembly = context.SemanticModel.Compilation.Assembly;
		foreach (
			var m in GetAllInterfaceMethods(
				interfaceSymbol,
				context.SemanticModel.Compilation,
				token
			)
		)
		{
			var mt = Utilities.GetMultiTargetConfiguration(m, assembly);
			if (mt.IsMultiTargetEnabled)
			{
				logger?.Debug(
					$"Skipping Activity generation for {interfaceSymbol.Name} due to multi-target usage."
				);
				return null;
			}
		}

		var semanticModel = context.SemanticModel;
		var activitySourceAttribute = SharedHelpers.GetActivitySourceAttribute(
			context.TargetSymbol,
			semanticModel,
			logger,
			token
		);
		if (activitySourceAttribute == null)
		{
			logger?.Error(
				$"Could not find {Constants.Activities.ActivitySourceAttribute} when one was expected '{interfaceDeclaration.Flatten()}'."
			);
			return null;
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
			activitySourceGenerationAttribute?.Name.IsSet == true
				? activitySourceGenerationAttribute.Name.Value!
			: activitySourceAttribute.Name.IsSet ? activitySourceAttribute.Name.Value!
			: null;

		if (activitySourceName == null)
		{
#pragma warning disable CA1308 // Normalize strings to uppercase
			var assemblyName = context.SemanticModel.Compilation.AssemblyName?.ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
			if (!string.IsNullOrWhiteSpace(assemblyName))
			{
				activitySourceName = assemblyName;
			}
		}

		var fullNamespace = Utilities.GetFullNamespace(interfaceDeclaration, true);
		var generationType = SharedHelpers.GetGenerationTypes(interfaceSymbol, token);
		var activityMethods = BuildActivityMethods(
			generationType,
			activitySourceAttribute,
			activitySourceGenerationAttribute,
			semanticModel,
			interfaceSymbol,
			logger,
			token,
			out var methodDiagnostics
		);

		return new(
			TelemetryGeneration: telemetryGeneration,
			GenerationType: generationType,
			ClassNameToGenerate: className,
			ClassNamespace: Utilities.GetNamespace(interfaceDeclaration),
			ParentClasses: Utilities.GetParentClasses(interfaceDeclaration),
			FullNamespace: fullNamespace,
			FullyQualifiedName: fullNamespace + className,
			InterfaceType: PurviewTypeFactory.Create(interfaceSymbol),
			ActivitySourceGenerationAttribute: activitySourceGenerationAttribute,
			ActivitySourceName: activitySourceName,
			ActivityMethods: activityMethods,
			ActivityTargetAttributeRecord: activitySourceAttribute,
			InterfaceLocation: interfaceDeclaration.GetLocation(),
			DuplicateMethods: BuildDuplicateMethods(interfaceSymbol, semanticModel, token),
			Failures: methodDiagnostics?.ToImmutableArray()
		);
	}

	static ImmutableArray<ActivityBasedGenerationTarget> BuildActivityMethods(
		GenerationType generationType,
		ActivitySourceAttributeRecord activitySourceAttribute,
		ActivitySourceGenerationAttributeRecord? activitySourceGenerationAttribute,
		SemanticModel semanticModel,
		INamedTypeSymbol interfaceSymbol,
		GenerationLogger? logger,
		CancellationToken token,
		out (TelemetryDiagnosticDescriptor, ImmutableArray<Location>)[]? methodDiagnostics
	)
	{
		token.ThrowIfCancellationRequested();

		var prefix = GeneratePrefix(
			activitySourceGenerationAttribute,
			activitySourceAttribute,
			token
		);
		var defaultToTags =
			activitySourceGenerationAttribute?.DefaultToTags.IsSet == true
				? activitySourceGenerationAttribute.DefaultToTags.Value!.Value
				: activitySourceAttribute.DefaultToTags.Value!.Value;
		var lowercaseBaggageAndTagKeys = activitySourceAttribute
			.LowercaseBaggageAndTagKeys!
			.Value!
			.Value;

		List<(TelemetryDiagnosticDescriptor, ImmutableArray<Location>)>? methodDiagnosticsList =
			null;
		List<ActivityBasedGenerationTarget> methodTargets = [];
		foreach (
			var method in GetAllInterfaceMethods(interfaceSymbol, semanticModel.Compilation, token)
		)
		{
			token.ThrowIfCancellationRequested();

			if (method.Arity > 0)
			{
				methodDiagnosticsList ??= [];
				methodDiagnosticsList.Add(
					(TelemetryDiagnostics.General.GenericMethodsNotSupported, method.Locations)
				);
				continue;
			}

			if (Utilities.ContainsAttribute(method, Constants.Shared.ExcludeAttribute, token))
			{
				logger?.Debug(
					$"Skipping {interfaceSymbol.Name}.{method.Name}, explicitly excluded."
				);
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
				activityAttribute?.Name.IsSet == true
					? activityAttribute.Value.Name.Value
					: eventAttribute?.Name.Value;

			if (string.IsNullOrWhiteSpace(activityOrEventName))
			{
				activityOrEventName = method.Name;
			}

			logger?.Debug($"Found {methodType} method {interfaceSymbol.Name}.{method.Name}.");

			var parameters = GetActivityParameters(
				method,
				prefix,
				defaultToTags,
				lowercaseBaggageAndTagKeys,
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

			methodTargets.Add(
				new(
					MethodName: method.Name,
					ReturnType: PurviewTypeFactory.Create(method.ReturnType),
					ActivityOrEventName: activityOrEventName!,
					HasActivityParameter: parameters.Any(m =>
						Constants.Activities.SystemDiagnostics.Activity.Equals(m.ParameterType)
					),
					Locations: method.Locations,
					ActivityAttribute: activityAttribute,
					EventAttribute: eventAttribute,
					MethodType: methodType,
					Parameters: parameters,
					Baggage: baggageParameters,
					Tags: tagParameters,
					TargetGenerationState: Utilities.IsValidGenerationTarget(
						method,
						generationType,
						GenerationType.Activities
					)
				)
			);
		}

		methodDiagnostics = methodDiagnosticsList?.ToArray();

		return [.. methodTargets];
	}

	static ImmutableArray<ActivityBasedParameterTarget> GetActivityParameters(
		IMethodSymbol method,
		string? prefix,
		bool defaultToTags,
		bool lowercaseBaggageAndTagKeys,
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		List<ActivityBasedParameterTarget> parameterTargets = [];
		foreach (var parameter in method.Parameters)
		{
			token.ThrowIfCancellationRequested();

			var parameterType = PurviewTypeFactory.Create(parameter.Type);
			var destination = defaultToTags
				? ActivityParameterDestination.Tag
				: ActivityParameterDestination.Baggage;
			if (
				Utilities.TryContainsAttribute(
					parameter,
					Constants.Shared.TagAttribute,
					token,
					out var attribute
				)
			)
			{
				logger?.Debug($"Found explicit tag: {parameter.Name}.");
				destination = ActivityParameterDestination.Tag;
			}
			else if (
				Utilities.TryContainsAttribute(
					parameter,
					Constants.Activities.BaggageAttribute,
					token,
					out attribute
				)
			)
			{
				logger?.Debug($"Found explicit baggage: {parameter.Name}.");
				destination = ActivityParameterDestination.Baggage;
			}
			else if (
				Utilities.ContainsAttribute(parameter, Constants.Activities.EscapeAttribute, token)
			)
			{
				logger?.Debug($"Found escape parameter: {parameter.Name}.");
				destination = ActivityParameterDestination.Escape;
			}
			else if (
				Utilities.ContainsAttribute(
					parameter,
					Constants.Activities.StatusDescriptionAttribute,
					token
				)
			)
			{
				logger?.Debug($"Found status description parameter: {parameter.Name}.");
				destination = ActivityParameterDestination.StatusDescription;
			}
			else if (Constants.Activities.SystemDiagnostics.Activity.Equals(parameterType))
			{
				destination = ActivityParameterDestination.Activity;
			}
			else if (
				Constants.Activities.SystemDiagnostics.ActivityTagsCollection.Equals(parameterType)
				|| Constants.Activities.SystemDiagnostics.ActivityTagIEnumerable.Equals(
					parameterType
				)
				|| Constants.System.TagList.Equals(parameterType)
			)
			{
				destination = ActivityParameterDestination.TagsEnumerable;
			}
			else if (
				Constants.Activities.SystemDiagnostics.ActivityContext.Equals(parameterType)
				|| (
					parameter.Name == Constants.Activities.ParentIdParameterName
					&& parameterType.SpecialType == SpecialType.System_String
				)
			)
			{
				destination = ActivityParameterDestination.ParentContextOrId;
			}
			else if (
				Constants.Activities.SystemDiagnostics.ActivityLinkArray.Equals(parameterType)
				|| Constants.Activities.SystemDiagnostics.ActivityLinkIEnumerable.Equals(
					parameterType
				)
			)
			{
				destination = ActivityParameterDestination.LinksEnumerable;
			}
			else if (
				parameter.Name == Constants.Activities.StartTimeParameterName
				&& Constants.System.DateTimeOffset.Equals(parameterType)
			)
			{
				destination = ActivityParameterDestination.StartTime;
			}
			else if (
				parameter.Name == Constants.Activities.TimeStampParameterName
				&& Constants.System.DateTimeOffset.Equals(parameterType)
			)
			{
				destination = ActivityParameterDestination.Timestamp;
			}
			else
			{
				// destination is already set to default.
				logger?.Debug(
					$"Inferring {(defaultToTags ? "tag" : "baggage")}: {parameter.Name}."
				);
			}

			TagOrBaggageAttributeRecord? tagOrBaggageAttribute = null;
			if (attribute != null)
			{
				tagOrBaggageAttribute = SharedHelpers.GetTagOrBaggageAttribute(
					attribute,
					semanticModel,
					logger,
					token
				);
			}

			var parameterName = parameter.Name;
			var generatedName = GenerateParameterName(
				tagOrBaggageAttribute?.Name.Value ?? parameterName,
				prefix,
				lowercaseBaggageAndTagKeys
			);

			parameterTargets.Add(
				new(
					ParameterName: parameterName,
					ParameterType: parameterType,
					GeneratedName: generatedName,
					ParamDestination: destination,
					SkipOnNullOrEmpty: GetSkipOnNullOrEmptyValue(tagOrBaggageAttribute),
					IsException: parameter.Type.IsExceptionType(),
					Locations: parameter.Locations
				)
			);
		}

		return [.. parameterTargets];
	}

	static (ActivityMethodType, bool) GetMethodType(
		IMethodSymbol method,
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken token,
		out ActivityAttributeRecord? activityAttribute,
		out EventAttributeRecord? eventAttribute
	)
	{
		eventAttribute = null;

		token.ThrowIfCancellationRequested();

		activityAttribute = SharedHelpers.GetActivityGenAttribute(
			method,
			semanticModel,
			logger,
			token
		);
		if (activityAttribute != null)
		{
			logger?.Debug($"Found explicit activity: {method.Name}.");
			return (ActivityMethodType.Activity, false);
		}

		eventAttribute = SharedHelpers.GetActivityEventAttribute(
			method,
			semanticModel,
			logger,
			token
		);
		if (eventAttribute != null)
		{
			logger?.Debug($"Found explicit event: {method.Name}.");
			return (ActivityMethodType.Event, false);
		}

		if (Utilities.ContainsAttribute(method, Constants.Activities.ContextAttribute, token))
		{
			logger?.Debug($"Found explicit context: {method.Name}.");
			return (ActivityMethodType.Context, false);
		}

		var returnType = method.ReturnType;
		if (Constants.Activities.SystemDiagnostics.Activity.Equals(returnType))
		{
			logger?.Debug(
				$"Inferring activity due to return type ({returnType.ToDisplayString()}): {method.Name}."
			);
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
				&& Constants.Activities.SystemDiagnostics.Activity.Equals(method.Parameters[0].Type)
			)
			{
				logger?.Debug(
					$"Inferring event as the method's first parameter is an Activity: {method.Name}."
				);

				return (ActivityMethodType.Event, true);
			}
		}

		if (method.Name.EndsWith("Context", StringComparison.Ordinal))
		{
			logger?.Debug(
				$"Inferring context as the method name ends in 'Context': {method.Name}."
			);
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
		{
			prefix = activitySourceGenPrefix + separator;
		}

		if (!string.IsNullOrWhiteSpace(activitySourcePrefix))
		{
			prefix = includeActivitySource
				? prefix + activitySourcePrefix + separator
				: activitySourcePrefix + separator;
		}

		return prefix;
	}
}
