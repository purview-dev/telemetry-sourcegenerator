using System.Text.Json;
using ModularPipelines.Attributes;
using ModularPipelines.Context;
using ModularPipelines.Modules;
using NuGet.Versioning;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Modules;

[ModuleCategory("Build")]
public class VersionModule : Module<NuGetVersion>
{
	protected override async Task<NuGetVersion?> ExecuteAsync(
		IModuleContext context,
		CancellationToken cancellationToken
	)
	{
		var packageJsonPath = Path.Combine(Environment.CurrentDirectory, "package.json");

		if (!File.Exists(packageJsonPath))
			throw new FileNotFoundException($"Could not find package.json at {packageJsonPath}");

		var packageJson = await File.ReadAllTextAsync(packageJsonPath, cancellationToken);

		using var document = JsonDocument.Parse(packageJson);
		var version = document.RootElement.GetProperty("version").GetString();

		if (string.IsNullOrWhiteSpace(version))
			throw new InvalidOperationException("The version field in package.json is missing or empty.");

		if (!NuGetVersion.TryParse(version, out var nugetVersion))
			throw new InvalidOperationException($"The version '{version}' in package.json is not a valid SemVer.");

		context.Summary.KeyValue("Version", "Package version", version);
		return nugetVersion;
	}
}
