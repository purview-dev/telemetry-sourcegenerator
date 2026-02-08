using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Telemetry.SourceGenerator.Helpers;

namespace Purview.Telemetry.SourceGenerator.Tests.Helpers;

public class UtilitiesNamespaceTests
{
	static TypeDeclarationSyntax GetTypeDeclaration(string source, string typeName)
	{
		var tree = CSharpSyntaxTree.ParseText(source);
		var root = tree.GetRoot();
		return root
			.DescendantNodes()
			.OfType<TypeDeclarationSyntax>()
			.First(t => t.Identifier.Text == typeName);
	}

	[Test]
	public async Task GetNamespace_GivenSimpleNamespace_ReturnsNamespace(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Program {
	}
}";
		var typeDeclaration = GetTypeDeclaration(source, "Program");

		// Act
		var result = Utilities.GetNamespace(typeDeclaration);

		// Assert
		await Assert.That(result).IsEqualTo("Test");
	}

	[Test]
	public async Task GetNamespace_GivenNestedNamespace_ReturnsFullNamespace(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test.Services {
	public class CustomerService {
	}
}";
		var typeDeclaration = GetTypeDeclaration(source, "CustomerService");

		// Act
		var result = Utilities.GetNamespace(typeDeclaration);

		// Assert
		await Assert.That(result).IsEqualTo("Test.Services");
	}

	[Test]
	public async Task GetNamespace_GivenFileScopedNamespace_ReturnsNamespace(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test.Services;

public class CustomerService {
}";
		var typeDeclaration = GetTypeDeclaration(source, "CustomerService");

		// Act
		var result = Utilities.GetNamespace(typeDeclaration);

		// Assert
		await Assert.That(result).IsEqualTo("Test.Services");
	}

	[Test]
	public async Task GetNamespace_GivenGlobalNamespace_ReturnsNull(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
public class Program {
}";
		var typeDeclaration = GetTypeDeclaration(source, "Program");

		// Act
		var result = Utilities.GetNamespace(typeDeclaration);

		// Assert
		await Assert.That(result).IsNull();
	}

	[Test]
	public async Task GetNamespace_GivenNestedClass_ReturnsNamespace(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Outer {
		public class Inner {
		}
	}
}";
		var typeDeclaration = GetTypeDeclaration(source, "Inner");

		// Act
		var result = Utilities.GetNamespace(typeDeclaration);

		// Assert
		await Assert.That(result).IsEqualTo("Test");
	}

	[Test]
	public async Task GetParentClasses_GivenTopLevelClass_ReturnsEmpty(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Program {
	}
}";
		var typeDeclaration = GetTypeDeclaration(source, "Program");

		// Act
		var result = Utilities.GetParentClasses(typeDeclaration);

		// Assert
		await Assert.That(result).IsEmpty();
	}

	[Test]
	public async Task GetParentClasses_GivenNestedClass_ReturnsParent(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Outer {
		public class Inner {
		}
	}
}";
		var typeDeclaration = GetTypeDeclaration(source, "Inner");

		// Act
		var result = Utilities.GetParentClasses(typeDeclaration);

		// Assert
		await Assert.That(result).Count().IsEqualTo(1);
		await Assert.That(result[0]).IsEqualTo("Outer");
	}

	[Test]
	public async Task GetParentClasses_GivenDeeplyNestedClass_ReturnsAllParents(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Level1 {
		public class Level2 {
			public class Level3 {
			}
		}
	}
}";
		var typeDeclaration = GetTypeDeclaration(source, "Level3");

		// Act
		var result = Utilities.GetParentClasses(typeDeclaration);

		// Assert
		await Assert.That(result).Count().IsEqualTo(2);
		await Assert.That(result[0]).IsEqualTo("Level2");
		await Assert.That(result[1]).IsEqualTo("Level1");
	}

	[Test]
	public async Task GetParentClassesAsNamespace_GivenTopLevelClass_ReturnsNull(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Program {
	}
}";
		var typeDeclaration = GetTypeDeclaration(source, "Program");

		// Act
		var result = Utilities.GetParentClassesAsNamespace(typeDeclaration);

		// Assert
		await Assert.That(result).IsNull();
	}

	[Test]
	public async Task GetParentClassesAsNamespace_GivenNestedClass_ReturnsParentAsNamespace(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Outer {
		public class Inner {
		}
	}
}";
		var typeDeclaration = GetTypeDeclaration(source, "Inner");

		// Act
		var result = Utilities.GetParentClassesAsNamespace(typeDeclaration);

		// Assert
		await Assert.That(result).IsEqualTo("Outer");
	}

	[Test]
	public async Task GetParentClassesAsNamespace_GivenDeeplyNestedClass_ReturnsParentsAsNamespace(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Level1 {
		public class Level2 {
			public class Level3 {
			}
		}
	}
}";
		var typeDeclaration = GetTypeDeclaration(source, "Level3");

		// Act
		var result = Utilities.GetParentClassesAsNamespace(typeDeclaration);

		// Assert
		await Assert.That(result).IsEqualTo("Level1.Level2");
	}

	[Test]
	public async Task GetFullNamespace_GivenSimpleClass_ReturnsNull(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Program {
	}
}";
		var typeDeclaration = GetTypeDeclaration(source, "Program");

		// Act
		var result = Utilities.GetFullNamespace(typeDeclaration, includeTrailingSeparator: false);

		// Assert
		await Assert.That(result).IsEqualTo("Test");
	}

	[Test]
	public async Task GetFullNamespace_GivenNestedClass_ReturnsFullNamespace(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Outer {
		public class Inner {
		}
	}
}";
		var typeDeclaration = GetTypeDeclaration(source, "Inner");

		// Act
		var result = Utilities.GetFullNamespace(typeDeclaration, includeTrailingSeparator: false);

		// Assert
		await Assert.That(result).IsEqualTo("Test.Outer");
	}

	[Test]
	public async Task GetFullNamespace_GivenNestedClassWithTrailingSeparator_ReturnsFullNamespaceWithDot(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test {
	public class Outer {
		public class Inner {
		}
	}
}";
		var typeDeclaration = GetTypeDeclaration(source, "Inner");

		// Act
		var result = Utilities.GetFullNamespace(typeDeclaration, includeTrailingSeparator: true);

		// Assert
		await Assert.That(result).IsEqualTo("Test.Outer.");
	}

	[Test]
	public async Task GetFullNamespace_GivenGlobalClass_ReturnsNull(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
public class Program {
}";
		var typeDeclaration = GetTypeDeclaration(source, "Program");

		// Act
		var result = Utilities.GetFullNamespace(typeDeclaration, includeTrailingSeparator: false);

		// Assert
		await Assert.That(result).IsNull();
	}

	[Test]
	public async Task GetFullNamespace_GivenDeeplyNestedClass_ReturnsCompleteNamespace(CancellationToken cancellationToken)
	{
		// Arrange
		const string source =
			@"
namespace Test.Services {
	public class Level1 {
		public class Level2 {
			public class Level3 {
			}
		}
	}
}";
		var typeDeclaration = GetTypeDeclaration(source, "Level3");

		// Act
		var result = Utilities.GetFullNamespace(typeDeclaration, includeTrailingSeparator: false);

		// Assert
		await Assert.That(result).IsEqualTo("Test.Services.Level1.Level2");
	}

}
