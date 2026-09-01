using ModularPipelines.Options;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Helpers;

public sealed record DotNetCLIOptions : CommandLineToolOptions
{
	public static DotNetCLIOptions Create(params string[] commandParts) =>
		new() { Tool = "dotnet", CommandParts = commandParts };
}
