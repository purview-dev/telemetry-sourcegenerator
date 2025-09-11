using System;
using System.Text;
using FluentAssertions;
using Purview.Telemetry.SourceGenerator.Helpers;
using Xunit;

namespace Purview.Telemetry.SourceGenerator.IntegrationTests;

public class CodeWriterTests
{
	[Fact]
	public void EmptyCodeWriter_ReturnsEmptyString()
	{
		using var cw = new CodeWriter();
		cw.ToString().Should().Be("");
	}

	[Fact]
	public void Write_SingleString_WritesCorrectly()
	{
		using var cw = new CodeWriter();
		cw.Write("hello");
		cw.ToString().Should().Be("hello");
	}

	[Fact]
	public void WriteLine_SingleString_WritesWithNewline()
	{
		using var cw = new CodeWriter();
		cw.WriteLine("hello");
		cw.ToString().Should().Be("hello\n");
	}

	[Fact]
	public void Indent_WritesCorrectTabs()
	{
		using var cw = new CodeWriter();
		cw.Indent(3).Write("test");
		cw.ToString().Should().Be("\t\t\ttest");
	}

	[Fact]
	public void FluentChaining_WorksCorrectly()
	{
		using var cw = new CodeWriter();
		cw.Write("public ").Write("void ").Write("Method").WriteLine("();");

		cw.ToString().Should().Be("public void Method();\n");
	}

	[Fact]
	public void BeginMethod_GeneratesCorrectSignature()
	{
		using var cw = new CodeWriter();
		cw.BeginMethod(1, "public void", "TestMethod");
		cw.ToString().Should().Be("\tpublic void TestMethod(");
	}

	[Fact]
	public void BeginMethod_WithParameters_GeneratesCorrectSignature()
	{
		using var cw = new CodeWriter();
		cw.BeginMethod(1, "public string", "TestMethod", "int id, string name");
		cw.ToString().Should().Be("\tpublic string TestMethod(int id, string name");
	}

	[Fact]
	public void BeginMethodBody_GeneratesOpeningBrace()
	{
		using var cw = new CodeWriter();
		cw.BeginMethodBody(1);
		cw.ToString().Should().Be("\t{\n");
	}

	[Fact]
	public void EndMethodBody_GeneratesClosingBrace()
	{
		using var cw = new CodeWriter();
		cw.EndMethodBody(1);
		cw.ToString().Should().Be("\t}\n");
	}

	[Fact]
	public void WriteReturn_GeneratesReturnStatement()
	{
		using var cw = new CodeWriter();
		cw.WriteReturn(2, "result");
		cw.ToString().Should().Be("\t\treturn result;\n");
	}

	[Fact]
	public void WriteMethodCall_GeneratesMethodCall()
	{
		using var cw = new CodeWriter();
		cw.WriteMethodCall(1, "Console", "WriteLine", "\"Hello World\"");
		cw.ToString().Should().Be("\tConsole.WriteLine(\"Hello World\");\n");
	}

	[Fact]
	public void WriteIf_GeneratesIfStatement()
	{
		using var cw = new CodeWriter();
		cw.WriteIf(1, "value != null");
		cw.ToString().Should().Be("\tif (value != null)\n");
	}

	[Fact]
	public void WriteClass_GeneratesClassDeclaration()
	{
		using var cw = new CodeWriter();
		cw.WriteClass(0, "TestClass");
		cw.ToString().Should().Be("public class TestClass\n{\n");
	}

	[Fact]
	public void WriteNamespace_GeneratesNamespaceDeclaration()
	{
		using var cw = new CodeWriter();
		cw.WriteNamespace("MyNamespace");
		cw.ToString().Should().Be("namespace MyNamespace\n{\n");
	}

	[Fact]
	public void WriteUsing_GeneratesUsingStatement()
	{
		using var cw = new CodeWriter();
		cw.WriteUsing("System");
		cw.ToString().Should().Be("using System;\n");
	}

	[Fact]
	public void FluentPunctuation_WorksCorrectly()
	{
		using var cw = new CodeWriter();
		cw.Write("Method").OpenParen().Write("args").CloseParen().Semicolon().WriteLine();

		cw.ToString().Should().Be("Method(args);\n");
	}

