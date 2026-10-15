using Purview.Skills.Testing;

namespace Purview.Telemetry;

/// <summary>
/// Runs the Agent Skills conformance rule set against this repository's authored skills in
/// <c>skills/</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>[InheritsTests]</c> is load-bearing.</b> TUnit does not walk base types when it discovers
/// tests, so without it this class compiles, the run is green, and nothing is validated.
/// </para>
/// <para>
/// There is no <c>RepositoryRoot</c> override: the default is the SDK's repository-root marker walk,
/// the same resolution <c>InstallPurviewSkills</c> uses, so an ordinary test run exercises it.
/// <see cref="SkillsFolder_ResolvesToThisRepository"/> is what catches a walk that landed elsewhere -
/// every inherited rule passes vacuously against a catalogue that was never found.
/// </para>
/// </remarks>
[InheritsTests]
public sealed class SkillTests : PurviewSkillTests
{
	[Test]
	public async Task SkillsFolder_ResolvesToThisRepository()
	{
		var catalogue = Evaluator.Catalogue;

		await Assert
			.That(catalogue.FolderExists)
			.IsTrue()
			.Because($"the marker walk resolved '{catalogue.RepositoryRoot}', whose skills folder does not exist");

		await Assert
			.That(File.Exists(Path.Combine(catalogue.RepositoryRoot, "AGENTS.md")))
			.IsTrue()
			.Because($"'{catalogue.RepositoryRoot}' does not look like this repository");

		await Assert.That(catalogue.Skills).IsNotEmpty();
	}

	[Test]
	public async Task EverySkill_IsNamedForThisProduct()
	{
		// Skills install into one flat directory per agent, shared with every skill a developer has
		// installed from anywhere. An unprefixed name collides with somebody else's, and the loser is
		// whichever installed second.
		var unprefixed = Evaluator
			.Catalogue.Skills.Select(skill => skill.FolderName)
			.Where(name => !name.StartsWith("purview-telemetry", StringComparison.Ordinal))
			.ToArray();

		await Assert
			.That(unprefixed)
			.IsEmpty()
			.Because(
				"every skill this repository publishes must carry the product prefix: " + string.Join(", ", unprefixed)
			);
	}
}
