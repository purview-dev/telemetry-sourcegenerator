using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

static partial class SharedHelpers
{
	public static GenerationType GetGenerationTypes(ISymbol symbol, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		var generationType = GenerationType.None;

		if (Utilities.ContainsAttribute(symbol, TypeLibrary.Purview.Telemetry.ActivitySourceAttribute, token))
			generationType |= GenerationType.Activities;

		if (Utilities.ContainsAttribute(symbol, TypeLibrary.Purview.Telemetry.LoggerAttribute, token))
			generationType |= GenerationType.Logging;

		if (Utilities.ContainsAttribute(symbol, TypeLibrary.Purview.Telemetry.MeterAttribute, token))
			generationType |= GenerationType.Metrics;

		return generationType;
	}

	/// <summary>
	/// Projects a type symbol into a <c>TypeReference</c>, including an open generic type parameter.
	/// </summary>
	/// <remarks>
	/// A type parameter cannot use the framework's <c>TypeReference.ForTypeParameter</c> here: the writer's
	/// parameter validation requires a named identity, so a type-parameter reference is rejected as a parameter type.
	/// A namespace-less identity renders as the bare parameter name and satisfies that check.
	/// </remarks>
	public static TypeReference ToTypeReference(ITypeSymbol type) =>
		type is ITypeParameterSymbol typeParameter
			? new TypeReference(new TypeIdentity(typeParameter.Name, null))
			: TypeReference.Create(type);

	/// <summary>
	/// Builds the type reference for the interface a generated class implements, including a generic interface's own
	/// type parameters.
	/// </summary>
	/// <remarks>
	/// The framework's <c>TypeReference.Create(ITypeSymbol)</c> cannot name an open type parameter, so a generic
	/// interface declaration is rebuilt from its name, namespace and type-parameter names. A nested generic interface
	/// is reported as unsupported rather than half-built here.
	/// </remarks>
	public static TypeReference GetInterfaceTypeReference(INamedTypeSymbol interfaceSymbol)
	{
		if (interfaceSymbol.Arity == 0)
			return TypeReference.Create(interfaceSymbol);

		TypeIdentity identity = new(
			interfaceSymbol.Name,
			interfaceSymbol.ContainingNamespace.IsGlobalNamespace
				? null
				: interfaceSymbol.ContainingNamespace.ToDisplayString(),
			interfaceSymbol.Arity
		);

		TypeReference[] typeArguments = [.. interfaceSymbol.TypeParameters.Select(ToTypeReference)];

		return new TypeReference(identity.MakeGeneric(typeArguments));
	}

	/// <summary>
	/// Whether every type parameter of an interface can be mirrored by the generated class. Only the
	/// <c>allows ref struct</c> anti-constraint is out of reach, because it cannot be repeated on the generated
	/// declaration.
	/// </summary>
	public static bool AreInterfaceTypeParametersSupported(INamedTypeSymbol interfaceSymbol) =>
		TypeParameterHelpers.AreSupported(interfaceSymbol.TypeParameters) && interfaceSymbol.ContainingType is null;

	public static bool ShouldEmit(GenerationType requestingType, GenerationType generationType)
	{
		// For multi-targeting: emit if the interface has the requesting target type
		// This allows ActivitySource+Logger+Meter interfaces to generate all three targets
		return generationType.HasFlag(requestingType);
	}

	/// <summary>
	/// Whether the compilation can resolve a type. Gate every emitted API on this rather than on the target
	/// framework: an analyzer-only package reference never brings a type to the consumer, and the
	/// System.Diagnostics.DiagnosticSource package carries .NET 9/10/11 types all the way down to
	/// <c>net462</c> and <c>netstandard2.0</c>.
	/// </summary>
	public static bool IsTypeAvailable(Compilation compilation, TypeIdentity identity) =>
		compilation.GetTypeByMetadataName(identity.MetadataFullName) is not null;

	/// <summary>
	/// Whether the compilation can resolve <c>System.Diagnostics.Metrics.IMeterFactory</c>. It arrives with
	/// Microsoft.Extensions.Diagnostics.Abstractions (or the shared framework on .NET 8+), so a project can target a
	/// modern framework and still not have it — the symbol probe, not the target framework, decides whether the
	/// generated constructor may take one.
	/// </summary>
	public static bool IsMeterFactoryAvailable(Compilation compilation) =>
		IsTypeAvailable(compilation, TypeLibrary.System.Diagnostics.Metrics.IMeterFactory);

	/// <summary>
	/// Whether the compilation can resolve <c>Activity.AddException</c> (.NET 9, or System.Diagnostics.DiagnosticSource
	/// 9+ on any target). When it can, the generated exception event is produced by the framework, which names and
	/// tags the event the way the runtime's own diagnostics do.
	/// </summary>
	public static bool IsActivityAddExceptionAvailable(Compilation compilation) =>
		compilation
			.GetTypeByMetadataName(TypeLibrary.System.Diagnostics.Activity.MetadataFullName)
			?.GetMembers("AddException")
			.Length > 0;

	/// <summary>
	/// The assembly's version, used as the default meter and activity-source version when an attribute does not set
	/// one. Read from the assembly identity, so <c>[assembly: AssemblyVersion]</c> and the SDK's own version
	/// generation both feed it. Resolved once per compilation (see <c>TelemetryCapabilities</c>) rather than per
	/// target, because the pipeline builds one capabilities value for the whole project.
	/// </summary>
	public static string? GetAssemblyVersion(Compilation compilation)
	{
		var version = compilation.Assembly.Identity.Version;

		// 0.0.0.0 is what the compiler supplies for a project that declares no version. Carrying it into
		// MeterOptions.Version or ActivitySourceOptions.Version would report a meaningless version to every
		// exporter, so an undeclared version stays absent and the emitted options keep their single-argument form.
		if (version == default)
			return null;

		if (version is { Major: 0, Minor: 0, Build: <= 0, Revision: <= 0 })
			return null;

		return version.ToString();
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
		GenerateTelemetryNamesAttribute: true,
		GenerateTelemetryNamesClass: false,
		TelemetryNamesClassName: null,
		TelemetryNamesNamespace: null,
		AggregateReferencedTelemetryNames: true
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

		// If the type doesn't have the attribute, fall back to the assembly-level attribute or defaults.
		return GetAssemblyTelemetryGenerationAttribute(compilation);
	}

	/// <summary>
	/// Reads the assembly-level <c>[TelemetryGeneration]</c>, falling back to the defaults. The
	/// telemetry-names pipeline needs this independently of any target: an assembly that only consumes
	/// telemetry from referenced projects has no interface to hang the settings off.
	/// </summary>
	public static TelemetryGenerationAttributeData GetAssemblyTelemetryGenerationAttribute(Compilation compilation)
	{
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
				TypeLibrary.Purview.Telemetry.ExcludeTargetsAttribute,
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
