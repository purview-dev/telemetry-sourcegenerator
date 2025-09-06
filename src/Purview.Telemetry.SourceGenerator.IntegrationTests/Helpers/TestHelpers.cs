using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Purview.Telemetry.SourceGenerator;

static partial class TestHelpers
{
	static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = false,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};

	public static bool TryGetGeneratedSourceResult(
		this GenerationResult generationResult,
		string hintName,
		out GeneratedSourceResult generatedSourceResult
	) =>
		TryGetGeneratedSourceResult(
			generationResult,
			r => r.HintName == hintName,
			out generatedSourceResult
		);

	public static bool TryGetGeneratedSourceResult(
		this GenerationResult generationResult,
		Func<GeneratedSourceResult, bool> predicate,
		out GeneratedSourceResult generatedSourceResult
	)
	{
		generatedSourceResult = default;
		foreach (var result in generationResult.DriverResult.Results)
		{
			// We only return one match here...
			foreach (var source in result.GeneratedSources.Where(predicate))
			{
				generatedSourceResult = source;
				return true;
			}
		}

		return false;
	}

	public static string? GetGeneratedSourceText(
		this GenerationResult generationResult,
		string hintName,
		[DoesNotReturnIf(true)] bool failOnNotFound = true
	) => GetGeneratedSourceText(generationResult, r => r.HintName == hintName, failOnNotFound);

	public static string? GetGeneratedSourceText(
		this GenerationResult generationResult,
		Func<GeneratedSourceResult, bool> predicate,
		[DoesNotReturnIf(true)] bool failOnNotFound = true
	)
	{
		var found = generationResult.TryGetGeneratedSourceResult(predicate, out var sourceResult);
		if (failOnNotFound && !found)
			found.ShouldBeTrue();

		return found ? sourceResult.SourceText.ToString() : null;
	}

	public static IEnumerable<GeneratedSourceResult> GetGeneratedSourceResults(
		this GenerationResult generationResult,
		Func<GeneratedSourceResult, bool>? predicate = null
	)
	{
		predicate ??= _ => true;

		foreach (var result in generationResult.DriverResult.Results)
		{
			foreach (var source in result.GeneratedSources.Where(predicate))
			{
				yield return source;
			}
		}
	}

	public const string DefaultUsingSet =
		@"using System;
using Purview.Telemetry;";

	public static List<string> GetCasePermutations(string input)
	{
		List<string> result = [];

		if (string.IsNullOrWhiteSpace(input))
		{
			result.Add(input);
			return result;
		}

		var currentChar = input[0];
		var remainder = input[1..];
		var remainderPermutations = GetCasePermutations(remainder);

		if (char.IsLetter(currentChar))
		{
			foreach (var s in remainderPermutations)
			{
				result.Add(
					char.ToLower(currentChar, System.Globalization.CultureInfo.InvariantCulture) + s
				);
				result.Add(
					char.ToUpper(currentChar, System.Globalization.CultureInfo.InvariantCulture) + s
				);
			}
		}
		else
		{
			foreach (var s in remainderPermutations)
				result.Add(currentChar + s);
		}

		return result;
	}

	public static string GetFriendlyTypeName(Type type, bool useSystemType = true)
	{
		if (type.IsGenericParameter)
			return type.Name;

		if (!type.IsGenericType)
		{
			if (useSystemType)
			{
				switch (type.FullName)
				{
					case "System.String":
						return "string";
					case "System.Int32":
						return "int";
					case "System.Int64":
						return "long";
					case "System.Int16":
						return "short";
					case "System.Boolean":
						return "bool";
					case "System.Single":
						return "float";
					case "System.Double":
						return "double";
					case "System.Decimal":
						return "decimal";
					case "System.Byte":
						return "byte";
					case "System.SByte":
						return "sbyte";
					case "System.UInt16":
						return "ushort";
					case "System.UInt32":
						return "uint";
					case "System.UInt64":
						return "ulong";
					case "System.Char":
						return "char";
					case "System.Object":
						return "object";
				}
			}

			return type.FullName ?? type.Name;
		}

		var name = type.Name;
		var index = name.IndexOf('`', StringComparison.OrdinalIgnoreCase);

		StringBuilder builder = new();
		builder.Append(type.Namespace).Append('.').Append(name[..index]).Append('<');

		var first = true;
		foreach (var arg in type.GetGenericArguments())
		{
			if (!first)
				builder.Append(',');

			builder.Append(GetFriendlyTypeName(arg));
			first = false;
		}

		builder.Append('>');
		return builder.ToString();
	}

	public static async Task Verify(
		GenerationResult generationResult,
		Action<SettingsTask>? config = null,
		bool expectsDiagnostics = false,
		bool whenValidatingDiagnosticsIgnoreNonErrors = false,
		bool validationCompilation = true,
		bool autoVerifyTemplates = true,
		params object[] parameters
	)
	{
		var verifierTask = Verifier
			.Verify(generationResult.DriverResult)
			.UseDirectory("Snapshots")
			.DisableRequireUniquePrefix()
			.DisableDateCounting()
			//.UniqueForTargetFrameworkAndVersion(typeof(TestHelpers).Assembly)
			.ScrubInlineDateTimeOffsets("yyyy-MM-dd HH:mm:ss zzzz"); // 2024-22-02 14:43:22 +00:00

		if (autoVerifyTemplates)
		{
			verifierTask = verifierTask.AutoVerify(file =>
			{
				foreach (var template in Constants.GetEmbeddedFileNames())
				{
					var potentialName = $"#{template}.g";
					if (
						file.IndexOf(potentialName, StringComparison.Ordinal) > -1
						&& file.EndsWith(".cs", StringComparison.Ordinal)
					)
					{
						return true;
					}
				}

				return false;
			});
		}

		if (parameters.Length > 0)
		{
			verifierTask = verifierTask.UseTextForParameters(
				ComputeParameterFilenameHash(parameters)
			);
		}

		config?.Invoke(verifierTask);

		// verifierTask = verifierTask.AutoVerify();

		await verifierTask;

		var diag = generationResult.Diagnostics.AsEnumerable();
		if (whenValidatingDiagnosticsIgnoreNonErrors)
			diag = diag.Where(m => m.Severity == DiagnosticSeverity.Error);

		if (expectsDiagnostics)
			diag.ShouldNotBeEmpty();
		else
			diag.ShouldBeEmpty();

		if (!validationCompilation)
			return;

		await using MemoryStream ms = new();

		var result = generationResult.Compilation.Emit(ms);
		if (!result.Success)
		{
			result
				.Diagnostics.Where(m => !m.Id.StartsWith("TSG", StringComparison.Ordinal))
				.ShouldBeEmpty(
					string.Join(
						Environment.NewLine,
						result.Diagnostics.Select(d =>
							d
							+ Environment.NewLine
							+ "-----------------------------------------------------"
						)
					)
				);
		}
	}

	static string ComputeParameterFilenameHash(IEnumerable<object> items)
	{
		var json = JsonSerializer.Serialize(items, JsonOptions);
		var digest = SHA256.HashData(Encoding.UTF8.GetBytes(json));
		var base64 = Convert
			.ToBase64String(digest)
			.TrimEnd('=') // remove padding
			.Replace('+', '-') // URL-safe
			.Replace('/', '_');

		return base64;
	}
}
