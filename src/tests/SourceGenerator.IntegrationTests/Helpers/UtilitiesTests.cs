namespace Purview.Telemetry.SourceGenerator.Helpers;

public class UtilitiesTests
{
	[Test]
	public async Task WithComma_GivenString_AddsComma()
	{
		// Arrange
		const string input = "test";

		// Act
		var result = input.WithComma();

		// Assert
		await Assert.That(result).IsEqualTo("test, ");
	}

	[Test]
	public async Task WithComma_GivenStringWithoutSpace_AddsCommaOnly()
	{
		// Arrange
		const string input = "test";

		// Act
		var result = input.WithComma(andSpace: false);

		// Assert
		await Assert.That(result).IsEqualTo("test,");
	}

	[Test]
	public async Task Wrap_GivenString_WrapsWithDefaultQuotes()
	{
		// Arrange
		const string input = "test";

		// Act
		var result = input.Wrap();

		// Assert
		await Assert.That(result).IsEqualTo("\"test\"");
	}

	[Test]
	public async Task Wrap_GivenStringWithCustomChar_WrapsWithCustomChar()
	{
		// Arrange
		const string input = "test";

		// Act
		var result = input.Wrap('\'');

		// Assert
		await Assert.That(result).IsEqualTo("'test'");
	}

	[Test]
	public async Task LowercaseFirstChar_GivenCapitalizedString_LowercasesFirstChar()
	{
		// Arrange
		const string input = "TestString";

		// Act
		var result = Utilities.LowercaseFirstChar(input);

		// Assert
		await Assert.That(result).IsEqualTo("testString");
	}

	[Test]
	public async Task LowercaseFirstChar_GivenEmptyString_ReturnsEmpty()
	{
		// Arrange
		const string input = "";

		// Act
		var result = Utilities.LowercaseFirstChar(input);

		// Assert
		await Assert.That(result).IsEqualTo("");
	}

	[Test]
	public async Task LowercaseFirstChar_GivenSingleChar_LowercasesThatChar()
	{
		// Arrange
		const string input = "T";

		// Act
		var result = Utilities.LowercaseFirstChar(input);

		// Assert
		await Assert.That(result).IsEqualTo("t");
	}

	[Test]
	public async Task UppercaseFirstChar_GivenLowercaseString_UppercasesFirstChar()
	{
		// Arrange
		const string input = "testString";

		// Act
		var result = Utilities.UppercaseFirstChar(input);

		// Assert
		await Assert.That(result).IsEqualTo("TestString");
	}

	[Test]
	public async Task UppercaseFirstChar_GivenEmptyString_ReturnsEmpty()
	{
		// Arrange
		const string input = "";

		// Act
		var result = Utilities.UppercaseFirstChar(input);

		// Assert
		await Assert.That(result).IsEqualTo("");
	}

	[Test]
	public async Task UppercaseFirstChar_GivenSingleChar_UppercasesThatChar()
	{
		// Arrange
		const string input = "t";

		// Act
		var result = Utilities.UppercaseFirstChar(input);

		// Assert
		await Assert.That(result).IsEqualTo("T");
	}

	[Test]
	public async Task Flatten_GivenStringWithMultipleSpaces_CollapsesToSingleSpace()
	{
		// Arrange
		const string input = "test    string   with     spaces";

		// Act
		var result = input.Flatten();

		// Assert
		await Assert.That(result).IsEqualTo("test string with spaces");
	}

	[Test]
	public async Task Flatten_GivenStringWithTabs_CollapsesToSingleSpace()
	{
		// Arrange
		const string input = "test\t\tstring\twith\t\t\ttabs";

		// Act
		var result = input.Flatten();

		// Assert
		await Assert.That(result).IsEqualTo("test string with tabs");
	}

	[Test]
	public async Task Flatten_GivenStringWithNewlines_CollapsesToSingleSpace()
	{
		// Arrange
		const string input = "test\n\nstring\nwith\n\n\nnewlines";

		// Act
		var result = input.Flatten();

		// Assert
		await Assert.That(result).IsEqualTo("test string with newlines");
	}

	[Test]
	public async Task ConvertToSeparatedLowercase_GivenPascalCase_ConvertsToDotSeparated()
	{
		// Arrange
		const string input = "EntityId";

		// Act
		var result = Utilities.ConvertToSeparatedLowercase(input);

		// Assert
		await Assert.That(result).IsEqualTo("entity.id");
	}

	[Test]
	public async Task ConvertToSeparatedLowercase_GivenCamelCase_ConvertsToDotSeparated()
	{
		// Arrange
		const string input = "entityId";

		// Act
		var result = Utilities.ConvertToSeparatedLowercase(input);

		// Assert
		await Assert.That(result).IsEqualTo("entity.id");
	}

	[Test]
	public async Task ConvertToSeparatedLowercase_GivenAcronym_HandlesCorrectly()
	{
		// Arrange
		const string input = "HTTPSConnection";

		// Act
		var result = Utilities.ConvertToSeparatedLowercase(input);

		// Assert
		await Assert.That(result).IsEqualTo("https.connection");
	}

	[Test]
	public async Task ConvertToSeparatedLowercase_GivenMultipleWords_ConvertsToDotSeparated()
	{
		// Arrange
		const string input = "CustomerFirstNameValue";

		// Act
		var result = Utilities.ConvertToSeparatedLowercase(input);

		// Assert
		await Assert.That(result).IsEqualTo("customer.first.name.value");
	}

	[Test]
	public async Task ConvertToSeparatedLowercase_GivenUnderscoreSeparator_ConvertsToUnderscoreSeparated()
	{
		// Arrange
		const string input = "EntityId";

		// Act
		var result = Utilities.ConvertToSeparatedLowercase(input, '_');

		// Assert
		await Assert.That(result).IsEqualTo("entity_id");
	}

	[Test]
	public async Task ConvertToSeparatedLowercase_GivenEmptyString_ReturnsEmpty()
	{
		// Arrange
		const string input = "";

		// Act
		var result = Utilities.ConvertToSeparatedLowercase(input);

		// Assert
		await Assert.That(result).IsEqualTo("");
	}

	[Test]
	public async Task ConvertToSeparatedLowercase_GivenSingleLowercaseChar_ReturnsSameChar()
	{
		// Arrange
		const string input = "a";

		// Act
		var result = Utilities.ConvertToSeparatedLowercase(input);

		// Assert
		await Assert.That(result).IsEqualTo("a");
	}

	[Test]
	public async Task ConvertToSeparatedLowercase_GivenSingleUppercaseChar_ReturnsLowercase()
	{
		// Arrange
		const string input = "A";

		// Act
		var result = Utilities.ConvertToSeparatedLowercase(input);

		// Assert
		await Assert.That(result).IsEqualTo("a");
	}
}
