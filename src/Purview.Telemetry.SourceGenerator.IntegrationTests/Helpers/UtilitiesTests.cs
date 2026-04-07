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

	[Test]
	public async Task IsLikelyCompoundWord_GivenEntityId_ReturnsTrue()
	{
		// Arrange
		const string input = "entityid";

		// Act
		var result = Utilities.IsLikelyCompoundWord(input);

		// Assert
		await Assert.That(result).IsTrue();
	}

	[Test]
	public async Task IsLikelyCompoundWord_GivenRequestCount_ReturnsTrue()
	{
		// Arrange
		const string input = "requestcount";

		// Act
		var result = Utilities.IsLikelyCompoundWord(input);

		// Assert
		await Assert.That(result).IsTrue();
	}

	[Test]
	public async Task IsLikelyCompoundWord_GivenHttpConnection_ReturnsTrue()
	{
		// Arrange
		const string input = "httpconnection";

		// Act
		var result = Utilities.IsLikelyCompoundWord(input);

		// Assert
		await Assert.That(result).IsFalse(); // "connection" doesn't match suffixes, but starts with common prefix
	}

	[Test]
	public async Task IsLikelyCompoundWord_GivenShortString_ReturnsFalse()
	{
		// Arrange
		const string input = "test";

		// Act
		var result = Utilities.IsLikelyCompoundWord(input);

		// Assert
		await Assert.That(result).IsFalse();
	}

	[Test]
	public async Task IsLikelyCompoundWord_GivenStringWithDot_ReturnsFalse()
	{
		// Arrange
		const string input = "entity.id";

		// Act
		var result = Utilities.IsLikelyCompoundWord(input);

		// Assert
		await Assert.That(result).IsFalse();
	}

	[Test]
	public async Task IsLikelyCompoundWord_GivenStringWithUnderscore_ReturnsFalse()
	{
		// Arrange
		const string input = "entity_id";

		// Act
		var result = Utilities.IsLikelyCompoundWord(input);

		// Assert
		await Assert.That(result).IsFalse();
	}

	[Test]
	public async Task IsLikelyCompoundWord_GivenEmptyString_ReturnsFalse()
	{
		// Arrange
		const string input = "";

		// Act
		var result = Utilities.IsLikelyCompoundWord(input);

		// Assert
		await Assert.That(result).IsFalse();
	}

	[Test]
	public async Task IsLikelyCompoundWord_GivenGetMethod_ReturnsTrue()
	{
		// Arrange
		const string input = "getvalue";

		// Act
		var result = Utilities.IsLikelyCompoundWord(input);

		// Assert
		await Assert.That(result).IsTrue();
	}

	[Test]
	public async Task IsGenericOrReservedName_GivenActivity_ReturnsTrue()
	{
		// Arrange
		const string input = "activity";

		// Act
		var result = Utilities.IsGenericOrReservedName(input);

		// Assert
		await Assert.That(result).IsTrue();
	}

	[Test]
	public async Task IsGenericOrReservedName_GivenEvent_ReturnsTrue()
	{
		// Arrange
		const string input = "event";

		// Act
		var result = Utilities.IsGenericOrReservedName(input);

		// Assert
		await Assert.That(result).IsTrue();
	}

	[Test]
	public async Task IsGenericOrReservedName_GivenError_ReturnsTrue()
	{
		// Arrange
		const string input = "error";

		// Act
		var result = Utilities.IsGenericOrReservedName(input);

		// Assert
		await Assert.That(result).IsTrue();
	}

	[Test]
	public async Task IsGenericOrReservedName_GivenCustomerName_ReturnsFalse()
	{
		// Arrange
		const string input = "CustomerName";

		// Act
		var result = Utilities.IsGenericOrReservedName(input);

		// Assert
		await Assert.That(result).IsFalse();
	}

	[Test]
	public async Task IsGenericOrReservedName_GivenEmptyString_ReturnsFalse()
	{
		// Arrange
		const string input = "";

		// Act
		var result = Utilities.IsGenericOrReservedName(input);

		// Assert
		await Assert.That(result).IsFalse();
	}

	[Test]
	public async Task IsGenericOrReservedName_GivenMixedCaseActivity_ReturnsTrue()
	{
		// Arrange
		const string input = "Activity";

		// Act
		var result = Utilities.IsGenericOrReservedName(input);

		// Assert
		await Assert.That(result).IsTrue();
	}
}
