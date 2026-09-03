using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

static partial class SharedHelpers
{
	public static GenerationType GetGenerationTypes(ISymbol symbol, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		var generationType = GenerationType.None;

		if (Utilities.ContainsAttribute(symbol, TypeLibrary.Activities.ActivitySourceAttribute, token))
			generationType |= GenerationType.Activities;

		if (Utilities.ContainsAttribute(symbol, TypeLibrary.Logging.LoggerAttribute, token))
			generationType |= GenerationType.Logging;

		if (Utilities.ContainsAttribute(symbol, TypeLibrary.Metrics.MeterAttribute, token))
			generationType |= GenerationType.Metrics;

		return generationType;
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

		// If no targets are registered, return None. This should not happen in practice, but we handle it gracefully.
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

	/// <summary>
	/// Matches the previous whitespace-skipping string-argument behaviour:
	/// a string argument that is null, empty or whitespace is treated as not-specified.
	/// </summary>
	public static string? NullIfWhitespace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

	public static TagOrBaggageAttributeRecord? GetTagOrBaggageAttribute(
		AttributeData attributeData,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		var tag = TagAttributeData.FromAttributeData(attributeData);
		if (tag.Exists)
			return new(Name: NullIfWhitespace(tag.Name), SkipOnNullOrEmpty: tag.SkipOnNullOrEmpty);

		var baggage = BaggageAttributeData.FromAttributeData(attributeData);
		if (baggage.Exists)
			return new(Name: NullIfWhitespace(baggage.Name), SkipOnNullOrEmpty: baggage.SkipOnNullOrEmpty);

		// If neither Tag nor Baggage attribute is found, return null
		return null;
	}

	static readonly TelemetryGenerationAttributeData DefaultTelemetryGeneration = new(
		exists: true,
		GenerateDependencyExtension: true,
		ClassName: null,
		DependencyInjectionClassName: null,
		DependencyInjectionClassIsPublic: false,
		NamingConvention: 1, // Default to OpenTelemetry
		GenerateTelemetryNamesClass: true,
		TelemetryNamesClassName: null,
		TelemetryNamesNamespace: null
	);

	public static TelemetryGenerationAttributeData GetTelemetryGenerationAttribute(
		ISymbol type,
		Compilation compilation,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		var typeData = TelemetryGenerationAttributeData.FromAttributeData(type);
		if (typeData.Exists)
			return Normalize(typeData);

		var assemblyData = TelemetryGenerationAttributeData.FromAttributeData(compilation.Assembly);
		return assemblyData.Exists ? Normalize(assemblyData) : DefaultTelemetryGeneration;
	}

	static TelemetryGenerationAttributeData Normalize(TelemetryGenerationAttributeData data) =>
		data with
		{
			ClassName = NullIfWhitespace(data.ClassName),
			DependencyInjectionClassName = NullIfWhitespace(data.DependencyInjectionClassName),
			TelemetryNamesClassName = NullIfWhitespace(data.TelemetryNamesClassName),
			TelemetryNamesNamespace = NullIfWhitespace(data.TelemetryNamesNamespace),
		};

	/// <summary>
	/// Gets the ExcludeTargetsAttribute from a parameter, if present.
	/// </summary>
	public static ExcludeTargetsAttributeRecord? GetExcludeTargetsAttribute(
		IParameterSymbol parameter,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				parameter,
				TypeLibrary.TelemetryShared.ExcludeTargetsAttribute,
				token,
				out var attributeData
			)
		)
		{
			return null;
		}

		var data = ExcludeTargetsAttributeData.FromAttributeData(attributeData!);
		return data.Exists ? new((GenerationType)data.Targets) : null;
	}

	/// <summary>
	/// Checks if a parameter should be excluded from a specific target based on ExcludeTargetsAttribute.
	/// </summary>
	public static bool IsParameterExcludedFromTarget(
		IParameterSymbol parameter,
		GenerationType target,
		CancellationToken token
	)
	{
		var excludeTargets = GetExcludeTargetsAttribute(parameter, token);
		return excludeTargets?.ExcludedTargets.HasFlag(target) == true;
	}
}
