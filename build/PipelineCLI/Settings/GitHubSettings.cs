using ModularPipelines.Attributes;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Settings;

public sealed record GitHubSettings
{
	public const string SectionName = "GitHub";

	[SecretValue]
	public string? AccessToken { get; init; }

	[SecretValue]
	[ConfigurationKeyName("GITHUB_TOKEN")]
	public string? EnvAccessToken { get; init; }

	public string ProductHeader { get; init; } = "Purview.Telemetry.SourceGenerator.Pipeline";

	public string? GetGitHubToken()
	{
		if (!string.IsNullOrWhiteSpace(AccessToken))
			return AccessToken;

		if (!string.IsNullOrWhiteSpace(EnvAccessToken))
			return EnvAccessToken;

		// GitHub Actions provisions the automatic GITHUB_TOKEN as a plain environment variable.
		// The config binder keys it under the "GitHub" section (GitHub:GITHUB_TOKEN), which the
		// standard GITHUB_TOKEN env var does not map to, so read it directly as a fallback.
		var processToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
		return string.IsNullOrWhiteSpace(processToken) ? null : processToken;
	}
}
