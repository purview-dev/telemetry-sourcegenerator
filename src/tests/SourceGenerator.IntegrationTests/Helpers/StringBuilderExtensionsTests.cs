using System.Text;

namespace Purview.Telemetry.SourceGenerator.Helpers;

public class StringBuilderExtensionsTests
{
	[Test]
	public async Task WithIndent_GivenZero_ReturnsNoIndent()
	{
		// Arrange
		var builder = new StringBuilder();

		// Act
		builder.WithIndent(0);
		var result = builder.ToString();

		// Assert
		await Assert.That(result).IsEqualTo("");
	}

	[Test]
	public async Task WithIndent_GivenOne_ReturnsOneTab()
	{
		// Arrange
		var builder = new StringBuilder();

		// Act
		builder.WithIndent(1);
		var result = builder.ToString();

		// Assert
		await Assert.That(result).IsEqualTo("\t");
	}

	[Test]
	public async Task WithIndent_GivenFive_ReturnsFiveTabs()
	{
		// Arrange
		var builder = new StringBuilder();

		// Act
		builder.WithIndent(5);
		var result = builder.ToString();

		// Assert
		await Assert.That(result).IsEqualTo("\t\t\t\t\t");
	}

	[Test]
	public async Task WithIndent_GivenEight_ReturnsEightTabs()
	{
		// Arrange
		var builder = new StringBuilder();

		// Act
		builder.WithIndent(8);
		var result = builder.ToString();

		// Assert
		await Assert.That(result).IsEqualTo("\t\t\t\t\t\t\t\t");
	}

	[Test]
	public async Task WithIndent_GivenTen_ReturnsTenTabs()
	{
		// Arrange
		var builder = new StringBuilder();

		// Act
		builder.WithIndent(10);
		var result = builder.ToString();

		// Assert
		await Assert.That(result).IsEqualTo("\t\t\t\t\t\t\t\t\t\t");
	}

	[Test]
	public async Task WithIndent_GivenLargeNumber_ReturnsCorrectTabs()
	{
		// Arrange
		var builder = new StringBuilder();

		// Act
		builder.WithIndent(15);
		var result = builder.ToString();

		// Assert
		await Assert.That(result.Length).IsEqualTo(15);
		await Assert.That(result).IsEqualTo("\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t");
	}

	[Test]
	public async Task Append_GivenTabsAndString_AppendsWithIndent()
	{
		// Arrange
		var builder = new StringBuilder();

		// Act
		builder.Append(2, "test");
		var result = builder.ToString();

		// Assert
		await Assert.That(result).Contains("\t\ttest");
		await Assert.That(result.TrimEnd()).IsEqualTo("\t\ttest");
	}

	[Test]
	public async Task Append_GivenTabsAndStringWithoutNewLine_AppendsWithoutNewLine()
	{
		// Arrange
		var builder = new StringBuilder();

		// Act
		builder.Append(2, "test", withNewLine: false);
		var result = builder.ToString();

		// Assert
		await Assert.That(result).IsEqualTo("\t\ttest");
	}

	[Test]
	public async Task Append_GivenTabsAndChar_AppendsWithIndent()
	{
		// Arrange
		var builder = new StringBuilder();

		// Act
		builder.Append(1, 'x');
		var result = builder.ToString();

		// Assert
		await Assert.That(result).Contains("\tx");
		await Assert.That(result.TrimEnd()).IsEqualTo("\tx");
	}

	[Test]
	public async Task Append_GivenTabsAndCharWithoutNewLine_AppendsWithoutNewLine()
	{
		// Arrange
		var builder = new StringBuilder();

		// Act
		builder.Append(1, 'x', withNewLine: false);
		var result = builder.ToString();

		// Assert
		await Assert.That(result).IsEqualTo("\tx");
	}

	[Test]
	public async Task AppendLine_GivenChar_AppendsCharWithNewLine()
	{
		// Arrange
		var builder = new StringBuilder();

		// Act
		builder.AppendLine('x');
		var result = builder.ToString();

		// Assert
		await Assert.That(result).Contains("x");
		await Assert.That(result.TrimEnd()).IsEqualTo("x");
	}

	[Test]
	public async Task AggressiveInlining_GivenIndent_AppendsAttribute()
	{
		// Arrange
		var builder = new StringBuilder();

		// Act
		builder.AggressiveInlining(1);
		var result = builder.ToString();

		// Assert
		await Assert.That(result).Contains("MethodImpl");
		await Assert.That(result).Contains("AggressiveInlining");
	}

	[Test]
	public async Task CodeGen_GivenIndent_AppendsAttribute()
	{
		// Arrange
		var builder = new StringBuilder();

		// Act
		builder.CodeGen(1);
		var result = builder.ToString();

		// Assert
		await Assert.That(result).Contains("GeneratedCode");
	}

	[Test]
	public async Task IfDefines_GivenCondition_AddsIfDefine()
	{
		// Arrange
		var builder = new StringBuilder();

		// Act
		builder.IfDefines("DEBUG", "test content");
		var result = builder.ToString();

		// Assert
		await Assert.That(result).Contains("#if DEBUG");
		await Assert.That(result).Contains("#endif");
		await Assert.That(result).Contains("test content");
	}

	[Test]
	public async Task IfDefines_GivenConditionWithIndent_AddsIfDefineWithIndent()
	{
		// Arrange
		var builder = new StringBuilder();

		// Act
		builder.IfDefines("DEBUG", 2, "test content");
		var result = builder.ToString();

		// Assert
		await Assert.That(result).Contains("#if DEBUG");
		await Assert.That(result).Contains("#endif");
		await Assert.That(result).Contains("test content");
	}

	[Test]
	public async Task IfDefines_GivenMultipleValues_AddsAllValues()
	{
		// Arrange
		var builder = new StringBuilder();

		// Act
		builder.IfDefines("DEBUG", "value1", "value2", "value3");
		var result = builder.ToString();

		// Assert
		await Assert.That(result).Contains("#if DEBUG");
		await Assert.That(result).Contains("#endif");
		await Assert.That(result).Contains("value1");
		await Assert.That(result).Contains("value2");
		await Assert.That(result).Contains("value3");
	}

	[Test]
	public async Task Chaining_MultipleOperations_WorksCorrectly()
	{
		// Arrange
		var builder = new StringBuilder();

		// Act
		builder.WithIndent(1).Append("test").WithIndent(2).Append("another");
		var result = builder.ToString();

		// Assert
		await Assert.That(result).IsEqualTo("\ttest\t\tanother");
	}
}
