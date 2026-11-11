using System.Text.Json;

namespace Purview.Telemetry.Repository;

/// <summary>
/// Guards the skills installed from other repositories. They are installed by the skills CLI into the
/// universal <c>.agents/skills/</c> folder only, and that folder and the CLI's <c>skills-lock.json</c>
/// are committed, so the two must agree and no per-agent copy or instruction file may appear beside them.
/// Claude Code only reads <c>.claude/skills/</c>, so that gitignored folder may hold links into
/// <c>.agents/skills/</c>, never copies.
/// </summary>
/// <remarks>
/// <para>
/// <c>SkillTests</c> validates this repository's own skills in <c>skills/</c>; these are someone else's,
/// so their content is not checked here, only that the installed set is the recorded set.
/// </para>
/// <para>
/// <see cref="DeveloperSkills"/> are the exception: skills written here for people working on the
/// generator. They are not consumer guidance and are never published, so they cannot live in
/// <c>skills/</c> (which the skills CLI ships) or in the lockfile (which the CLI writes). They live in
/// <c>.agents/skills/</c> because that is the one folder every agent reads.
/// </para>
/// </remarks>
public sealed class InstalledSkillTests
{
	static readonly Lazy<string> RepositoryRoot = new(FindRepositoryRoot);

	/// <summary>
	/// Skills committed here for this repository's own developers. Each is authored in place, is not in
	/// <c>skills-lock.json</c> (no CLI wrote it) and must never appear in <c>skills/</c>, because anything
	/// there is published to consumers.
	/// </summary>
	static readonly string[] DeveloperSkills = ["purview-telemetry-benchmarks"];

	[Test]
	public async Task Lockfile_GivenEveryEntry_HasItsSkillInstalledUnderAgents(CancellationToken cancellationToken)
	{
		// Arrange
		var recorded = await ReadLockfileSkillsAsync(cancellationToken);

		// Act
		var missing = recorded
			.Where(static name => !File.Exists(Path.Combine(AgentsSkillsFolder(), name, "SKILL.md")))
			.ToArray();

		// Assert
		await Assert.That(recorded).IsNotEmpty().Because("skills-lock.json records no installed skills");
		await Assert
			.That(missing)
			.IsEmpty()
			.Because(
				"skills-lock.json records skills that are not installed in .agents/skills/. Restore them with "
					+ $"`npx -y skills@1 experimental_install`: {string.Join(", ", missing)}"
			);
	}

	[Test]
	public async Task AgentsSkills_GivenEveryInstalledSkill_IsRecordedInTheLockfile(CancellationToken cancellationToken)
	{
		// Arrange
		var recorded = await ReadLockfileSkillsAsync(cancellationToken);

		// Act
		var unrecorded = Directory
			.GetDirectories(AgentsSkillsFolder())
			.Select(static path => Path.GetFileName(path))
			.Where(name => !recorded.Contains(name, StringComparer.Ordinal))
			.Where(name => !DeveloperSkills.Contains(name, StringComparer.Ordinal))
			.ToArray();

		// Assert
		await Assert
			.That(unrecorded)
			.IsEmpty()
			.Because(
				"every folder in .agents/skills/ must be written by the skills CLI, which records it in "
					+ "skills-lock.json, or declared in InstalledSkillTests.DeveloperSkills as a "
					+ $"developer skill of this repository: {string.Join(", ", unrecorded)}"
			);
	}

	[Test]
	public async Task DeveloperSkills_GivenEachOne_IsPresentAndNeverPublished(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();

		// Act: a developer skill has to be loadable by an agent, and it must not also be a consumer skill.
		var notLoadable = DeveloperSkills
			.Where(static name => !File.Exists(Path.Combine(AgentsSkillsFolder(), name, "SKILL.md")))
			.ToArray();
		var published = DeveloperSkills
			.Where(static name => Directory.Exists(Path.Combine(RepositoryRoot.Value, "skills", name)))
			.ToArray();

		// Assert
		await Assert
			.That(notLoadable)
			.IsEmpty()
			.Because($"a developer skill must ship its SKILL.md in .agents/skills/: {string.Join(", ", notLoadable)}");
		await Assert
			.That(published)
			.IsEmpty()
			.Because(
				"a developer skill is for this repository only, so it must never sit in skills/, which the "
					+ $"skills CLI publishes to consumers: {string.Join(", ", published)}"
			);
	}

