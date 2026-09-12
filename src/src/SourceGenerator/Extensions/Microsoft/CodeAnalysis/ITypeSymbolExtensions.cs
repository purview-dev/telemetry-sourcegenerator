using System.ComponentModel;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis;

[EditorBrowsable(EditorBrowsableState.Never)]
static class ITypeSymbolExtensions
{
	extension(ITypeSymbol typeSymbol)
	{
		public bool IsComplexType()
		{
			// Check for class, struct, or record types
			if (typeSymbol.TypeKind is TypeKind.Class or TypeKind.Struct)
			{
				// Exclude primitive types and special types like string
				if (typeSymbol.SpecialType is SpecialType.None)
					return true;
			}

			return false;
		}

		public bool IsArray() =>
			typeSymbol.SpecialType != SpecialType.System_String && typeSymbol.TypeKind is TypeKind.Array;

		public bool IsIEnumerable(Compilation compilation)
		{
			if (typeSymbol.SpecialType == SpecialType.System_String)
				return false;
			if (IsIEnumerable(typeSymbol))
				return true;

			// Get the `IEnumerable` symbol from the compilation
			var ienumerableSymbol = compilation.GetTypeByMetadataName(TypeLibrary.System.Collections.IEnumerable);
			// Check if the type implements `IEnumerable`
			return ienumerableSymbol != null
				&& typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, ienumerableSymbol));
		}

		public bool IsExceptionType()
		{
			// The type itself, or any base in the hierarchy, derives from System.Exception.
			return TypeLibrary.System.Exception.Equals(typeSymbol)
				|| TypeHelpers.InheritsFrom(typeSymbol, TypeLibrary.System.Exception);
		}
	}

	static bool IsIEnumerable(ITypeSymbol typeSymbol)
	{
		if (typeSymbol.SpecialType == SpecialType.System_String)
			return false;
		// Check for common enumerable types
		return typeSymbol.SpecialType
			is SpecialType.System_Collections_IEnumerable
				or SpecialType.System_Collections_Generic_ICollection_T
				or SpecialType.System_Collections_Generic_IList_T
				or SpecialType.System_Collections_Generic_IReadOnlyCollection_T
				or SpecialType.System_Collections_Generic_IReadOnlyList_T
				or SpecialType.System_Collections_Generic_IEnumerable_T;
	}
}
