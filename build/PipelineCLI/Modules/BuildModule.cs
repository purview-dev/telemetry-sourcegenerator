using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Modules;

[ModuleCategory("Build")]
[DependsOn<RestoreModule>]
public class BuildModule(IOptions<BuildSettings> settings) : Module<CommandResult>
{
	protected override async Task<CommandResult?> ExecuteAsync(
		IModuleContext context,
		CancellationToken cancellationToken
	)
	{
		return await context
			.DotNet()
			.Build(
				new()
				{
					ProjectSolution = settings.Value.Solution,
					Configuration = settings.Value.Configuration,
					NoRestore = true,
				},
				cancellationToken: cancellationToken
			);
	}
}