	[Test]
	public async Task ClaudeFolder_GivenTheInstalledSkills_HoldsOnlyLinksToThem(CancellationToken cancellationToken)
	{
		// Arrange: Claude Code reads .claude/skills/, not .agents/skills/, so a local (gitignored) folder of
		// links is allowed. A real copy is not: it drifts from skills-lock.json the moment the CLI updates.
		cancellationToken.ThrowIfCancellationRequested();
		var claudeSkills = Path.Combine(RepositoryRoot.Value, ".claude", "skills");
		if (!Directory.Exists(claudeSkills))
			return;

		// Act
		var notLinks = Directory
			.GetFileSystemEntries(claudeSkills)
			.Where(static path => !IsLinkToInstalledSkill(path))
			.Select(static path => Path.GetFileName(path))
			.ToArray();

		// Assert
		await Assert
			.That(notLinks)
			.IsEmpty()
			.Because(
				"every entry in .claude/skills/ must be a link to the same skill in .agents/skills/, not a copy. "
					+ $"Replace these with links (see AGENTS.md): {string.Join(", ", notLinks)}"
			);
	}

	[Test]
	public async Task AgentsSkills_GivenTheAuthoredSkills_HoldsNoCopyOfThem(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		var authored = Directory
			.GetDirectories(Path.Combine(RepositoryRoot.Value, "skills"))
			.Select(static path => Path.GetFileName(path))
			.ToArray();

		// Act
		var copied = authored
			.Where(static name => Directory.Exists(Path.Combine(AgentsSkillsFolder(), name)))
			.ToArray();

		// Assert
		await Assert
			.That(copied)
			.IsEmpty()
			.Because(
				"this repository's own skills live in skills/ only; a copy in .agents/skills/ would drift from "
					+ $"the authored one: {string.Join(", ", copied)}"
			);
	}

	[Test]
	public async Task Repository_GivenAgentInstructions_HasNoClaudeMd(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		string[] candidates =
		[
			Path.Combine(RepositoryRoot.Value, "CLAUDE.md"),
			Path.Combine(RepositoryRoot.Value, ".github", "CLAUDE.md"),
		];

		// Act
		var present = candidates.Where(File.Exists).ToArray();

		// Assert
		await Assert
			.That(present)
			.IsEmpty()
			.Because(
				"this repository is not agent-specific: AGENTS.md is its one instruction file, so a CLAUDE.md "
					+ $"must not be added: {string.Join(", ", present)}"
			);
	}

	static string AgentsSkillsFolder() => Path.Combine(RepositoryRoot.Value, ".agents", "skills");

	static bool IsLinkToInstalledSkill(string path)
	{
		var linkTarget = new DirectoryInfo(path).LinkTarget;
		if (linkTarget is null)
			return false;

		var resolved = Path.GetFullPath(linkTarget, Path.GetDirectoryName(path)!);
		var expected = Path.Combine(AgentsSkillsFolder(), Path.GetFileName(path));

		return string.Equals(Path.TrimEndingDirectorySeparator(resolved), expected, StringComparison.OrdinalIgnoreCase);
	}

	static async Task<string[]> ReadLockfileSkillsAsync(CancellationToken cancellationToken)
	{
		await using var stream = File.OpenRead(Path.Combine(RepositoryRoot.Value, "skills-lock.json"));
		using var lockfile = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

		return
		[
			.. lockfile
				.RootElement.GetProperty("skills")
				.EnumerateObject()
				.Select(static property => property.Name)
				.Order(StringComparer.Ordinal),
		];
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
