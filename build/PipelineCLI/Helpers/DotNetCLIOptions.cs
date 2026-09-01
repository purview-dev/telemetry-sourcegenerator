using ModularPipelines.Options;

namespace Purview.Telemetry.SourceGenerator.PipelineCLI.Helpers;

public sealed record DotNetCLIOptions : CommandLineToolOptions
{
	public static DotNetCLIOptions Create(params string[] commandParts) =>
		new() { Tool = "dotnet", CommandParts = commandParts };
}
