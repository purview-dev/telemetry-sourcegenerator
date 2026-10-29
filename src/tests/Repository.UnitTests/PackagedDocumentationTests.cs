using System.Text.RegularExpressions;

namespace Purview.Telemetry.Repository;

/// <summary>
/// Guards the documentation that ships inside the packages: every <c>Sdk/*.md</c> file under <c>src/src</c>, which
/// Purview.BuildSdk packs at the package root (the generator's <c>Sdk/README.md</c> is a symbolic link to the
/// repository <c>README.md</c>).
/// </summary>
/// <remarks>
/// A packaged document is frozen into every version of the package it ships in, and nothing in the pipeline rewrites
/// it: <c>just update-version</c> is a manual step. 5.0.6 shipped a README telling consumers to install 5.0.5. So the
/// packaged documents carry no package version at all - <c>dotnet add package</c> and the NuGet badge supply it - and
/// these tests fail when one creeps back in.
/// </remarks>
public sealed partial class PackagedDocumentationTests
{
	const string PackageId = "Purview.Telemetry.SourceGenerator";

	static readonly Lazy<string> RepositoryRoot = new(FindRepositoryRoot);

	public static IEnumerable<Func<string>> PackagedDocuments() =>
		Directory
			.GetFiles(Path.Combine(FindRepositoryRoot(), "src", "src"), "*.md", SearchOption.AllDirectories)
			.Where(static path => Path.GetFileName(Path.GetDirectoryName(path)) == "Sdk")
			.Select(path => Path.GetRelativePath(FindRepositoryRoot(), path).Replace('\\', '/'))
			.Order(StringComparer.Ordinal)
			.Select(static path => (Func<string>)(() => path));

	[Test]
	public async Task PackagedDocuments_GivenTheRepository_IncludeTheGeneratorReadme(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string readme = "src/src/SourceGenerator/Sdk/README.md";

		// Act
		var documents = PackagedDocuments().Select(static document => document()).ToArray();
		var packaged = await File.ReadAllTextAsync(Path.Combine(RepositoryRoot.Value, readme), cancellationToken);
		var repository = await File.ReadAllTextAsync(
			Path.Combine(RepositoryRoot.Value, "README.md"),
			cancellationToken
		);

		// Assert
		await Assert.That(documents).Contains(readme).Because("the discovery found no packaged README to check");
		await Assert
			.That(packaged)
			.IsEqualTo(repository)
			.Because(
				$"{readme} must resolve to the repository README.md. A checkout without symbolic link support "
					+ "(git config core.symlinks false) leaves the link's target path in the file, and that is what "
					+ "would be packed as the package README"
			);
	}

	[Test]
	[MethodDataSource(nameof(PackagedDocuments))]
	public async Task PackagedDocument_GivenItsText_CarriesNoHardCodedPackageVersion(
		string document,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var lines = await File.ReadAllLinesAsync(Path.Combine(RepositoryRoot.Value, document), cancellationToken);

		// Act
		var pinned = lines
			.Select(static (line, index) => (Line: line, Number: index + 1))
			.Where(static entry => PinsAVersion(entry.Line))
			.Select(static entry => $"line {entry.Number}: {entry.Line.Trim()}")
			.ToArray();

		// Assert
		await Assert
			.That(pinned)
			.IsEmpty()
			.Because(
				$"{document} ships inside the package and is never rewritten, so a version in it goes stale with the "
					+ $"next release. Leave the version to `dotnet add package` or the NuGet badge: {string.Join("; ", pinned)}"
			);
	}

	[Test]
	[Arguments("<PackageReference Include=\"Purview.Telemetry.SourceGenerator\" Version=\"5.0.5\">", true)]
	[Arguments("<PackageVersion Include=\"Purview.Telemetry.SourceGenerator\" Version=\"5.0.7-prerelease.1\" />", true)]
	[Arguments("dotnet add package Purview.Telemetry.SourceGenerator --version 5.0.6", true)]
	[Arguments("Install-Package Purview.Telemetry.SourceGenerator -Version 5.0.6", true)]
	[Arguments("**Current Version:** 5.0.6", true)]
	[Arguments("dotnet add package Purview.Telemetry.SourceGenerator", false)]
	[Arguments("<PackageReference Include=\"Purview.Telemetry.SourceGenerator\">", false)]
	[Arguments("Benchmarked on 13th Gen Intel Core i9-13900KF, .NET SDK 10.0.401.", false)]
	[Arguments("- Visual Studio 2026 (18.x) or .NET 10 SDK (Roslyn 5.9.0+)", false)]
	public async Task PinsAVersion_GivenALine_RecognisesAPackageVersion(
		string line,
		bool expected,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();

		// Act
		var pins = PinsAVersion(line);

		// Assert
		await Assert.That(pins).IsEqualTo(expected);
	}

	/// <summary>
	/// A version written beside this package's id, in a <c>Version="..."</c> attribute, after a <c>--version</c> /
	/// <c>-Version</c> switch, or as the <c>**Current Version:**</c> marker <c>.build/update-version.ts</c> rewrites.
	/// </summary>
	static bool PinsAVersion(string line) =>
		VersionAttribute().IsMatch(line)
		|| VersionSwitch().IsMatch(line)
		|| line.Contains("**Current Version:**", StringComparison.Ordinal)
		|| (line.Contains(PackageId, StringComparison.Ordinal) && SemanticVersion().IsMatch(line));

	[GeneratedRegex(@"\bVersion\s*=\s*""\d", RegexOptions.IgnoreCase)]
	private static partial Regex VersionAttribute();

	[GeneratedRegex(@"(^|\s)--?version\s+\d", RegexOptions.IgnoreCase)]
	private static partial Regex VersionSwitch();

	[GeneratedRegex(@"\b\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?\b")]
	private static partial Regex SemanticVersion();

	static string FindRepositoryRoot()
	{
		for (
			var directory = new DirectoryInfo(AppContext.BaseDirectory);
			directory is not null;
			directory = directory.Parent
		)
		{
			if (
				File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))
				&& File.Exists(Path.Combine(directory.FullName, "package.json"))
				&& Directory.Exists(Path.Combine(directory.FullName, "src", "src"))
			)
				return directory.FullName;
		}

		throw new InvalidOperationException(
			$"No repository root above '{AppContext.BaseDirectory}' carries AGENTS.md, package.json and src/src."
		);
	}
}
