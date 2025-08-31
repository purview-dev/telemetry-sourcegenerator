namespace Purview.Telemetry.SourceGenerator.Templates;

public class PurviewTypeInfoTests
{
	[Fact]
	public void Create_GivenBasicTypeNameAsString_CreatesPurviewTypeInfo()
	{
		// Arrange
		var type = GetType();
		var fullName = type.FullName!;
		var expectedNamespace = type.Namespace!;
		var expectedName = type.Name;

		// Arrange & Act
		var typeInfo = PurviewTypeFactory.Create(fullName);

		// Assert
		typeInfo.TypeName.ShouldBe(expectedName);
		typeInfo.FullyQualifiedName.ShouldBe(fullName);
		typeInfo.Namespace.ShouldBe(expectedNamespace);
		typeInfo.IsNullable.ShouldBeFalse();
		typeInfo.SystemAlias.ShouldBeNull();
		typeInfo.SpecialType.ShouldBe(SpecialType.None);
		typeInfo.GenericTypeArguments.ShouldBeEmpty();
	}

	[Fact]
	public void Create_GivenNestedTypeAsAString_CreatesPurviewTypeInfo()
	{
		// Arrange
		var type = typeof(System.Collections.Concurrent.Partitioner);
		var fullName = type.FullName!;
		var expectedNamespace = type.Namespace!;
		var expectedName = type.Name;

		// Arrange & Act
		var typeInfo = PurviewTypeFactory.Create(fullName);

		// Assert
		typeInfo.TypeName.ShouldBe(expectedName);
		typeInfo.FullyQualifiedName.ShouldBe(fullName);
		typeInfo.Namespace.ShouldBe(expectedNamespace);
		typeInfo.IsNullable.ShouldBeFalse();
		typeInfo.SystemAlias.ShouldBeNull();
		typeInfo.SpecialType.ShouldBe(SpecialType.None);
		typeInfo.GenericTypeArguments.ShouldBeEmpty();
	}

	[Theory(DisplayName = "Ensures PurviewTypeInfo can be created from a SpecialType")]
	[MemberData(nameof(SpecialTypesData))]
	public void Create_GivenSpecialType_CreatesPurviewTypeInfoWithAliasAndSpecialType(
		SpecialType specialType
	)
	{
		// Arrange
		var systemAlias = PurviewTypeFactory.AliasMap.Value.GetValueOrDefault(specialType);
		var systemType = PurviewTypeFactory
			.TypeMap.Value.SingleOrDefault(kv => kv.Value == specialType)
			.Key;

		// Act
		var typeInfo = PurviewTypeFactory.Create(specialType);

		// Assert
		systemAlias.ShouldNotBeNull();
		typeInfo.ShouldNotBeNull();

		typeInfo.TypeName.ShouldBe(typeInfo.TypeName);
		typeInfo.FullyQualifiedName.ShouldBe(typeInfo.FullyQualifiedName);
		typeInfo.Namespace.ShouldBe(typeInfo.Namespace);
		typeInfo.IsNullable.ShouldBeFalse();
		typeInfo.SystemAlias.ShouldBe(systemAlias);
		typeInfo.SpecialType.ShouldBe(specialType);
		typeInfo.GenericTypeArguments.ShouldBeEmpty();
	}

	public static TheoryData<SpecialType> SpecialTypesData =>
		[
			SpecialType.System_Boolean,
			SpecialType.System_Byte,
			SpecialType.System_Char,
			SpecialType.System_Decimal,
			SpecialType.System_Double,
			SpecialType.System_Int16,
			SpecialType.System_Int32,
			SpecialType.System_Int64,
			SpecialType.System_Object,
			SpecialType.System_SByte,
			SpecialType.System_Single,
			SpecialType.System_String,
			SpecialType.System_UInt16,
			SpecialType.System_UInt32,
			SpecialType.System_UInt64,
		];
}
