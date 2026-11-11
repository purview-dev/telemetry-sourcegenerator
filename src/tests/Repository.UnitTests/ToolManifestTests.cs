using System.Text.Json;

namespace Purview.Telemetry.Repository;

/// <summary>
/// Guards the local tool manifest (<c>.config/dotnet-tools.json</c>) against a shape that only fails on a
/// cold NuGet cache - which is every hosted run and every fresh clone.
/// </summary>
/// <remarks>
/// A RID-specific tool (<c>DotNetCliTool Version="2"</c> with <c>RuntimeIdentifierPackages</c>, laid out under
/// <c>tools/any/any</c>) installed beside another tool makes NuGet validate the wrong package's commands, so a
/// cold <c>dotnet tool restore</c> fails with a message that names the other entry:
/// <code>
/// The command "purview-build" specified in the tool manifest file is not contained in the package with
/// Package Id purview.build. The commands contained in the package are dotnet-inspect.
/// </code>
/// <c>dotnet-inspect</c> is the tool this repository has hit it with, twice: removed in <c>7427652</c> and
/// re-added by accident in <c>2f1c731</c>, which took the <c>pack-validation</c> PR job down again. Nothing here
/// runs it, so the entry is rejected by name; a new tool of the same shape needs its own line and the same
/// reasoning.
/// </remarks>
public sealed class ToolManifestTests
{
	/// <summary>Tools known to break a cold restore; each is rejected below with why.</summary>
	static readonly string[] RidSpecificTools = ["dotnet-inspect"];

	[Test]
	public async Task Manifest_GivenAKnownRidSpecificTool_IsRejected(CancellationToken cancellationToken)
	{
		// Arrange
		var manifestPath = Path.Combine(FindRepositoryRoot(), ".config", "dotnet-tools.json");
		await using var stream = File.OpenRead(manifestPath);
		using var manifest = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

		// Act
		var present = manifest
			.RootElement.GetProperty("tools")
			.EnumerateObject()
			.Select(static property => property.Name)
			.Intersect(RidSpecificTools, StringComparer.OrdinalIgnoreCase)
			.ToArray();

		// Assert
		await Assert
			.That(present)
			.IsEmpty()
			.Because(
				"a RID-specific tool in the manifest makes a cold 'dotnet tool restore' fail with a message that "
					+ $"names the wrong package, which breaks every hosted run: {string.Join(", ", present)}"
			);
	}

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
				&& File.Exists(Path.Combine(directory.FullName, "skills-lock.json"))
				&& Directory.Exists(Path.Combine(directory.FullName, "src", "src"))
			)
				return directory.FullName;
		}

		throw new InvalidOperationException(
			$"No repository root above '{AppContext.BaseDirectory}' carries AGENTS.md, skills-lock.json and src/src."
		);
	}
}
