using System.ComponentModel.DataAnnotations;
using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.Models;
using ModularPipelines.Modules;
using NuGet.Versioning;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Modules;

[ModuleCategory("Build")]
[DependsOn<PackModule>]
public class PublishLocalNuGetModule(
	IOptions<PublishLocalNuGetSettings> localNuGetFeedSettings,
	IOptions<ReleaseSettings> releaseSettings,
	IOptions<BuildSettings> buildSettings
) : Module<CommandResult>
{
	protected override ModuleConfiguration Configure() =>
		ModuleConfiguration
			.Create()
			.WithSkipWhen(ctx =>
				!ctx.IsRunningLocally() || releaseSettings.Value.Mode != ReleaseMode.LocalNuGet
					? SkipDecision.Skip(
						"Local NuGet Feed publishing is disabled. Run the pipeline locally with Release__Mode=LocalNuGet to enable it."
					)
					: SkipDecision.DoNotSkip
			)
			.Build();

	protected override async Task<CommandResult?> ExecuteAsync(
		IModuleContext context,
		CancellationToken cancellationToken
	)
	{
		var localFeedPath = localNuGetFeedSettings.Value.LocalFeedPath;

		var validationResults = new List<ValidationResult>();
		var validationContext = new ValidationContext(localNuGetFeedSettings.Value);
		if (
			!Validator.TryValidateObject(
				localNuGetFeedSettings.Value,
				validationContext,
				validationResults,
				validateAllProperties: true
			)
		)
		{
			foreach (var validationResult in validationResults)
				context.Logger.LogError("{Message}", validationResult.ErrorMessage);

			throw new InvalidOperationException(
				$"Invalid {nameof(PublishLocalNuGetSettings)} configuration for {nameof(PublishLocalNuGetSettings.LocalFeedPath)}. "
					+ "Windows paths with backslashes may have been stripped by the shell; "
					+ "use forward slashes, e.g. --PublishLocalNuGet:LocalFeedPath=p:/_sync-projects/.local-nuget/."
			);
		}

		var fullLocalFeedPath = Path.GetFullPath(localFeedPath);
		context.Logger.LogInformation("Publishing local NuGet packages to {LocalFeedPath}.", fullLocalFeedPath);

		if (!Directory.Exists(fullLocalFeedPath))
			Directory.CreateDirectory(fullLocalFeedPath);

		var packages = Directory
			.GetFiles(buildSettings.Value.ArtifactsFolder, "*.nupkg")
			.Concat(Directory.GetFiles(buildSettings.Value.ArtifactsFolder, "*.snupkg"))
			.ToArray();
		if (packages.Length == 0)
		{
			throw new InvalidOperationException(
				$"No packages found in {buildSettings.Value.ArtifactsFolder}. The local feed was not populated."
			);
		}

		List<PackageDetails> nupkgPackages = [];
		foreach (var package in packages)
		{
			var fileName = Path.GetFileName(package);
			var destinationPath = Path.Combine(fullLocalFeedPath, fileName);

			if (Path.GetExtension(fileName) == ".nupkg")
				nupkgPackages.Add(await ParsePackageDetailsAsync(package, cancellationToken));

			if (!localNuGetFeedSettings.Value.OverwriteExistingPackages && File.Exists(destinationPath))
			{
				context.Logger.LogInformation("Package {Package} already exists in local feed. Skipping.", fileName);
				File.Delete(package);

				continue;
			}

			File.Move(package, destinationPath, true);
			context.Logger.LogInformation("Copied package {Package} to local feed.", fileName);
		}

		if (localNuGetFeedSettings.Value.ClearPackageCache)
		{
			context.Logger.LogInformation("Clearing local NuGet package cache...");

			var globalPackagesResult = await context.Shell.Command.ExecuteCommandLineTool(
				DotNetCLIOptions.Create("nuget", "locals", "global-packages", "--list"),
				cancellationToken: cancellationToken
			);
			if (globalPackagesResult.ExitCode != 0)
				return globalPackagesResult;

			var httpCacheResult = await context.Shell.Command.ExecuteCommandLineTool(
				DotNetCLIOptions.Create("nuget", "locals", "http-cache", "--list"),
				cancellationToken: cancellationToken
			);
			if (httpCacheResult.ExitCode != 0)
				return httpCacheResult;

			var globalPackagePaths = globalPackagesResult
				.StandardOutput.Replace("global-packages: ", "", StringComparison.Ordinal)
				.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
				.Where(Directory.Exists);

			var httpCachePaths = httpCacheResult
				.StandardOutput.Replace("http-cache: ", "", StringComparison.Ordinal)
				.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
				.Where(Directory.Exists);

			foreach (var artifact in nupkgPackages)
			{
#pragma warning disable CA1308 // Normalize strings to uppercase
				var loweredPackageId = artifact.PackageId.ToLowerInvariant();
				var loweredVersion = artifact.Version.ToFullString().ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase

				foreach (var globalPath in globalPackagePaths)
				{
					var packagePath = Path.Combine(globalPath, loweredPackageId, artifact.Version.ToFullString());
					if (Directory.Exists(packagePath))
					{
						Directory.Delete(packagePath, true);
						context.Logger.LogInformation(
							"Deleted package {Package} version {Version} from global packages cache.",
							artifact.PackageId,
							artifact.Version
						);
					}
				}
				foreach (var httpCachePath in httpCachePaths)
				{
					string[] packagePaths =
					[
						Path.Combine(httpCachePath, "list_" + loweredPackageId + ".dat"),
						Path.Combine(httpCachePath, "list_" + loweredPackageId + "_index.dat"),
						Path.Combine(httpCachePath, "list_" + loweredPackageId + "_range_*.dat"),
						Path.Combine(httpCachePath, "nupkg_" + loweredPackageId + "." + loweredVersion + ".dat"),
					];

					foreach (var path in packagePaths)
					{
						var directory = Path.GetDirectoryName(path);
						var pattern = Path.GetFileName(path);
						foreach (var file in Directory.EnumerateFiles(directory!, pattern, SearchOption.AllDirectories))
						{
							File.Delete(file);
							context.Logger.LogInformation(
								"Deleted package {Package} version {Version} from HTTP cache.",
								artifact.PackageId,
								artifact.Version
							);
						}
					}
				}
			}
		}

		if (localNuGetFeedSettings.Value.ShutdownDotnetBuilderServer)
		{
			context.Logger.LogInformation("Shutting down dotnet builder server...");

			return await context.Shell.Command.ExecuteCommandLineTool(
				DotNetCLIOptions.Create("build-server", "shutdown"),
				cancellationToken: cancellationToken
			);
		}

		return null;
	}

	static async Task<PackageDetails> ParsePackageDetailsAsync(string artifact, CancellationToken cancellationToken)
	{
		using var packageReader = new NuGet.Packaging.PackageArchiveReader(artifact);
		var packaging = await packageReader.GetNuspecReaderAsync(cancellationToken);

		return new(packaging.GetId(), packaging.GetVersion());
	}
}

record struct PackageDetails(string PackageId, NuGetVersion Version);
