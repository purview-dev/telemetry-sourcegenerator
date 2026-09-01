using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Modules;

[ModuleCategory("Release")]
[DependsOn<PackModule>]
[DependsOn<RunTestsModule>]
public class PublishNuGetModule(
	IOptions<BuildSettings> buildSettings,
	IOptions<NuGetSettings> nugetSettings,
	IOptions<ReleaseSettings> releaseSettings
) : Module<CommandResult[]>
{
	protected override ModuleConfiguration Configure() =>
		ModuleConfiguration
			.Create()
			.WithSkipWhen(_ =>
				releaseSettings.Value.Mode != ReleaseMode.NuGet
				|| string.IsNullOrWhiteSpace(nugetSettings.Value.GetNuGetAPIKey())
					? SkipDecision.Skip(
						"NuGet publishing is disabled. Set Release__Mode=NuGet and NuGet__ApiKey (or NUGET_APIKEY) to publish packages to nuget.org."
					)
					: SkipDecision.DoNotSkip
			)
			.Build();

	protected override async Task<CommandResult[]?> ExecuteAsync(
		IModuleContext context,
		CancellationToken cancellationToken
	)
	{
		var artifactsFolder = buildSettings.Value.ArtifactsFolder;
		if (!Directory.Exists(artifactsFolder))
		{
			throw new InvalidOperationException(
				$"The artifacts folder '{artifactsFolder}' does not exist. "
					+ "Ensure the pack step ran (Release__Mode must not be None) before publishing."
			);
		}

		var packages = Directory.EnumerateFiles(artifactsFolder, "*.nupkg", SearchOption.TopDirectoryOnly).ToList();

		if (packages.Count == 0)
		{
			throw new InvalidOperationException($"No NuGet packages found in {buildSettings.Value.ArtifactsFolder}.");
		}

		var tasks = packages.Select(package =>
			context
				.DotNet()
				.Nuget.Push(
					new()
					{
						Path = package,
						Source = nugetSettings.Value.FeedUrl,
						ApiKey = nugetSettings.Value.GetNuGetAPIKey(),
						SkipDuplicate = true,
					},
					cancellationToken: cancellationToken
				)
		);

		return await Task.WhenAll(tasks);
	}
}
