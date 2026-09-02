namespace Purview.Telemetry.SourceGenerator.Infra;

static class TestHelpers
{
	/// <summary>Replaces all occurrences using ordinal semantics (net48 lacks the StringComparison overload).</summary>
	public static string ReplaceOrdinal(this string value, string oldValue, string newValue) =>
#if NET48
		value.Replace(oldValue, newValue);
#else
		value.Replace(oldValue, newValue, StringComparison.Ordinal);
#endif

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

	public static async Task VerifyAsync(
		DriverRunResult result,
		bool expectsDiagnostics = false,
		bool whenValidatingDiagnosticsIgnoreNonErrors = false,
		CancellationToken cancellationToken = default
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var diag = result.DriverResult.Diagnostics.AddRange(result.AnalyzerResult?.Diagnostics ?? []).ToArray();
		if (whenValidatingDiagnosticsIgnoreNonErrors)
			diag = [.. diag.Where(m => m.Severity == DiagnosticSeverity.Error)];

		if (expectsDiagnostics)
		{
			await Assert.That(diag).IsNotEmpty();
		}
		else
		{
			await Assert
				.That(diag)
				.IsEmpty()
				.Because(
					$"Expected no diagnostics, but found: [{string.Join(", ", diag.Select(d => d.Id).Distinct())}]"
				);
		}
	}
}
