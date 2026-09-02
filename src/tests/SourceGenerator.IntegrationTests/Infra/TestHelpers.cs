using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Purview.Telemetry.SourceGenerator.Infra;

static class TestHelpers
{
	public const string DefaultUsingSet =
		@"
using System;
using Purview.Telemetry;

";

	public static string Wrap(this string value, char c = '"') => c + value + c;

	/// <summary>Replaces all occurrences using ordinal semantics (net48 lacks the StringComparison overload).</summary>
	public static string ReplaceOrdinal(this string value, string oldValue, string newValue) =>
#if NET48
		value.Replace(oldValue, newValue);
#else
		value.Replace(oldValue, newValue, StringComparison.Ordinal);
#endif

	/// <summary>Throws if the argument is null (net48 lacks ArgumentNullException.ThrowIfNull).</summary>
#if NET8_0_OR_GREATER
	[StackTraceHidden]
#endif
	[DebuggerHidden]
	public static void ThrowIfNull(object? argument, [CallerMemberName] string? paramName = null)
	{
#if NET48
		if (argument is null)
			throw new ArgumentNullException(paramName);
#else
		ArgumentNullException.ThrowIfNull(argument, paramName);
#endif
	}

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
		bool validationCompilation = true,
		string[]? expectedDiagnosticCodes = null,
		CancellationToken cancellationToken = default
	)
	{
		var diag = result.DriverResult.Diagnostics.AddRange(result.AnalyzerResult?.Diagnostics ?? []).ToArray();
		if (whenValidatingDiagnosticsIgnoreNonErrors)
			diag = [.. diag.Where(m => m.Severity == DiagnosticSeverity.Error)];

		if (expectsDiagnostics)
		{
			await Assert.That(diag).IsNotEmpty();

			if (expectedDiagnosticCodes?.Length > 0)
			{
				var actualDiagnosticCodes = diag.Select(d => d.Id).Distinct().ToArray();
				var expectedCodes = expectedDiagnosticCodes.ToArray();

				await Assert
					.That(actualDiagnosticCodes)
					.IsEquivalentTo(expectedCodes)
					.Because(
						$"Expected diagnostic codes: [{string.Join(", ", expectedCodes)}], "
							+ $"but found: [{string.Join(", ", actualDiagnosticCodes)}]"
					);
			}
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

		if (!validationCompilation)
			return;

		using MemoryStream ms = new();

		var emitResult = result.CompilationResult.Compilation.Emit(ms, cancellationToken: cancellationToken);
		if (!emitResult.Success)
		{
			await Assert
				.That(emitResult.Diagnostics.Where(m => !m.Id.StartsWith("TSG", StringComparison.Ordinal)))
				.IsEmpty()
				.Because(
					string.Join(
						Environment.NewLine,
						emitResult.Diagnostics.Select(d =>
							$"{d}{Environment.NewLine}-----------------------------------------------------"
						)
					)
				);
		}
	}
}
