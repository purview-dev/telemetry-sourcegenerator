namespace Purview.Telemetry.SourceGenerator.Templates;

public class PurviewTypeInfoTests
{
	[Test]
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

	[Test]
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

	[Test]
	public void Create_GivenSpecialTypeOfString_CreatesPurviewTypeInfoWithAliasAndSpecialType()
	{
		// Arrange & Act
		var typeInfo = PurviewTypeFactory.Create(SpecialType.System_String);

		// Assert
		typeInfo.TypeName.ShouldBe("String");
		typeInfo.FullyQualifiedName.ShouldBe("System.String");
		typeInfo.Namespace.ShouldBe("System");
		typeInfo.IsNullable.ShouldBeFalse();
		typeInfo.SystemAlias.ShouldBe("string");
		typeInfo.SpecialType.ShouldBe(SpecialType.System_String);
		typeInfo.GenericTypeArguments.ShouldBeEmpty();
	}
}
