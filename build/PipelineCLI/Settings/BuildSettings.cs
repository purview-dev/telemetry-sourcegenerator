using System.ComponentModel.DataAnnotations;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Settings;

public sealed class BuildSettings
{
	public const string SectionName = "Build";

	public LogLevel LogLevel { get; init; } = LogLevel.Warning;

	[Required(AllowEmptyStrings = false)]
	public string Solution { get; init; } = "src/Telemetry.SourceGenerator.slnx";

	[Required(AllowEmptyStrings = false)]
	public string Configuration { get; init; } = "Release";

	[Required(AllowEmptyStrings = false)]
	public string ArtifactsFolder { get; init; } = "artifacts";

	public bool RunTests { get; init; } = true;

	[Required(AllowEmptyStrings = false)]
	public string TestFilter { get; init; } = "/*/*/*/*/";

	/// <summary>
	/// Comma-separated list of test project file names (or glob patterns) to run.
	/// Empty or "*" runs every test project under <c>src/tests</c>.
	/// </summary>
	public string TestProjects { get; init; } = "*";

	public bool RunLint { get; init; } = true;

	public bool RunPack { get; init; } = true;
}
