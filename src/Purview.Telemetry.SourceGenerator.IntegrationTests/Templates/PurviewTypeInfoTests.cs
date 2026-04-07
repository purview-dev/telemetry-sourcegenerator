namespace Purview.Telemetry.SourceGenerator.Templates;

public class PurviewTypeInfoTests
{
	[Test]
	public async Task Create_GivenBasicTypeNameAsString_CreatesPurviewTypeInfo()
	{
		// Arrange
		var type = GetType();
		var fullName = type.FullName!;
		var expectedNamespace = type.Namespace!;
		var expectedName = type.Name;

		// Arrange & Act
		var typeInfo = PurviewTypeFactory.Create(fullName);

		// Assert
		await Assert.That(typeInfo.TypeName).IsEqualTo(expectedName);
		await Assert.That(typeInfo.FullyQualifiedName).IsEqualTo(fullName);
		await Assert.That(typeInfo.Namespace).IsEqualTo(expectedNamespace);
		await Assert.That(typeInfo.IsNullable).IsFalse();
		await Assert.That(typeInfo.SystemAlias).IsNull();
		await Assert.That(typeInfo.SpecialType).IsEqualTo(SpecialType.None);
		await Assert.That(typeInfo.GenericTypeArguments).IsEmpty();
	}

	[Test]
	public async Task Create_GivenNestedTypeAsAString_CreatesPurviewTypeInfo()
	{
		// Arrange
		var type = typeof(System.Collections.Concurrent.Partitioner);
		var fullName = type.FullName!;
		var expectedNamespace = type.Namespace!;
		var expectedName = type.Name;

		// Arrange & Act
		var typeInfo = PurviewTypeFactory.Create(fullName);

		// Assert
		await Assert.That(typeInfo.TypeName).IsEqualTo(expectedName);
		await Assert.That(typeInfo.FullyQualifiedName).IsEqualTo(fullName);
		await Assert.That(typeInfo.Namespace).IsEqualTo(expectedNamespace);
		await Assert.That(typeInfo.IsNullable).IsFalse();
		await Assert.That(typeInfo.SystemAlias).IsNull();
		await Assert.That(typeInfo.SpecialType).IsEqualTo(SpecialType.None);
		await Assert.That(typeInfo.GenericTypeArguments).IsEmpty();
	}

	[Test]
	public async Task Create_GivenSpecialTypeOfString_CreatesPurviewTypeInfoWithAliasAndSpecialType()
	{
		// Arrange & Act
		var typeInfo = PurviewTypeFactory.Create(SpecialType.System_String);

		// Assert
		await Assert.That(typeInfo.TypeName).IsEqualTo("String");
		await Assert.That(typeInfo.FullyQualifiedName).IsEqualTo("System.String");
		await Assert.That(typeInfo.Namespace).IsEqualTo("System");
		await Assert.That(typeInfo.IsNullable).IsFalse();
		await Assert.That(typeInfo.SystemAlias).IsEqualTo("string");
		await Assert.That(typeInfo.SpecialType).IsEqualTo(SpecialType.System_String);
		await Assert.That(typeInfo.GenericTypeArguments).IsEmpty();
	}
}
