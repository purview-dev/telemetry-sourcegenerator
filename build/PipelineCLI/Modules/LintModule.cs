using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Modules;

[ModuleCategory("Build")]
public sealed class LintModule(IOptions<BuildSettings> settings) : Module<CommandResult>
{
	protected override ModuleConfiguration Configure() =>
		ModuleConfiguration
			.Create()
			.WithSkipWhen(_ =>
				settings.Value.RunLint
					? SkipDecision.DoNotSkip
					: SkipDecision.Skip("Linting is disabled. Set Build__RunLint=true to enable it.")
			)
			.Build();

	protected override async Task<CommandResult?> ExecuteAsync(
		IModuleContext context,
		CancellationToken cancellationToken
	)
	{
		var repositoryRoot = PathHelpers.FindRepositoryRoot();
		var dotnet = context.DotNet();
		var restoreResult = await dotnet.Tool.Restore(
			new() { Interactive = false, ToolManifest = Path.Combine(repositoryRoot, ".config", "dotnet-tools.json") },
			new() { WorkingDirectory = repositoryRoot },
			cancellationToken
		);
		if (restoreResult.ExitCode != 0)
			return restoreResult;

		// Restore worked, now run the linter
		return await context.Shell.Command.ExecuteCommandLineTool(
			DotNetCLIOptions.Create("tool", "run", "csharpier", "check", repositoryRoot),
			new() { WorkingDirectory = repositoryRoot },
			cancellationToken: cancellationToken
		);
	}
}
