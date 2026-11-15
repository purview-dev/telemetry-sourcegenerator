using System.Diagnostics;
using System.Text.Json;

namespace Purview.Telemetry.Repository;

/// <summary>
/// Guards the skills installed from other repositories. They are installed by the skills CLI into the
/// universal <c>.agents/skills/</c> folder only, which is a local install: gitignored, reproducible from
/// the committed <c>skills-lock.json</c>, and never tracked - so a skill this repository installs but does
/// not publish cannot be found through <c>skills add purview-dev/telemetry-sourcegenerator</c>. Claude
/// Code only reads <c>.claude/skills/</c>, so that gitignored folder may hold links into
/// <c>.agents/skills/</c>, never copies.
/// </summary>
/// <remarks>
/// <para>
/// <c>SkillTests</c> validates this repository's own skills in <c>skills/</c>; these are someone else's,
/// so their content is not checked here, only that the installed set is the recorded set.
/// </para>
/// <para>
/// <see cref="DeveloperSkills"/> are the exception in one respect only: they are written here, for people
/// working on the generator, rather than installed from elsewhere. They are still authored skills, so they
/// live in <c>skills/</c> with the rest, and no CLI records them in <c>skills-lock.json</c>.
/// </para>
/// </remarks>
public sealed class InstalledSkillTests
{
	static readonly Lazy<string> RepositoryRoot = new(FindRepositoryRoot);

	/// <summary>
	/// Skills authored here for this repository's own developers rather than installed from elsewhere. They
	/// live in <c>skills/</c> like any other authored skill, and no CLI records them in
	/// <c>skills-lock.json</c>.
	/// </summary>
	static readonly string[] DeveloperSkills = ["purview-telemetry-benchmarks"];

	[Test]
	public async Task Lockfile_GivenEveryEntry_HasItsSkillInstalledUnderAgents(CancellationToken cancellationToken)
	{
		// Arrange
		var recorded = await ReadLockfileSkillsAsync(cancellationToken);
		if (!Directory.Exists(AgentsSkillsFolder()))
		{
			// A fresh clone has no local install - it is gitignored and restored with `just skills-install` -
			// so the committed record is what has to be there.
			await Assert.That(recorded).IsNotEmpty().Because("skills-lock.json records no installed skills");
			return;
		}

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
		if (!Directory.Exists(AgentsSkillsFolder()))
		{
			// No local install to check against the record; the record itself is checked above.
			return;
		}

		// Act - the folder holds what the CLI installed and nothing else: the authored skills, including
		// the developer ones, live in skills/.
		var unrecorded = Directory
			.GetDirectories(AgentsSkillsFolder())
			.Select(static path => Path.GetFileName(path))
			.Where(name => !recorded.Contains(name, StringComparer.Ordinal))
			.ToArray();

		// Assert
		await Assert
			.That(unrecorded)
			.IsEmpty()
			.Because(
				"every folder in .agents/skills/ must be written by the skills CLI, which records it in "
					+ $"skills-lock.json: {string.Join(", ", unrecorded)}"
			);
	}

	[Test]
	public async Task DeveloperSkills_GivenEachOne_IsAuthoredUnderSkills(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();

		// Act - a developer skill is an authored skill, so it ships from skills/ like the rest. No CLI
		// installed it, which is why skills-lock.json does not record it.
		var notAuthored = DeveloperSkills
			.Where(static name => !File.Exists(Path.Combine(RepositoryRoot.Value, "skills", name, "SKILL.md")))
			.ToArray();
		var recorded = await ReadLockfileSkillsAsync(cancellationToken);
		var recordedAnyway = DeveloperSkills.Where(name => recorded.Contains(name, StringComparer.Ordinal)).ToArray();

		// Assert
		await Assert
			.That(notAuthored)
			.IsEmpty()
			.Because($"a developer skill must ship its SKILL.md in skills/: {string.Join(", ", notAuthored)}");
		await Assert
			.That(recordedAnyway)
			.IsEmpty()
			.Because(
				"a developer skill is written here rather than installed, so no CLI records it in "
					+ $"skills-lock.json: {string.Join(", ", recordedAnyway)}"
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

	[Test]
	public async Task AgentsSkills_GivenEverythingTheCLIWrites_IsIgnoredAndNeverTracked(
		CancellationToken cancellationToken
	)
	{
		// Arrange - the folder is a local install, so nothing under it may be tracked, and the rules that
		// keep it (and every per-agent folder) out of this repository must be in place. This is what stops
		// a skill the repository installs but does not publish being found by `skills add
		// purview-dev/telemetry-sourcegenerator`.
		var tracked = await TrackedFilesUnderAgentsSkillsAsync(cancellationToken);
		var ignores = await AgentsSkillsIgnoreRulesAsync(cancellationToken);

		// Assert
		await Assert
			.That(tracked)
			.IsEmpty()
			.Because(
				"nothing under .agents/skills/ may be tracked: it is a local install, reproducible from "
					+ $"skills-lock.json with `just skills-install`: {string.Join(", ", tracked)}"
			);
		await Assert
			.That(ignores.Agents)
			.IsTrue()
			.Because(".gitignore must keep .agents/skills/ out of this repository");
		await Assert.That(ignores.Claude).IsTrue().Because(".gitignore must keep .claude/ out of this repository");
	}

	/// <summary>What git tracks under <c>.agents/skills/</c>, asked of the repository itself.</summary>
	static async Task<string[]> TrackedFilesUnderAgentsSkillsAsync(CancellationToken cancellationToken)
	{
		using Process process = new()
		{
			StartInfo = new ProcessStartInfo("git", "ls-files .agents/skills")
			{
				WorkingDirectory = RepositoryRoot.Value,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			},
		};
		process.Start();
		var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
		await process.WaitForExitAsync(cancellationToken);
		return
		[
			.. output
				.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Order(StringComparer.Ordinal),
		];
	}

	/// <summary>The ignore rules for the two agent folders, as <c>.gitignore</c> spells them.</summary>
	static async Task<(bool Agents, bool Claude)> AgentsSkillsIgnoreRulesAsync(CancellationToken cancellationToken)
	{
		var lines = await File.ReadAllLinesAsync(Path.Combine(RepositoryRoot.Value, ".gitignore"), cancellationToken);
		var rules = lines.Select(static line => line.Trim()).ToHashSet(StringComparer.Ordinal);
		return (
			rules.Overlaps([".agents/skills/", ".agents/skills", "/.agents/skills/", ".agents/"]),
			rules.Overlaps([".claude/", ".claude", "/.claude/", "/.claude"])
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
