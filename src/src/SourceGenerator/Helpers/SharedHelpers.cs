using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

static partial class SharedHelpers
{
	public static GenerationType GetGenerationTypes(ISymbol symbol, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		var generationType = GenerationType.None;

		if (Utilities.ContainsAttribute(symbol, TemplateLibrary.Activities.ActivitySourceAttribute, token))
			generationType |= GenerationType.Activities;

		if (Utilities.ContainsAttribute(symbol, TemplateLibrary.Logging.LoggerAttribute, token))
			generationType |= GenerationType.Logging;

		if (Utilities.ContainsAttribute(symbol, TemplateLibrary.Metrics.MeterAttribute, token))
			generationType |= GenerationType.Metrics;

		return generationType;
	}

	/// <summary>
	/// Reads a value-type attribute argument (constructor parameter or named argument) using the framework's
	/// <see cref="AttributeDataExtensions"/> helpers. <paramref name="ctorName"/> is the constructor parameter
	/// name (camelCase); the named-argument lookup uses its PascalCase property name.
	/// </summary>
	static AttributeValue<T> GetAttributeValue<T>(AttributeData attributeData, string ctorName, T? defaultValue = null)
		where T : struct
	{
		if (
			attributeData.TryGetConstructorArgument<T>(ctorName, out var value)
			|| attributeData.TryGetNamedArgument<T>(Utilities.UppercaseFirstChar(ctorName), out value)
		)
		{
			return new(value);
		}

		return defaultValue is { } d ? new(d) : new();
	}

	/// <summary>
	/// Reads a string attribute argument (constructor parameter or named argument) using the framework's
	/// <see cref="AttributeDataExtensions"/> helpers, ignoring empty/whitespace values.
	/// </summary>
	static AttributeStringValue GetAttributeStringValue(
		AttributeData attributeData,
		string ctorName,
		string? defaultValue = null
	)
	{
		if (
			attributeData.TryGetConstructorArgument<string>(ctorName, out var value)
			|| attributeData.TryGetNamedArgument<string>(Utilities.UppercaseFirstChar(ctorName), out value)
		)
		{
			if (!string.IsNullOrWhiteSpace(value))
				return new(value);
		}

		return defaultValue is { } d ? new(d) : new();
	}

	public static bool ShouldEmit(GenerationType requestingType, GenerationType generationType)
	{
		// For multi-targeting: emit if the interface has the requesting target type
		// This allows ActivitySource+Logger+Meter interfaces to generate all three targets
		return generationType.HasFlag(requestingType);
	}

	/// <summary>
	/// Returns the single target that "owns" shared emission (DI extension, class attributes,
	/// constructor, throw stubs) for a multi-target interface. Only one target emits these to
	/// avoid duplicate definitions across the generated partial classes.
	/// </summary>
	/// <param name="generationType">The registered telemetry types for the interface.</param>
	/// <param name="includeActivities">
	/// Whether Activities participate in the priority. Activities never own the constructor,
	/// so constructor logic passes <see langword="false"/>.
	/// </param>
	public static GenerationType GetCanonicalTargetType(GenerationType generationType, bool includeActivities)
	{
		// Priority: Activities > Logging > Metrics
		if (includeActivities && generationType.HasFlag(GenerationType.Activities))
			return GenerationType.Activities;

		if (generationType.HasFlag(GenerationType.Logging))
			return GenerationType.Logging;

		if (generationType.HasFlag(GenerationType.Metrics))
			return GenerationType.Metrics;

		return GenerationType.None;
	}

	/// <summary>
	/// Determines if the DI extension should be generated for this requesting type.
	/// Only one target should generate the DI extension to avoid duplicate files.
	/// </summary>
	public static bool ShouldEmitDIExtension(GenerationType requestingType, GenerationType generationType) =>
		requestingType != GenerationType.None
		&& GetCanonicalTargetType(generationType, includeActivities: true) == requestingType;

	/// <summary>
	/// Determines if class-level attributes should be emitted for this requesting type.
	/// Only one target should emit class attributes to avoid duplicate attributes on partial classes.
	/// </summary>
	public static bool ShouldEmitClassAttributes(GenerationType requestingType, GenerationType generationType) =>
		requestingType != GenerationType.None
		&& GetCanonicalTargetType(generationType, includeActivities: true) == requestingType;

	/// <summary>
	/// Determines if the constructor should be emitted for this requesting type.
	/// Only one target should emit the constructor to avoid duplicate definitions.
	/// Constructor is emitted by the first target that needs one (Logging or Metrics).
	/// </summary>
	public static bool ShouldEmitConstructor(GenerationType requestingType, GenerationType generationType) =>
		requestingType != GenerationType.None
		&& GetCanonicalTargetType(generationType, includeActivities: false) == requestingType;

	public static TagOrBaggageAttributeRecord? GetTagOrBaggageAttribute(
		AttributeData attributeData,
		SemanticModel semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		return new(
			Name: GetAttributeStringValue(attributeData, "name"),
			SkipOnNullOrEmpty: GetAttributeValue<bool>(attributeData!, "skipOnNullOrEmpty", false)
		);
	}

