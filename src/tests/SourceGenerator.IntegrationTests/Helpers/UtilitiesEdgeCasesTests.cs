namespace Purview.Telemetry.SourceGenerator.Helpers;

public class UtilitiesEdgeCasesTests
{
	[Test]
	public async Task ConvertToSeparatedLowercase_GivenAllUppercase_ConvertsCorrectly()
	{
		// Arrange
		const string input = "HTTP";

		// Act
		var result = Utilities.ConvertToSeparatedLowercase(input);

		// Assert
		await Assert.That(result).IsEqualTo("http");
	}

	[Test]
	public async Task ConvertToSeparatedLowercase_GivenAllLowercase_ReturnsAsIs()
	{
		// Arrange
		const string input = "test";

		// Act
		var result = Utilities.ConvertToSeparatedLowercase(input);

		// Assert
		await Assert.That(result).IsEqualTo("test");
	}

	[Test]
	public async Task ConvertToSeparatedLowercase_GivenMixedWithNumbers_HandlesNumbers()
	{
		// Arrange
		const string input = "Test123Value";

		// Act
		var result = Utilities.ConvertToSeparatedLowercase(input);

		// Assert
		await Assert.That(result).Contains("test");
		await Assert.That(result).Contains("123");
		await Assert.That(result).Contains("value");
	}

	[Test]
	public async Task ConvertToSeparatedLowercase_GivenSingleUppercaseLetter_ReturnsLowercase()
	{
		// Arrange
		const string input = "X";

		// Act
		var result = Utilities.ConvertToSeparatedLowercase(input);

		// Assert
		await Assert.That(result).IsEqualTo("x");
	}

	[Test]
	public async Task Flatten_GivenMixedWhitespace_CollapsesAll()
	{
		// Arrange
		const string input = "test \t\n\r string";

		// Act
		var result = input.Flatten();

		// Assert
		await Assert.That(result).IsEqualTo("test string");
	}

	[Test]
	public async Task Flatten_GivenOnlyWhitespace_ReturnsSpace()
	{
		// Arrange
		const string input = "   \t\n\r   ";

		// Act
		var result = input.Flatten();

		// Assert
		await Assert.That(result).IsEqualTo(" ");
	}

	[Test]
	public async Task Flatten_GivenNoWhitespace_ReturnsAsIs()
	{
		// Arrange
		const string input = "test";

		// Act
		var result = input.Flatten();

		// Assert
		await Assert.That(result).IsEqualTo("test");
	}

	[Test]
	public async Task ConvertToSeparatedLowercase_GivenUnicodeCharacters_HandlesCorrectly()
	{
		// Arrange
		const string input = "TestÄöüValue";

		// Act
		var result = Utilities.ConvertToSeparatedLowercase(input);

		// Assert
		await Assert.That(result).Contains("test");
		await Assert.That(result).Contains("value");
	}

	[Test]
	public async Task Wrap_GivenEmptyString_WrapsEmpty()
	{
		// Arrange
		const string input = "";

		// Act
		var result = input.Wrap();

		// Assert
		await Assert.That(result).IsEqualTo("\"\"");
	}

	[Test]
	public async Task WithComma_GivenEmptyString_AddsComma()
	{
		// Arrange
		const string input = "";

		// Act
		var result = input.WithComma();

		// Assert
		await Assert.That(result).IsEqualTo(", ");
	}

	[Test]
	public async Task ConvertToSeparatedLowercase_GivenVeryLongString_Completes()
	{
		// Arrange
		var input = string.Concat(Enumerable.Repeat("TestValue", 100));

		// Act
		var result = Utilities.ConvertToSeparatedLowercase(input);

		// Assert
		await Assert.That(result).IsNotNull();
		await Assert.That(result.Length).IsGreaterThan(0);
	}

	[Test]
	public async Task Flatten_GivenVeryLongString_Completes()
	{
		// Arrange
		var input = string.Concat(Enumerable.Repeat("test   ", 1000));

		// Act
		var result = input.Flatten();

		// Assert
		await Assert.That(result).IsNotNull();
		await Assert.That(result).DoesNotContain("   ");
	}
}