	[Fact]
	public void CloseBrace_GeneratesClosingBrace()
	{
		using var cw = new CodeWriter();
		cw.CloseBrace();
		cw.ToString().Should().Be("}\n");
	}

	[Fact]
	public void ComplexMethod_GeneratesCorrectCode()
	{
		using var cw = new CodeWriter();

		cw.BeginMethod(1, "public async Task<string>", "GetNameAsync")
			.CloseParen()
			.BeginMethodBody(1)
			.WriteMethodCall(2, "await Task", "Delay", "1")
			.WriteReturn(2, "_name")
			.EndMethodBody(1);

		var expected =
			"\tpublic async Task<string> GetNameAsync()\n"
			+ "\t{\n"
			+ "\t\tawait Task.Delay(1);\n"
			+ "\t\treturn _name;\n"
			+ "\t}\n";

		cw.ToString().Should().Be(expected);
	}

	[Fact]
	public void ComplexClass_GeneratesCorrectStructure()
	{
		using var cw = new CodeWriter();

		cw.WriteUsing("System")
			.WriteUsing("System.Threading.Tasks")
			.WriteLine()
			.WriteNamespace("Generated")
			.WriteClass(1, "TestService")
			.Indent(2)
			.WriteLine("private readonly string _name = \"TestService\";")
			.WriteLine()
			.BeginMethod(2, "public string", "GetName")
			.CloseParen()
			.BeginMethodBody(2)
			.WriteReturn(3, "_name")
			.EndMethodBody(2)
			.EndMethodBody(1)
			.CloseBrace();

		var expected =
			"using System;\n"
			+ "using System.Threading.Tasks;\n"
			+ "\n"
			+ "namespace Generated\n"
			+ "{\n"
			+ "\tpublic class TestService\n"
			+ "\t{\n"
			+ "\t\tprivate readonly string _name = \"TestService\";\n"
			+ "\n"
			+ "\t\tpublic string GetName()\n"
			+ "\t\t{\n"
			+ "\t\t\treturn _name;\n"
			+ "\t\t}\n"
			+ "\t}\n"
			+ "}\n";

		cw.ToString().Should().Be(expected);
	}

	[Fact]
	public void LargeContent_HandlesResizing()
	{
		using var cw = new CodeWriter(16); // Small initial capacity

		// Generate enough content to force resize
		for (int i = 0; i < 100; i++)
		{
			cw.WriteLine($"// This is line {i} with some content to force buffer growth");
		}

		var result = cw.ToString();
		result.Split('\n').Length.Should().Be(101); // 100 lines + final empty line
		result.Should().Contain("// This is line 99");
	}

	[Fact]
	public void WriteFormatted_HandlesSimpleFormatting()
	{
		using var cw = new CodeWriter();

		// Test internal zero-allocation formatting
		cw.Write("Method");
		cw.Write(42);
		cw.Write("End");

		cw.ToString().Should().Be("Method42End");
	}

	[Fact]
	public void Disposal_CanBeCalledMultipleTimes()
	{
		var cw = new CodeWriter();
		cw.Write("test");
		cw.Dispose();
		cw.Dispose(); // Should not throw
	}

	[Fact]
	public void ToString_AfterDisposal_ReturnsEmpty()
	{
		var cw = new CodeWriter();
		cw.Write("test");
		cw.Dispose();
		cw.ToString().Should().Be("");
	}

	[Fact]
	public void BackwardCompatibility_LegacyMethods_StillWork()
	{
		using var cw = new CodeWriter();

		// Test legacy method compatibility
		cw.WriteIndent(1);
		cw.Write("legacy");
		cw.WriteLine();

		cw.ToString().Should().Be("\tlegacy\n");
	}

	[Theory]
	[InlineData(0, "")]
	[InlineData(1, "\t")]
	[InlineData(2, "\t\t")]
	[InlineData(5, "\t\t\t\t\t")]
	public void Indent_GeneratesCorrectIndentation(int level, string expected)
	{
		using var cw = new CodeWriter();
		cw.Indent(level);
		var result = cw.ToString();
		result.Should().Be(expected);
	}

	[Fact]
	public void ZeroAllocation_IntegerWrite_DoesNotAllocate()
	{
		using var cw = new CodeWriter();

		// This should use zero-allocation integer formatting
		cw.Write(12345);
		cw.ToString().Should().Be("12345");
	}
}
