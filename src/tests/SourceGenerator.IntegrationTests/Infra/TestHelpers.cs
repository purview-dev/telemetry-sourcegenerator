namespace Purview.Telemetry.SourceGenerator.Infra;

static class TestHelpers
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0057:Use range operator")]
	public static List<string> GetCasePermutations(string input)
	{
		List<string> result = [];

		if (string.IsNullOrWhiteSpace(input))
		{
			result.Add(input);
			return result;
		}

		var currentChar = input[0];
		var remainder = input.Substring(1);
		var remainderPermutations = GetCasePermutations(remainder);

		if (char.IsLetter(currentChar))
		{
			foreach (var s in remainderPermutations)
			{
				result.Add(char.ToLower(currentChar, System.Globalization.CultureInfo.InvariantCulture) + s);
				result.Add(char.ToUpper(currentChar, System.Globalization.CultureInfo.InvariantCulture) + s);
			}
		}
		else
		{
			foreach (var s in remainderPermutations)
				result.Add(currentChar + s);
		}

		return result;
	}
}
