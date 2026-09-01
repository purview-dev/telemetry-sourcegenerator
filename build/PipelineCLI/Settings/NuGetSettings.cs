using ModularPipelines.Attributes;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Settings;

public sealed record NuGetSettings
{
	public const string SectionName = "NuGet";

	[SecretValue]
	public string? APIKey { get; set; }

	[SecretValue]
	[ConfigurationKeyName("NUGET_APIKEY")]
	public string? EnvAPIKey { get; set; }

	public string FeedUrl { get; init; } = "https://api.nuget.org/v3/index.json";

	public string? GetNuGetAPIKey() =>
		!string.IsNullOrWhiteSpace(APIKey) ? APIKey
		: !string.IsNullOrWhiteSpace(EnvAPIKey) ? EnvAPIKey
		: null;
}
