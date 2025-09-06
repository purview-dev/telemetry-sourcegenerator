namespace Purview.Telemetry.SourceGenerator.Configuration;

/// <summary>
/// Configuration for test output generation.
/// </summary>
static class TestOutputConfiguration
{
	/// <summary>
	/// Environment variable name to enable test output generation.
	/// Set to "true" to enable writing generated content to the output folder.
	/// </summary>
	public const string EnableOutputEnvironmentVariable =
		"PURVIEW_TELEMETRY_OUTPUT_GENERATED_FILES";

	/// <summary>
	/// Environment variable name to specify the output directory.
	/// Defaults to "generated-output" relative to the test project directory.
	/// </summary>
	public const string OutputDirectoryEnvironmentVariable = "PURVIEW_TELEMETRY_OUTPUT_DIRECTORY";

	/// <summary>
	/// Default output directory name (relative to test project root).
	/// </summary>
	public const string DefaultOutputDirectory = "generated-output";

	/// <summary>
	/// Gets whether output generation is enabled based on environment variables.
	/// </summary>
	public static bool IsOutputEnabled =>
		Environment
			.GetEnvironmentVariable(EnableOutputEnvironmentVariable)
			?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

	/// <summary>
	/// Gets the configured output directory path.
	/// </summary>
	public static string OutputDirectory
	{
		get
		{
			var customDir = Environment.GetEnvironmentVariable(OutputDirectoryEnvironmentVariable);
			if (!string.IsNullOrWhiteSpace(customDir))
			{
				return customDir;
			}

			// Default to a directory relative to the test project
			var testProjectDir = GetTestProjectDirectory();
			return Path.Combine(testProjectDir, DefaultOutputDirectory);
		}
	}

	/// <summary>
	/// Gets the test project directory by walking up from the current directory
	/// to find the directory containing a .csproj file.
	/// </summary>
	static string GetTestProjectDirectory()
	{
		var currentDir = AppContext.BaseDirectory;
		while (currentDir != null)
		{
			if (Directory.GetFiles(currentDir, "*.csproj").Length > 0)
			{
				return currentDir;
			}

			var parentDir = Directory.GetParent(currentDir);
			currentDir = parentDir?.FullName;
		}

		// Fallback to current directory if we can't find a project file
		return AppContext.BaseDirectory;
	}

	/// <summary>
	/// Ensures the output directory exists and returns the full path.
	/// </summary>
	public static string EnsureOutputDirectory()
	{
		var outputPath = OutputDirectory;
		if (!Directory.Exists(outputPath))
		{
			Directory.CreateDirectory(outputPath);
		}
		return outputPath;
	}

	/// <summary>
	/// Cleans the output directory by removing all files and subdirectories.
	/// </summary>
	public static void CleanOutputDirectory()
	{
		var outputPath = OutputDirectory;
		if (Directory.Exists(outputPath))
		{
			var di = new DirectoryInfo(outputPath);
			foreach (var file in di.GetFiles())
			{
				file.Delete();
			}
			foreach (var dir in di.GetDirectories())
			{
				dir.Delete(recursive: true);
			}
		}
	}

	/// <summary>
	/// Creates a test-specific subdirectory within the output directory.
	/// </summary>
	/// <param name="testName">The name of the test (used as subdirectory name).</param>
	/// <returns>The path to the test-specific output directory.</returns>
	public static string CreateTestDirectory(string testName)
	{
		var outputDir = EnsureOutputDirectory();
		var testDir = Path.Combine(outputDir, SanitizeFileName(testName));

		if (Directory.Exists(testDir))
		{
			Directory.Delete(testDir, recursive: true);
		}

		Directory.CreateDirectory(testDir);
		return testDir;
	}

	/// <summary>
	/// Sanitizes a test name to be safe for use as a directory name.
	/// </summary>
	static string SanitizeFileName(string fileName)
	{
		var invalidChars = Path.GetInvalidFileNameChars();
		var sanitized = string.Join(
			"_",
			fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)
		);
		return sanitized.Length > 100 ? sanitized.Substring(0, 100) : sanitized;
	}
}
