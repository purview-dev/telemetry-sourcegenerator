namespace Purview.Telemetry.SourceGeneratorPipelineCLI.Settings;

public enum ReleaseMode
{
	None,

	NuGet,

	GitHubRelease,

	LocalNuGet,
}

public sealed record ReleaseSettings
{
	public const string SectionName = "Release";

	public ReleaseMode Mode { get; set; } = ReleaseMode.None;
}
