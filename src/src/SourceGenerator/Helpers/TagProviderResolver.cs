using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

/// <summary>
/// Why a <c>[TagProvider]</c> could not be used. Each value maps to one <c>TSG</c> rule.
/// </summary>
enum TagProviderStatus
{
	/// <summary>The provider was resolved to a usable method.</summary>
	Resolved,

	/// <summary>The provider type named by the attribute cannot be resolved.</summary>
	TypeNotFound,

	/// <summary>The provider type declares no method with the attribute's name.</summary>
	MethodNotFound,

	/// <summary>The named method exists but cannot be used as a tag provider.</summary>
	InvalidSignature,
}

/// <summary>
/// The outcome of resolving a <c>[TagProvider]</c>: either a target the emitter can call, or the reason there is none.
/// </summary>
readonly record struct TagProviderResolution(TagProviderStatus Status, TagProviderTarget? Target)
{
	public static TagProviderResolution Failed(TagProviderStatus status) => new(status, null);
}

/// <summary>
/// Resolves and validates <c>[TagProvider(typeof(T), "M")]</c> on a logging parameter.
/// </summary>
/// <remarks>
/// Modelled on <c>Microsoft.Gen.Logging</c>'s <c>Parser.TagProvider</c>, with one deliberate difference: the .NET 11
/// collector shape (<c>static void M(ITagCollector, T)</c>) is not accepted, because implementing
/// <c>ITagCollector</c> in generated code would pull <c>Microsoft.Extensions.Compliance.Classification</c> into every
/// consumer's build. Only the pair-returning shape the generated state can consume is supported; a collector-shaped
/// method is reported as <see cref="TagProviderStatus.InvalidSignature"/> and recorded in
/// <c>docs/wiki/Platform-Support.md</c>.
/// </remarks>
static class TagProviderResolver
{
	public static TagProviderResolution Resolve(
		IParameterSymbol parameter,
		TagProviderAttributeData attribute,
		Compilation compilation,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		// An unresolvable `typeof(...)` leaves the model's TypeIdentity at its default, which names nothing.
		if (string.IsNullOrWhiteSpace(attribute.ProviderType.Name))
			return TagProviderResolution.Failed(TagProviderStatus.TypeNotFound);

		var providerType = compilation.GetTypeByMetadataName(attribute.ProviderType.MetadataFullName);
		if (providerType is null)
			return TagProviderResolution.Failed(TagProviderStatus.TypeNotFound);

		if (attribute.ProviderMethod is not { } providerMethodName)
			return TagProviderResolution.Failed(TagProviderStatus.MethodNotFound);

		var candidates = providerType.GetMembers(providerMethodName).OfType<IMethodSymbol>().ToArray();
		if (candidates.Length == 0)
			return TagProviderResolution.Failed(TagProviderStatus.MethodNotFound);

		foreach (var candidate in candidates)
		{
			token.ThrowIfCancellationRequested();

			if (
				!candidate.IsStatic
				|| candidate.IsGenericMethod
				|| candidate.Parameters.Length != 1
				|| candidate.Parameters[0].RefKind != RefKind.None
			)
			{
				continue;
			}

			if (!IsAcceptedReturnType(candidate.ReturnType))
				continue;

			if (!AcceptsParameter(candidate.Parameters[0].Type, parameter.Type))
				continue;

			if (!IsVisible(candidate))
				continue;

			return new(
				TagProviderStatus.Resolved,
				new(
					ProviderTypeName: providerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
					MethodName: candidate.Name,
					OmitReferenceName: attribute.OmitReferenceName
				)
			);
		}

		return TagProviderResolution.Failed(TagProviderStatus.InvalidSignature);
	}

	/// <summary>
	/// Whether the method returns something the generated <c>foreach</c> can read as key/value pairs. Accepts
	/// <c>IEnumerable&lt;KeyValuePair&lt;string, object?&gt;&gt;</c>, the <c>string</c>-valued variant, and any type
	/// implementing one of them (<c>KeyValuePair&lt;string, object&gt;[]</c>, <c>List&lt;...&gt;</c>, ...).
	/// </summary>
	static bool IsAcceptedReturnType(ITypeSymbol returnType)
	{
		if (TryGetEnumerableElementType(returnType, out var element) && IsAcceptedPair(element))
			return true;

		foreach (var candidate in returnType.AllInterfaces)
		{
			if (TryGetEnumerableElementType(candidate, out element) && IsAcceptedPair(element))
				return true;
		}

		return false;
	}

	static bool TryGetEnumerableElementType(ITypeSymbol type, out ITypeSymbol? element)
	{
		element = null;

		if (type is not INamedTypeSymbol named)
			return false;

		if (named.OriginalDefinition.SpecialType != SpecialType.System_Collections_Generic_IEnumerable_T)
			return false;

		element = named.TypeArguments[0];
		return true;
	}

	static bool IsAcceptedPair(ITypeSymbol? element)
	{
		if (element is not INamedTypeSymbol { Arity: 2 } pair)
			return false;

		if (pair.Name != "KeyValuePair")
			return false;

		if (pair.ContainingNamespace?.ToDisplayString() != "System.Collections.Generic")
			return false;

		if (pair.TypeArguments[0].SpecialType != SpecialType.System_String)
			return false;

		// A nullability annotation is irrelevant to whether the pair can be read, so both `object?` and `object` count.
		var valueType = pair.TypeArguments[1].WithNullableAnnotation(NullableAnnotation.None).ToDisplayString();

		return valueType is "object" or "string";
	}

	/// <summary>
	/// Whether the provider method's single parameter can accept the annotated parameter: the same type, a base type,
	/// an implemented interface, <c>object</c>, or a type parameter the annotated type satisfies.
	/// </summary>
	static bool AcceptsParameter(ITypeSymbol providerParameterType, ITypeSymbol annotatedType)
	{
		if (providerParameterType is ITypeParameterSymbol)
			return true;

		if (providerParameterType.SpecialType == SpecialType.System_Object)
			return true;

		var providerType = providerParameterType.WithNullableAnnotation(NullableAnnotation.None);
		for (var current = annotatedType; current is not null; current = current.BaseType)
		{
			if (
				SymbolEqualityComparer.Default.Equals(
					current.WithNullableAnnotation(NullableAnnotation.None),
					providerType
				)
			)
				return true;
		}

		foreach (var candidate in annotatedType.AllInterfaces)
		{
			if (
				SymbolEqualityComparer.Default.Equals(
					candidate.WithNullableAnnotation(NullableAnnotation.None),
					providerType
				)
			)
				return true;
		}

		return false;
	}

	/// <summary>
	/// Whether the provider method is reachable from generated code in the consumer's assembly. The .NET generator
	/// walks the containing symbols and rejects private and protected members.
	/// </summary>
	static bool IsVisible(ISymbol symbol)
	{
		for (
			var current = symbol;
			current is not null && current.Kind != SymbolKind.Namespace;
			current = current.ContainingSymbol
		)
		{
			if (
				current.DeclaredAccessibility
				is Accessibility.Private
					or Accessibility.Protected
					or Accessibility.ProtectedAndInternal
			)
			{
				return false;
			}
		}

		return true;
	}
}
