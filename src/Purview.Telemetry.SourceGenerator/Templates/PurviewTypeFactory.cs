using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;

namespace Purview.Telemetry.SourceGenerator.Templates;

static class PurviewTypeFactory
{
	static readonly ImmutableDictionary<SpecialType, string> AliasMap = new Dictionary<
		SpecialType,
		string
	>
	{
		{ SpecialType.System_Void, Constants.System.VoidKeyword },
		{ SpecialType.System_Object, "object" },
		{ SpecialType.System_String, "string" },
		{ SpecialType.System_Boolean, "bool" },
		{ SpecialType.System_Char, "char" },
		{ SpecialType.System_Byte, "byte" },
		{ SpecialType.System_SByte, "sbyte" },
		{ SpecialType.System_Int16, "short" },
		{ SpecialType.System_UInt16, "ushort" },
		{ SpecialType.System_Int32, "int" },
		{ SpecialType.System_UInt32, "uint" },
		{ SpecialType.System_Int64, "long" },
		{ SpecialType.System_UInt64, "ulong" },
		{ SpecialType.System_Decimal, "decimal" },
		{ SpecialType.System_Single, "float" },
		{ SpecialType.System_Double, "double" },
	}.ToImmutableDictionary();

	public static PurviewTypeInfo Create(string fullName)
	{
		if (string.IsNullOrWhiteSpace(fullName))
			throw new ArgumentNullException(nameof(fullName));

		var lastDotIndex = fullName.LastIndexOf('.');
		if (lastDotIndex < 0)
		{
			throw new ArgumentException(
				"Type name must contain a namespace and a type name.",
				nameof(fullName)
			);
		}

		var typeName = fullName.Substring(lastDotIndex + 1);
		var @namespace = fullName.Substring(0, lastDotIndex);
		var isNullable = typeName[typeName.Length - 1] == '?';

		return new(typeName, fullName, @namespace, null, isNullable, IsValueType: false, SpecialType.None, []);
	}

	public static PurviewTypeInfo Create(ITypeSymbol typeSymbol)
	{
		if (typeSymbol == null)
			throw new ArgumentNullException(nameof(typeSymbol));

		var isNullable = typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
		ImmutableArray<PurviewTypeInfo> typeArguments = [];
		if (
			typeSymbol as INamedTypeSymbol is
			{ IsGenericType: true, IsValueType: false } genericType
		)
		{
			typeArguments = [.. genericType.TypeArguments.Select(Create)];
		}

		var systemAlias = AliasMap.GetValueOrDefault(typeSymbol.SpecialType);

		return new(
			TypeName: SymbolHelpers.GetTypeName(typeSymbol),
			FullyQualifiedName: SymbolHelpers.GetFullyQualifiedName(typeSymbol),
			Namespace: SymbolHelpers.GetNamespace(typeSymbol),
			SystemAlias: systemAlias,
			IsNullable: isNullable,
			IsValueType: typeSymbol.IsValueType,
			SpecialType: typeSymbol.SpecialType,
			GenericTypeArguments: typeArguments
		);
	}

	public static PurviewTypeInfo Create<T>() => Create(typeof(T).FullName);

	public static PurviewTypeInfo Create(SpecialType special)
	{
		return special switch
		{
			SpecialType.System_Void => new(
				Constants.System.VoidKeyword,
				Constants.System.VoidKeyword,
				"System",
				Constants.System.VoidKeyword,
				false,
				IsValueType: false,
				special,
				[]
			),
			SpecialType.System_Object => new(
				"Object",
				"System.Object",
				"System",
				"object",
				false,
				IsValueType: false,
				special,
				[]
			),
			SpecialType.System_String => new(
				"String",
				"System.String",
				"System",
				"string",
				false,
				IsValueType: false,
				special,
				[]
			),
			SpecialType.System_Boolean => new(
				"Boolean",
				"System.Boolean",
				"System",
				"bool",
				false,
				IsValueType: true,
				special,
				[]
			),
			SpecialType.System_Char => new(
				"Char",
				"System.Char",
				"System",
				"char",
				false,
				IsValueType: true,
				special,
				[]
			),
			SpecialType.System_Byte => new(
				"Byte",
				"System.Byte",
				"System",
				"byte",
				false,
				IsValueType: true,
				special,
				[]
			),
			SpecialType.System_SByte => new(
				"SByte",
				"System.SByte",
				"System",
				"sbyte",
				false,
				IsValueType: true,
				special,
				[]
			),
			SpecialType.System_Int16 => new(
				"Int16",
				"System.Int16",
				"System",
				"short",
				false,
				IsValueType: true,
				special,
				[]
			),
			SpecialType.System_UInt16 => new(
				"UInt16",
				"System.UInt16",
				"System",
				"ushort",
				false,
				IsValueType: true,
				special,
				[]
			),
			SpecialType.System_Int32 => new(
				"Int32",
				"System.Int32",
				"System",
				"int",
				false,
				IsValueType: true,
				special,
				[]
			),
			SpecialType.System_UInt32 => new(
				"UInt32",
				"System.UInt32",
				"System",
				"uint",
				false,
				IsValueType: true,
				special,
				[]
			),
			SpecialType.System_Int64 => new(
				"Int64",
				"System.Int64",
				"System",
				"long",
				false,
				IsValueType: true,
				special,
				[]
			),
			SpecialType.System_UInt64 => new(
				"UInt64",
				"System.UInt64",
				"System",
				"ulong",
				false,
				IsValueType: true,
				special,
				[]
			),
			SpecialType.System_Decimal => new(
				"Decimal",
				"System.Decimal",
				"System",
				"decimal",
				false,
				IsValueType: true,
				special,
				[]
			),
			SpecialType.System_Single => new(
				"Single",
				"System.Single",
				"System",
				"float",
				false,
				IsValueType: true,
				special,
				[]
			),
			SpecialType.System_Double => new(
				"Double",
				"System.Double",
				"System",
				"double",
				false,
				IsValueType: true,
				special,
				[]
			),
			_ => throw new ArgumentOutOfRangeException(nameof(special), special, null),
		};
	}
}
