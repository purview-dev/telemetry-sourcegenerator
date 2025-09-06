using System.Text;
using Purview.Telemetry.SourceGenerator.Configuration;

namespace Purview.Telemetry.SourceGenerator;

/// <summary>
/// Helper class for writing generated source content to files for debugging and inspection.
/// </summary>
static class TestOutputWriter
{
	/// <summary>
	/// Writes all generated sources from a GenerationResult to the configured output directory.
	/// Only writes if output is enabled via environment variable.
	/// </summary>
	/// <param name="generationResult">The generation result containing generated sources.</param>
	/// <param name="testName">The name of the test (used for organizing output).</param>
	public static void WriteGeneratedContent(GenerationResult generationResult, string testName)
	{
		if (!TestOutputConfiguration.IsOutputEnabled)
		{
			return;
		}

		try
		{
			var testOutputDir = TestOutputConfiguration.CreateTestDirectory(testName);
			WriteGenerationResult(generationResult, testOutputDir);
			WriteMetadata(generationResult, testOutputDir, testName);
		}
		catch (Exception ex)
		{
			// Don't let output writing failures break tests
			Console.WriteLine($"Warning: Failed to write test output for {testName}: {ex.Message}");
		}
	}

	/// <summary>
	/// Writes the generation result to the specified directory.
	/// </summary>
	static void WriteGenerationResult(GenerationResult generationResult, string outputDir)
	{
		// Write generated sources
		var sourcesDir = Path.Combine(outputDir, "Generated");
		Directory.CreateDirectory(sourcesDir);

		foreach (var result in generationResult.DriverResult.Results)
		{
			if (result.Exception != null)
			{
				WriteExceptionInfo(result.Exception, sourcesDir);
				continue;
			}

			foreach (var source in result.GeneratedSources)
			{
				WriteSingleSource(source, sourcesDir);
			}
		}

		// Write compilation diagnostics
		if (generationResult.Diagnostics.Length > 0)
		{
			WriteDiagnostics(generationResult.Diagnostics, outputDir);
		}

		// Write compilation syntax trees (input sources)
		WriteInputSources(generationResult.Compilation, outputDir);
	}

