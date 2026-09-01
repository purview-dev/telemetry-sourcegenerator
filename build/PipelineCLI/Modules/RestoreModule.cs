using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace Purview.Telemetry.SourceGenerator.PipelineCLI.Modules;

[ModuleCategory("Build")]
public class RestoreModule(IOptions<BuildSettings> settings) : Module<CommandResult>
{
	protected override async Task<CommandResult?> ExecuteAsync(
		IModuleContext context,
		CancellationToken cancellationToken
	)
	{
		return await context
			.DotNet()
			.Restore(
				new DotNetRestoreOptions { ProjectSolution = settings.Value.Solution },
				cancellationToken: cancellationToken
			);
	}
}