	public static TelemetryGenerationAttributeRecord GetTelemetryGenerationAttribute(
		ISymbol type,
		SemanticModel semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		AttributeData? assemblyAttribute = null;
		if (
			!Utilities.TryContainsAttribute(
				type,
				TemplateLibrary.Shared.TelemetryGenerationAttribute,
				token,
				out var typeAttribute
			)
		)
		{
			if (
				!Utilities.TryContainsAttribute(
					semanticModel.Compilation.Assembly,
					TemplateLibrary.Shared.TelemetryGenerationAttribute,
					token,
					out assemblyAttribute
				)
			)
			{
				return CreateDefault();
			}
		}

		var assemblyTelemetryGeneration =
			assemblyAttribute == null
				? null
				: GetTelemetryGenerationAttribute(assemblyAttribute, semanticModel, logger, token);
		var typeGeneration =
			typeAttribute == null ? null : GetTelemetryGenerationAttribute(typeAttribute, semanticModel, logger, token);

		return assemblyAttribute == null && typeGeneration == null
			? CreateDefault()
			: new(
				GenerateDependencyExtension: typeGeneration?.GenerateDependencyExtension
					?? assemblyTelemetryGeneration?.GenerateDependencyExtension
					?? new(true),
				ClassName: typeGeneration?.ClassName ?? assemblyTelemetryGeneration?.ClassName ?? new(),
				DependencyInjectionClassName: typeGeneration?.DependencyInjectionClassName
					?? assemblyTelemetryGeneration?.DependencyInjectionClassName
					?? new(),
				DependencyInjectionClassIsPublic: typeGeneration?.DependencyInjectionClassIsPublic
					?? assemblyTelemetryGeneration?.DependencyInjectionClassIsPublic
					?? new(false),
				NamingConvention: typeGeneration?.NamingConvention
					?? assemblyTelemetryGeneration?.NamingConvention
					?? new(1), // Default to OpenTelemetry
				GenerateTelemetryNamesClass: typeGeneration?.GenerateTelemetryNamesClass
					?? assemblyTelemetryGeneration?.GenerateTelemetryNamesClass
					?? new(true),
				TelemetryNamesClassName: typeGeneration?.TelemetryNamesClassName
					?? assemblyTelemetryGeneration?.TelemetryNamesClassName
					?? new()
			);

		static TelemetryGenerationAttributeRecord CreateDefault() =>
			new(
				GenerateDependencyExtension: new(true),
				ClassName: new(),
				DependencyInjectionClassName: new(),
				DependencyInjectionClassIsPublic: new(false),
				NamingConvention: new(1), // Default to OpenTelemetry
				GenerateTelemetryNamesClass: new(true),
				TelemetryNamesClassName: new()
			);
	}

	static TelemetryGenerationAttributeRecord? GetTelemetryGenerationAttribute(
		AttributeData attributeData,
		SemanticModel semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		return new(
			GenerateDependencyExtension: GetAttributeValue<bool>(attributeData!, "generateDependencyExtension", true),
			ClassName: GetAttributeStringValue(attributeData, "className"),
			DependencyInjectionClassName: GetAttributeStringValue(attributeData, "dependencyInjectionClassName"),
			DependencyInjectionClassIsPublic: GetAttributeValue<bool>(
				attributeData!,
				"dependencyInjectionClassIsPublic",
				false
			),
			NamingConvention: GetAttributeValue<int>(attributeData!, "namingConvention", 1), // Default to OpenTelemetry
			GenerateTelemetryNamesClass: GetAttributeValue<bool>(attributeData!, "generateTelemetryNamesClass", true),
			TelemetryNamesClassName: GetAttributeStringValue(attributeData, "telemetryNamesClassName")
		);
	}

	/// <summary>
	/// Gets the ExcludeTargetsAttribute from a parameter, if present.
	/// </summary>
	public static ExcludeTargetsAttributeRecord? GetExcludeTargetsAttribute(
		IParameterSymbol parameter,
		SemanticModel? semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				parameter,
				TemplateLibrary.Shared.ExcludeTargetsAttribute,
				token,
				out var attributeData
			)
		)
		{
			return null;
		}

		var excludedTargets = GetAttributeValue<int>(attributeData!, "targets", 0).Value ?? 0;

		return new ExcludeTargetsAttributeRecord((GenerationType)excludedTargets);
	}

	/// <summary>
	/// Checks if a parameter should be excluded from a specific target based on ExcludeTargetsAttribute.
	/// </summary>
	public static bool IsParameterExcludedFromTarget(
		IParameterSymbol parameter,
		GenerationType target,
		SemanticModel semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		var excludeTargets = GetExcludeTargetsAttribute(parameter, semanticModel, logger, token);
		return excludeTargets?.ExcludedTargets.HasFlag(target) == true;
	}
}