	/// <summary>
	/// Writes a single generated source to a file.
	/// </summary>
	static void WriteSingleSource(GeneratedSourceResult source, string sourcesDir)
	{
		var fileName = source.HintName;
		if (!fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
		{
			fileName += ".cs";
		}

		var filePath = Path.Combine(sourcesDir, fileName);
		var sourceText = source.SourceText.ToString();

		File.WriteAllText(filePath, sourceText, Encoding.UTF8);
	}

	/// <summary>
	/// Writes exception information to a file.
	/// </summary>
	static void WriteExceptionInfo(Exception exception, string sourcesDir)
	{
		var exceptionFile = Path.Combine(sourcesDir, "EXCEPTION.txt");
		var content = new StringBuilder();
		content
			.AppendLine("Exception occurred during generation:")
			.AppendLine($"Type: {exception.GetType().FullName}")
			.AppendLine($"Message: {exception.Message}")
			.AppendLine("Stack Trace:")
			.AppendLine(exception.StackTrace);

		if (exception.InnerException != null)
		{
			content
				.AppendLine()
				.AppendLine("Inner Exception:")
				.AppendLine($"Type: {exception.InnerException.GetType().FullName}")
				.AppendLine($"Message: {exception.InnerException.Message}")
				.AppendLine("Stack Trace:")
				.AppendLine(exception.InnerException.StackTrace);
		}

		File.WriteAllText(exceptionFile, content.ToString(), Encoding.UTF8);
	}

	/// <summary>
	/// Writes compilation diagnostics to a file.
	/// </summary>
	static void WriteDiagnostics(ImmutableArray<Diagnostic> diagnostics, string outputDir)
	{
		var diagnosticsFile = Path.Combine(outputDir, "diagnostics.txt");
		var content = new StringBuilder();

		content
			.AppendLine("Compilation Diagnostics:")
			.AppendLine("========================")
			.AppendLine();

		foreach (var diagnostic in diagnostics)
		{
			content.AppendLine(
				$"[{diagnostic.Severity}] {diagnostic.Id}: {diagnostic.GetMessage()}"
			);
			if (diagnostic.Location != Location.None)
			{
				content.AppendLine($"  Location: {diagnostic.Location}");
			}
			content.AppendLine();
		}

		File.WriteAllText(diagnosticsFile, content.ToString(), Encoding.UTF8);
	}

	/// <summary>
	/// Writes input source files (what was fed to the generator) to a directory.
	/// </summary>
	static void WriteInputSources(Compilation compilation, string outputDir)
	{
		var inputDir = Path.Combine(outputDir, "Input");
		Directory.CreateDirectory(inputDir);

		var index = 0;
		foreach (var syntaxTree in compilation.SyntaxTrees)
		{
			var fileName = Path.GetFileName(syntaxTree.FilePath);
			if (
				string.IsNullOrEmpty(fileName)
				|| fileName.Contains("System.")
				|| fileName.Contains("Microsoft.")
			)
			{
				fileName = $"Input_{index:D3}.cs";
			}

			var filePath = Path.Combine(inputDir, fileName);
			var sourceText = syntaxTree.GetText().ToString();

			File.WriteAllText(filePath, sourceText, Encoding.UTF8);
			index++;
		}
	}

	/// <summary>
	/// Writes metadata about the test run.
	/// </summary>
	static void WriteMetadata(GenerationResult generationResult, string outputDir, string testName)
	{
		var metadataFile = Path.Combine(outputDir, "metadata.json");
		var metadata = new
		{
			TestName = testName,
			Timestamp = DateTime.UtcNow.ToString("O"),
			GeneratedSourcesCount = generationResult.DriverResult.Results.Sum(r =>
				r.GeneratedSources.Length
			),
			DiagnosticsCount = generationResult.Diagnostics.Length,
			HasErrors = generationResult.Diagnostics.Any(d =>
				d.Severity == DiagnosticSeverity.Error
			),
			HasWarnings = generationResult.Diagnostics.Any(d =>
				d.Severity == DiagnosticSeverity.Warning
			),
			CompilationAssemblyName = generationResult.Compilation.AssemblyName,
			InputSourcesCount = generationResult.Compilation.SyntaxTrees.Count(),
			Environment = new
			{
				Environment.MachineName,
				Environment.UserName,
				OSVersion = Environment.OSVersion.ToString(),
				CLRVersion = Environment.Version.ToString(),
				WorkingDirectory = Environment.CurrentDirectory,
			},
		};

		var json = System.Text.Json.JsonSerializer.Serialize(
			metadata,
			new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
		);

		File.WriteAllText(metadataFile, json, Encoding.UTF8);
	}

	/// <summary>
	/// Writes a summary of all generated content to a README file.
	/// </summary>
	public static void WriteSummary(string outputDir, string testName)
	{
		if (!TestOutputConfiguration.IsOutputEnabled)
		{
			return;
		}

		try
		{
			var readmeFile = Path.Combine(outputDir, "README.md");
			var content = new StringBuilder();

			content
				.AppendLine($"# Generated Content for Test: {testName}")
				.AppendLine()
				.AppendLine($"Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
				.AppendLine()
				.AppendLine("## Structure")
				.AppendLine()
				.AppendLine(
					"- `Generated/` - Contains all source files generated by the source generator"
				)
				.AppendLine(
					"- `Input/` - Contains the input source files that were fed to the generator"
				)
				.AppendLine(
					"- `diagnostics.txt` - Any compilation diagnostics (warnings, errors, etc.)"
				)
				.AppendLine(
					"- `metadata.json` - Metadata about the test run and generation process"
				)
				.AppendLine("- `README.md` - This file")
				.AppendLine()
				.AppendLine("## Usage")
				.AppendLine()
				.AppendLine(
					"This content is automatically generated when the `PURVIEW_TELEMETRY_OUTPUT_GENERATED_FILES=true` environment variable is set."
				)
				.AppendLine(
					"The output directory can be customized using the `PURVIEW_TELEMETRY_OUTPUT_DIRECTORY` environment variable."
				)
				.AppendLine()
				.AppendLine("## Generated Files")
				.AppendLine();

			var generatedDir = Path.Combine(outputDir, "Generated");
			if (Directory.Exists(generatedDir))
			{
				var files = Directory.GetFiles(generatedDir, "*.cs", SearchOption.AllDirectories);
				foreach (var file in files.Order())
				{
					var relativePath = Path.GetRelativePath(outputDir, file);
					var fileName = Path.GetFileName(file);
					content.AppendLine($"- `{relativePath}` - {fileName}");
				}
			}

			File.WriteAllText(readmeFile, content.ToString(), Encoding.UTF8);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Warning: Failed to write summary for {testName}: {ex.Message}");
		}
	}
}
