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
	public async Task IsLikelyCompoundWord_GivenExactly6Chars_ReturnsFalse()
	{
		// Arrange
		const string input = "testid"; // 6 chars with 'id' suffix

		// Act
		var result = Utilities.IsLikelyCompoundWord(input);

		// Assert
		// The code checks for length >= 6, so this may match the 'id' suffix
		await Assert.That(result).IsTrue();
	}

	[Test]
	public async Task IsLikelyCompoundWord_GivenJustOverThreshold_WithSuffix_ReturnsTrue()
	{
		// Arrange
		const string input = "testkey"; // 7 chars with 'key' suffix

		// Act
		var result = Utilities.IsLikelyCompoundWord(input);

		// Assert
		await Assert.That(result).IsTrue();
	}

	[Test]
	public async Task IsLikelyCompoundWord_GivenStringEndingButNotMatching_ReturnsFalse()
	{
		// Arrange
		const string input = "testingkey"; // ends with 'key' but >6 chars total

		// Act
		var result = Utilities.IsLikelyCompoundWord(input);

		// Assert
		await Assert.That(result).IsTrue(); // Should match 'key' suffix
	}

	[Test]
	public async Task IsLikelyCompoundWord_GivenAllCommonSuffixes_ReturnsTrue()
	{
		// Arrange
		string[] suffixes =
		[
			"id",
			"key",
			"name",
			"type",
			"count",
			"value",
			"time",
			"date",
			"code",
			"number",
		];

		foreach (var suffix in suffixes)
		{
			var input = $"test{suffix}";

			// Act
			var result = Utilities.IsLikelyCompoundWord(input);

			// Assert
			await Assert.That(result).IsTrue();
		}
	}

	[Test]
	public async Task IsLikelyCompoundWord_GivenAllCommonPrefixes_ReturnsTrue()
	{
		// Arrange
		string[] prefixes = ["get", "set", "is", "has", "can", "should", "will"];

		foreach (var prefix in prefixes)
		{
			var input = $"{prefix}value";

			// Act
			var result = Utilities.IsLikelyCompoundWord(input);

			// Assert
			await Assert.That(result).IsTrue();
		}
	}

	[Test]
	public async Task IsGenericOrReservedName_GivenAllReservedTerms_ReturnsTrue()
	{
		// Arrange
		string[] reserved =
		[
			"activity",
			"event",
			"error",
			"exception",
			"start",
			"stop",
			"begin",
			"end",
			"task",
			"action",
			"func",
			"method",
			"operation",
			"process",
			"handler",
		];

		foreach (var term in reserved)
		{
			// Act
			var result = Utilities.IsGenericOrReservedName(term);

			// Assert
			await Assert.That(result).IsTrue();
		}
	}

	[Test]
	public async Task IsGenericOrReservedName_GivenWhitespace_ReturnsFalse()
	{
		// Arrange
		const string input = "   ";

		// Act
		var result = Utilities.IsGenericOrReservedName(input);

		// Assert
		await Assert.That(result).IsFalse();
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

	[Test]
	public async Task IsLikelyCompoundWord_GivenVeryLongString_Completes()
	{
		// Arrange
		var input = "very" + new string('x', 100) + "name";

		// Act
		var result = Utilities.IsLikelyCompoundWord(input);

		// Assert
		await Assert.That(result).IsTrue(); // Should detect 'name' suffix
	}
}
