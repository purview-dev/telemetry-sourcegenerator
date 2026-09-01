using System.ComponentModel.DataAnnotations;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Settings;

public sealed record PublishLocalNuGetSettings : IValidatableObject
{
	public const string SectionName = "PublishLocalNuGet";

	[Required(AllowEmptyStrings = false)]
	public string LocalFeedPath { get; init; } = string.Empty;

	public bool OverwriteExistingPackages { get; init; } = true;

	public bool ShutdownDotnetBuilderServer { get; init; } = true;

	public bool ClearPackageCache { get; init; } = true;

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (string.IsNullOrWhiteSpace(LocalFeedPath))
		{
			yield return new ValidationResult("LocalFeedPath is required.", [nameof(LocalFeedPath)]);
			yield break;
		}

		// Path.IsPathRooted("p:foo") returns true, but a drive-relative path like "p:foo" is NOT an
		// absolute path: Path.GetFullPath resolves it against the current directory and can silently
		// copy packages to an unintended location. This is the classic signature of a Windows path whose
		// backslashes were stripped by a sh-style shell, e.g. 'p:\_sync-projects\.local-nuget\'.
		if (LocalFeedPath.Length >= 2 && LocalFeedPath[1] == ':')
		{
			var hasSeparatorAfterDrive =
				LocalFeedPath.Length >= 3
				&& (
					LocalFeedPath[2] == Path.DirectorySeparatorChar
					|| LocalFeedPath[2] == Path.AltDirectorySeparatorChar
				);
			if (!hasSeparatorAfterDrive)
			{
				yield return new ValidationResult(
					$"LocalFeedPath '{LocalFeedPath}' is drive-relative, not an absolute path. "
						+ "This is usually caused by the shell stripping backslashes from a Windows path such as "
						+ $"'p:\\_sync-projects\\.local-nuget\\'. Use forward slashes instead, e.g. "
						+ "'p:/_sync-projects/.local-nuget/'.",
					[nameof(LocalFeedPath)]
				);
				yield break;
			}
		}

		if (!Path.IsPathRooted(LocalFeedPath))
		{
			yield return new ValidationResult(
				$"LocalFeedPath must be an absolute path. Received: '{LocalFeedPath}'.",
				[nameof(LocalFeedPath)]
			);
			yield break;
		}

		var root = Path.GetPathRoot(LocalFeedPath);
		if (string.IsNullOrEmpty(root))
		{
			yield return new ValidationResult(
				$"LocalFeedPath could not be parsed. Received: '{LocalFeedPath}'.",
				[nameof(LocalFeedPath)]
			);
			yield break;
		}

		var lastChar = root[^1];
		if (lastChar == Path.DirectorySeparatorChar || lastChar == Path.AltDirectorySeparatorChar)
			yield break;

		if (root.StartsWith(@"\\", StringComparison.Ordinal) || root.StartsWith("//", StringComparison.Ordinal))
			yield break;

		yield return new ValidationResult(
			$"LocalFeedPath must be an absolute path (e.g. 'C:\\folder' or '\\\\server\\share'). Received: '{LocalFeedPath}'.",
			[nameof(LocalFeedPath)]
		);
	}
}
