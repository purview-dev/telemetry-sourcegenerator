using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Purview.Telemetry.SourceGenerator.Helpers;

namespace Purview.Telemetry.SourceGenerator.Tests.Helpers;

public class UtilitiesTypeCheckingTests
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

	static ITypeSymbol GetPropertyType(Compilation compilation, string typeName, string propertyName)
	{
		var typeSymbol = compilation.GetTypeByMetadataName(typeName)!;
		var propertySymbol = typeSymbol
			.GetMembers(propertyName)
			.OfType<IPropertySymbol>()
			.First();
		return propertySymbol.Type;
	}

	[Test]
	public async Task IsComplexType_GivenClass_ReturnsTrue(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Customer {
		public string Name { get; set; }
	}
	public class Program {
		public Customer Customer { get; set; }
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = GetPropertyType(compilation, "Test.Program", "Customer");

		// Act
		var result = typeSymbol.IsComplexType();

		// Assert
		await Assert.That(result).IsTrue();
	}

	[Test]
	public async Task IsComplexType_GivenStruct_ReturnsTrue(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public struct CustomStruct {
		public int Value { get; set; }
	}
	public class Program {
		public CustomStruct Struct { get; set; }
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = GetPropertyType(compilation, "Test.Program", "Struct");

		// Act
		var result = typeSymbol.IsComplexType();

		// Assert
		await Assert.That(result).IsTrue();
	}

	[Test]
	public async Task IsComplexType_GivenString_ReturnsFalse(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Program {
		public string Name { get; set; }
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = GetPropertyType(compilation, "Test.Program", "Name");

		// Act
		var result = typeSymbol.IsComplexType();

		// Assert
		await Assert.That(result).IsFalse();
	}

	[Test]
	public async Task IsComplexType_GivenInt_ReturnsFalse(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Program {
		public int Value { get; set; }
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = GetPropertyType(compilation, "Test.Program", "Value");

		// Act
		var result = typeSymbol.IsComplexType();

		// Assert
		await Assert.That(result).IsFalse();
	}

	[Test]
	public async Task IsArray_GivenIntArray_ReturnsTrue(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Program {
		public int[] Numbers { get; set; }
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = GetPropertyType(compilation, "Test.Program", "Numbers");

		// Act
		var result = typeSymbol.IsArray();

		// Assert
		await Assert.That(result).IsTrue();
	}

	[Test]
	public async Task IsArray_GivenString_ReturnsFalse(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Program {
		public string Name { get; set; }
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = GetPropertyType(compilation, "Test.Program", "Name");

		// Act
		var result = typeSymbol.IsArray();

		// Assert
		await Assert.That(result).IsFalse();
	}

	[Test]
	public async Task IsArray_GivenList_ReturnsFalse(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
using System.Collections.Generic;
namespace Test {
	public class Program {
		public List<int> Numbers { get; set; }
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = GetPropertyType(compilation, "Test.Program", "Numbers");

		// Act
		var result = typeSymbol.IsArray();

		// Assert
		await Assert.That(result).IsFalse();
	}

	[Test]
	public async Task IsIEnumerable_GivenString_ReturnsFalse(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Program {
		public string Name { get; set; }
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = GetPropertyType(compilation, "Test.Program", "Name");

		// Act
		var result = typeSymbol.IsIEnumerable(compilation);

		// Assert
		await Assert.That(result).IsFalse();
	}

	[Test]
	public async Task IsIEnumerable_GivenInt_ReturnsFalse(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Program {
		public int Value { get; set; }
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = GetPropertyType(compilation, "Test.Program", "Value");

		// Act
		var result = typeSymbol.IsIEnumerable(compilation);

		// Assert
		await Assert.That(result).IsFalse();
	}

	[Test]
	public async Task IsExceptionType_GivenException_ReturnsTrue(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
using System;
namespace Test {
	public class Program {
		public Exception Error { get; set; }
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = GetPropertyType(compilation, "Test.Program", "Error");

		// Act
		var result = typeSymbol.IsExceptionType();

		// Assert
		await Assert.That(result).IsTrue();
	}

	[Test]
	public async Task IsExceptionType_GivenCustomException_ReturnsTrue(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
using System;
namespace Test {
	public class CustomException : Exception {
	}
	public class Program {
		public CustomException Error { get; set; }
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = GetPropertyType(compilation, "Test.Program", "Error");

		// Act
		var result = typeSymbol.IsExceptionType();

		// Assert
		await Assert.That(result).IsTrue();
	}

	[Test]
	public async Task IsExceptionType_GivenDerivedException_ReturnsTrue(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
using System;
namespace Test {
	public class ArgumentException : Exception {
	}
	public class Program {
		public ArgumentException Error { get; set; }
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = GetPropertyType(compilation, "Test.Program", "Error");

		// Act
		var result = typeSymbol.IsExceptionType();

		// Assert
		await Assert.That(result).IsTrue();
	}

	[Test]
	public async Task IsExceptionType_GivenString_ReturnsFalse(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Program {
		public string Name { get; set; }
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = GetPropertyType(compilation, "Test.Program", "Name");

		// Act
		var result = typeSymbol.IsExceptionType();

		// Assert
		await Assert.That(result).IsFalse();
	}

	[Test]
	public async Task IsExceptionType_GivenCustomClass_ReturnsFalse(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Customer {
		public string Name { get; set; }
	}
	public class Program {
		public Customer Customer { get; set; }
	}
}";
		var compilation = CreateCompilation(source);
		var typeSymbol = GetPropertyType(compilation, "Test.Program", "Customer");

		// Act
		var result = typeSymbol.IsExceptionType();

		// Assert
		await Assert.That(result).IsFalse();
	}

}
