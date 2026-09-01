using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.GitHub.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Modules;

[ModuleCategory("Release")]
[DependsOn<PublishNuGetModule>]
[DependsOn<VersionModule>]
public class CreateGitHubReleaseModule(IOptions<ReleaseSettings> releaseSettings, IOptions<GitHubSettings> gitSettings)
	: Module<Release?>
{
	protected override ModuleConfiguration Configure() =>
		ModuleConfiguration
			.Create()
			.WithSkipWhen(_ =>
				releaseSettings.Value.Mode is not (ReleaseMode.NuGet or ReleaseMode.GitHubRelease)
				|| string.IsNullOrWhiteSpace(gitSettings.Value.GetGitHubToken())
					? SkipDecision.Skip(
						"GitHub release creation is disabled. Set Release__Mode=NuGet (or GitHubRelease) and GITHUB_TOKEN to create a GitHub release."
					)
					: SkipDecision.DoNotSkip
			)
			.Build();

	protected override async Task<Release?> ExecuteAsync(IModuleContext context, CancellationToken cancellationToken)
	{
		var versionResult = await context.GetModule<VersionModule>();
		var version =
			versionResult.ValueOrDefault
			?? throw new InvalidOperationException("The version was not produced by the version module.");

		var tag = $"v{version}";

		var repositoryIdString = context.GitHub().EnvironmentVariables.RepositoryId;
		if (!long.TryParse(repositoryIdString, out var repositoryId))
		{
			throw new InvalidOperationException(
				$"Failed to parse RepositoryId '{repositoryIdString}' as a valid long integer."
			);
		}

		// Create a new release on GitHub with the specified tag and generate release notes
		return await context
			.GitHub()
			.Client.Repository.Release.Create(
				repositoryId,
				new NewRelease(tag) { Name = tag, GenerateReleaseNotes = true }
			);
	}
}
