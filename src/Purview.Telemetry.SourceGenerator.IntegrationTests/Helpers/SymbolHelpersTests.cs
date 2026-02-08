using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Purview.Telemetry.SourceGenerator.Helpers;

namespace Purview.Telemetry.SourceGenerator.Tests.Helpers;

public class SymbolHelpersTests
{
	static CSharpCompilation CreateCompilation(string source)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source);
		var references = AppDomain
			.CurrentDomain.GetAssemblies()
			.Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
			.Select(a => MetadataReference.CreateFromFile(a.Location))
			.ToList();

		return CSharpCompilation.Create(
			"TestAssembly",
			[syntaxTree],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);
	}

	static ITypeSymbol GetTypeSymbol(Compilation compilation, string typeName)
	{
		return compilation.GetTypeByMetadataName(typeName)!;
	}

	[Test]
	public async Task GetTypeName_GivenSimpleType_ReturnsTypeName()
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Customer {
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = GetTypeSymbol(compilation, "Test.Customer");

		// Act
		var result = SymbolHelpers.GetTypeName(typeSymbol);

		// Assert
		await Assert.That(result).IsEqualTo("Customer");
	}

	[Test]
	public async Task GetTypeName_GivenGenericType_ReturnsBaseTypeName()
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Container<T> {
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = compilation.GetTypeByMetadataName("Test.Container`1")!;

		// Act
		var result = SymbolHelpers.GetTypeName(typeSymbol);

		// Assert
		await Assert.That(result).IsEqualTo("Container");
	}

	[Test]
	public async Task GetTypeName_GivenNullableType_ReturnsBaseTypeName()
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Program {
		public int? Value { get; set; }
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = compilation.GetTypeByMetadataName("Test.Program")!;
		var propertySymbol = typeSymbol
			.GetMembers("Value")
			.OfType<IPropertySymbol>()
			.First();

		// Act
		var result = SymbolHelpers.GetTypeName(propertySymbol.Type);

		// Assert
		await Assert.That(result).IsEqualTo("Int32");
	}

	[Test]
	public async Task GetNamespace_GivenTypeInNamespace_ReturnsNamespace()
	{
		// Arrange
		const string source =
			@"
namespace Test.Services {
	public class CustomerService {
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = GetTypeSymbol(compilation, "Test.Services.CustomerService");

		// Act
		var result = SymbolHelpers.GetNamespace(typeSymbol);

		// Assert
		await Assert.That(result).IsEqualTo("Test.Services");
	}

	[Test]
	public async Task GetNamespace_GivenTypeInGlobalNamespace_ReturnsNull()
	{
		// Arrange
		const string source =
			@"
public class GlobalClass {
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = compilation.GetTypeByMetadataName("GlobalClass")!;

		// Act
		var result = SymbolHelpers.GetNamespace(typeSymbol);

		// Assert
		await Assert.That(result).IsNull();
	}

	[Test]
	public async Task GetNamespace_GivenGenericType_ReturnsNamespace()
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Container<T> {
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = compilation.GetTypeByMetadataName("Test.Container`1")!;

		// Act
		var result = SymbolHelpers.GetNamespace(typeSymbol);

		// Assert
		await Assert.That(result).IsEqualTo("Test");
	}

	[Test]
	public async Task GetFullyQualifiedName_GivenSimpleType_ReturnsFullyQualifiedName()
	{
		// Arrange
		const string source =
			@"
namespace Test.Services {
	public class CustomerService {
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = GetTypeSymbol(compilation, "Test.Services.CustomerService");

		// Act
		var result = SymbolHelpers.GetFullyQualifiedName(typeSymbol);

		// Assert
		await Assert.That(result).IsEqualTo("Test.Services.CustomerService");
	}

	[Test]
	public async Task GetFullyQualifiedName_GivenGenericType_ReturnsFullyQualifiedNameWithoutTypeArgs()
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Container<T> {
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = compilation.GetTypeByMetadataName("Test.Container`1")!;

		// Act
		var result = SymbolHelpers.GetFullyQualifiedName(typeSymbol);

		// Assert
		// SymbolHelpers strips generics completely per its documentation
		await Assert.That(result).IsEqualTo("Test.Container");
	}

	[Test]
	public async Task GetFullyQualifiedName_GivenNullableType_ReturnsFullyQualifiedNameWithoutNullable()
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Program {
		public int? Value { get; set; }
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = compilation.GetTypeByMetadataName("Test.Program")!;
		var propertySymbol = typeSymbol
			.GetMembers("Value")
			.OfType<IPropertySymbol>()
			.First();

		// Act
		var result = SymbolHelpers.GetFullyQualifiedName(propertySymbol.Type);

		// Assert
		await Assert.That(result).IsEqualTo("System.Int32");
	}

	[Test]
	public async Task GetFullyQualifiedName_GivenNestedGenericType_ReturnsCorrectFormat()
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Outer<T> {
		public class Inner<U> {
		}
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = compilation.GetTypeByMetadataName("Test.Outer`1+Inner`1")!;

		// Act
		var result = SymbolHelpers.GetFullyQualifiedName(typeSymbol);

		// Assert
		// SymbolHelpers strips generics completely per its documentation
		await Assert.That(result).Contains("Test.Outer");
		await Assert.That(result).Contains("Inner");
	}

	[Test]
	public async Task GetFullyQualifiedName_GivenReferenceTypeWithNullableAnnotation_StripsnullableAnnotation()
	{
		// Arrange
		const string source =
			@"
#nullable enable
namespace Test {
	public class Program {
		public string? Value { get; set; }
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = compilation.GetTypeByMetadataName("Test.Program")!;
		var propertySymbol = typeSymbol
			.GetMembers("Value")
			.OfType<IPropertySymbol>()
			.First();

		// Act
		var result = SymbolHelpers.GetFullyQualifiedName(propertySymbol.Type);

		// Assert
		await Assert.That(result).IsEqualTo("System.String");
		await Assert.That(result).DoesNotContain("?");
	}
}
