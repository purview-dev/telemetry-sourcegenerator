using Microsoft.CodeAnalysis;

namespace Purview.Telemetry.SourceGenerator.Helpers;

static class SymbolHelpers
{
	static readonly SymbolDisplayFormat FullyQualifiedNoGenericsNoGlobal = new(
		globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
		typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
		genericsOptions: SymbolDisplayGenericsOptions.None
	);

	static readonly SymbolDisplayFormat NamespaceOnly = new(
		globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted
,
		typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
		genericsOptions: SymbolDisplayGenericsOptions.None);

	public static string GetTypeName(ITypeSymbol symbol) => StripNullableAndGenerics(symbol).Name; // "List"

	/// <summary>
	/// If it's in the global namespace, returns null.
	/// </summary>
	public static string? GetNamespace(ITypeSymbol symbol)
	{
		var ns = StripNullableAndGenerics(symbol).ContainingNamespace;
		return ns is { IsGlobalNamespace: false }
			? ns.ToDisplayString(NamespaceOnly) // "System.Collections.Generic"
			: null;
	}

	/// <summary>
	/// Gets the fully qualified name of a type symbol, stripping nullable annotations and generics.
	/// </summary>
	public static string GetFullyQualifiedName(ITypeSymbol symbol) =>
		StripNullableAndGenerics(symbol).ToDisplayString(FullyQualifiedNoGenericsNoGlobal);

	static ITypeSymbol StripNullableAndGenerics(ITypeSymbol type)
	{
		// Unwraps System.Nullable<T> or reference‑type annotations "string?"
		type = StripNullable(type);

		// Remove concrete type arguments  (Dictionary<int,string> -> Dictionary<,>)
		if (type is INamedTypeSymbol named && named.IsGenericType)
		{
			// Keeps arity but no args
			type = named.OriginalDefinition;
		}

		return type;
	}

	static ITypeSymbol StripNullable(ITypeSymbol type)
	{
		// Removes value‑type "int?" and erroneous "Dictionary<int,string>?".
		if (
			type is INamedTypeSymbol named
			&& named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
		)
		{
			// Unwraps to the underlying T
			return named.TypeArguments[0];
		}

		// Strip reference‑type annotations "string?" ‑ just clear the annotation
		return type.WithNullableAnnotation(NullableAnnotation.None);
	}
}
