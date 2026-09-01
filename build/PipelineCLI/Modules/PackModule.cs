using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Modules;

[ModuleCategory("Build")]
[DependsOn<RunTestsModule>]
[DependsOn<VersionModule>]
public sealed class PackModule(IOptions<BuildSettings> settings, IOptions<ReleaseSettings> releaseSettings)
	: Module<CommandResult>
{
	protected override ModuleConfiguration Configure() =>
		ModuleConfiguration
			.Create()
			.WithSkipWhen(_ =>
				!settings.Value.RunPack || releaseSettings.Value.Mode == ReleaseMode.None
					? SkipDecision.Skip(
						"Packing is disabled. Set Build__RunPack=true and Release__Mode to something other than None to enable it."
					)
					: SkipDecision.DoNotSkip
			)
			.Build();

	protected override async Task<CommandResult?> ExecuteAsync(
		IModuleContext context,
		CancellationToken cancellationToken
	)
	{
		var versionResult = await context.GetModule<VersionModule>();
		var nugetVersion =
			versionResult.ValueOrDefault
			?? throw new InvalidOperationException("The version was not produced by the version module.");

		Directory.CreateDirectory(settings.Value.ArtifactsFolder);

		var version = nugetVersion.ToString();
		return await context
			.DotNet()
			.Pack(
				new DotNetPackOptions
				{
					ProjectSolution = settings.Value.Solution,
					Configuration = settings.Value.Configuration,
					Output = settings.Value.ArtifactsFolder,
					Properties = [("PackageVersion", version), ("Version", version)],
				},
				cancellationToken: cancellationToken
			);
	}
}
