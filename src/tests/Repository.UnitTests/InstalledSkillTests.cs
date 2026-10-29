using System.Text.Json;

namespace Purview.Telemetry.Repository;

/// <summary>
/// Guards the skills installed from other repositories. They are installed by the skills CLI into the
/// universal <c>.agents/skills/</c> folder only, and that folder and the CLI's <c>skills-lock.json</c>
/// are committed, so the two must agree and no per-agent copy or instruction file may appear beside them.
/// </summary>
/// <remarks>
/// <c>SkillTests</c> validates this repository's own skills in <c>skills/</c>; these are someone else's,
/// so their content is not checked here, only that the installed set is the recorded set.
/// </remarks>
public sealed class InstalledSkillTests
{
	static readonly Lazy<string> RepositoryRoot = new(FindRepositoryRoot);

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
	public async Task ClaudeFolder_GivenTheInstalledSkills_HoldsNoCopy(CancellationToken cancellationToken)
	{
		// Arrange
		cancellationToken.ThrowIfCancellationRequested();
		var claudeSkills = Path.Combine(RepositoryRoot.Value, ".claude", "skills");

		// Act
		var exists = Directory.Exists(claudeSkills);

		// Assert
		await Assert
			.That(exists)
			.IsFalse()
			.Because(
				"skills are installed into .agents/skills/ only. A .claude/skills/ folder means the CLI ran without "
					+ "`-a universal`; delete it and reinstall with `-a universal`"
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
