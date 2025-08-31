using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;

namespace Purview.Telemetry.SourceGenerator.Templates;

static class PurviewTypeFactory
{
	public static readonly Lazy<ImmutableDictionary<SpecialType, string>> AliasMap = new(
		CreateAliasMap
	);
	public static readonly Lazy<ImmutableDictionary<Type, SpecialType>> TypeMap = new(
		CreateTypeMap
	);

	static ImmutableDictionary<SpecialType, string> CreateAliasMap()
	{
		var aliasBuilder = ImmutableDictionary.CreateBuilder<SpecialType, string>();

		aliasBuilder.Add(SpecialType.System_Void, Constants.System.VoidKeyword);
		aliasBuilder.Add(SpecialType.System_Object, Constants.System.BuiltInTypes.ObjectKeyword);
		aliasBuilder.Add(SpecialType.System_String, Constants.System.BuiltInTypes.StringKeyword);
		aliasBuilder.Add(SpecialType.System_Boolean, Constants.System.BuiltInTypes.BoolKeyword);
		aliasBuilder.Add(SpecialType.System_Char, Constants.System.BuiltInTypes.CharKeyword);
		aliasBuilder.Add(SpecialType.System_Byte, Constants.System.BuiltInTypes.ByteKeyword);
		aliasBuilder.Add(SpecialType.System_SByte, Constants.System.BuiltInTypes.SByteKeyword);
		aliasBuilder.Add(SpecialType.System_Int16, Constants.System.BuiltInTypes.ShortKeyword);
		aliasBuilder.Add(SpecialType.System_UInt16, Constants.System.BuiltInTypes.UShortKeyword);
		aliasBuilder.Add(SpecialType.System_Int32, Constants.System.BuiltInTypes.IntKeyword);
		aliasBuilder.Add(SpecialType.System_UInt32, Constants.System.BuiltInTypes.UIntKeyword);
		aliasBuilder.Add(SpecialType.System_Int64, Constants.System.BuiltInTypes.LongKeyword);
		aliasBuilder.Add(SpecialType.System_UInt64, Constants.System.BuiltInTypes.ULongKeyword);
		aliasBuilder.Add(SpecialType.System_Decimal, Constants.System.BuiltInTypes.DecimalKeyword);
		aliasBuilder.Add(SpecialType.System_Single, Constants.System.BuiltInTypes.FloatKeyword);
		aliasBuilder.Add(SpecialType.System_Double, Constants.System.BuiltInTypes.DoubleKeyword);

		return aliasBuilder.ToImmutable();
	}

	static ImmutableDictionary<Type, SpecialType> CreateTypeMap()
	{
		var typeBuilder = ImmutableDictionary.CreateBuilder<Type, SpecialType>();

		typeBuilder.Add(typeof(object), SpecialType.System_Object);
		typeBuilder.Add(typeof(string), SpecialType.System_String);
		typeBuilder.Add(typeof(bool), SpecialType.System_Boolean);
		typeBuilder.Add(typeof(char), SpecialType.System_Char);
		typeBuilder.Add(typeof(byte), SpecialType.System_Byte);
		typeBuilder.Add(typeof(sbyte), SpecialType.System_SByte);
		typeBuilder.Add(typeof(short), SpecialType.System_Int16);
		typeBuilder.Add(typeof(ushort), SpecialType.System_UInt16);
		typeBuilder.Add(typeof(int), SpecialType.System_Int32);
		typeBuilder.Add(typeof(uint), SpecialType.System_UInt32);
		typeBuilder.Add(typeof(long), SpecialType.System_Int64);
		typeBuilder.Add(typeof(ulong), SpecialType.System_UInt64);
		typeBuilder.Add(typeof(decimal), SpecialType.System_Decimal);
		typeBuilder.Add(typeof(float), SpecialType.System_Single);
		typeBuilder.Add(typeof(double), SpecialType.System_Double);

		return typeBuilder.ToImmutable();
	}

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

		return new(typeName, fullName, @namespace, null, isNullable, SpecialType.None, []);
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

		var systemAlias = AliasMap.Value.GetValueOrDefault(typeSymbol.SpecialType);
		return new(
			TypeName: SymbolHelpers.GetTypeName(typeSymbol),
			FullyQualifiedName: SymbolHelpers.GetFullyQualifiedName(typeSymbol),
			Namespace: SymbolHelpers.GetNamespace(typeSymbol),
			SystemAlias: systemAlias,
			IsNullable: isNullable,
			SpecialType: typeSymbol.SpecialType,
			GenericTypeArguments: typeArguments
		);
	}

	public static PurviewTypeInfo Create<T>() => Create(typeof(T));

	public static PurviewTypeInfo Create(Type type)
	{
		if (type == null)
			throw new ArgumentNullException(nameof(type));

		var nullableType = Nullable.GetUnderlyingType(type);
		var isNullable = nullableType != null;
		if (isNullable)
			type = nullableType!;

		var fullName = type.FullName ?? throw new ArgumentException("Type must have a full name.");
		var typeName = type.Name;
		var @namespace = type.Namespace ?? "";

		// Handle generic types
		ImmutableArray<PurviewTypeInfo> typeArguments = [];
		if (type.IsGenericType && !type.IsGenericTypeDefinition)
			typeArguments = [.. type.GetGenericArguments().Select(Create)];

		var specialType = TypeMap.Value.GetValueOrDefault(type, SpecialType.None);
		var alias =
			specialType == SpecialType.None ? null : AliasMap.Value.GetValueOrDefault(specialType);

		return new(typeName, fullName, @namespace, alias, isNullable, specialType, typeArguments);
	}

	public static PurviewTypeInfo Create(SpecialType specialType)
	{
		if (!AliasMap.Value.TryGetValue(specialType, out var alias))
		{
			throw new ArgumentOutOfRangeException(
				nameof(specialType),
				$"SpecialType '{specialType}' does not have a known alias."
			);
		}

		var fullName = specialType.ToString().Replace("System_", "System.");
		return new(
			TypeName: alias,
			FullyQualifiedName: fullName,
			Namespace: "System",
			SystemAlias: alias,
			IsNullable: false,
			SpecialType: specialType,
			GenericTypeArguments: []
		);
	}
}
