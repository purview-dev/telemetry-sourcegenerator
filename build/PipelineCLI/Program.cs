var pipelineDirectory = PipelineProjectDirectory.Find();
var repositoryRoot = PathHelpers.FindRepositoryRoot(pipelineDirectory);

var builder = Pipeline.CreateBuilder(args);

builder
	.Configuration.AddJsonFile(Path.Combine(pipelineDirectory, "appsettings.json"), optional: false)
	.AddEnvironmentVariables()
	.AddCommandLine(args);

builder.Services.Configure<BuildSettings>(builder.Configuration.GetSection(BuildSettings.SectionName));
builder.Services.Configure<NuGetSettings>(builder.Configuration.GetSection(NuGetSettings.SectionName));
builder.Services.Configure<PublishLocalNuGetSettings>(
	builder.Configuration.GetSection(PublishLocalNuGetSettings.SectionName)
);
builder.Services.Configure<GitHubSettings>(builder.Configuration.GetSection(GitHubSettings.SectionName));
builder.Services.Configure<ReleaseSettings>(builder.Configuration.GetSection(ReleaseSettings.SectionName));

builder.Services.AddSingleton<IGitHubClient>(serviceProvider =>
{
	var settings = serviceProvider.GetRequiredService<IOptions<GitHubSettings>>();
	var accessToken = settings.Value.GetGitHubToken();

	return new GitHubClient(new(settings.Value.ProductHeader), new InMemoryCredentialStore(new(accessToken)));
});

Environment.CurrentDirectory = repositoryRoot;

builder
	.AddModule<VersionModule>()
	.AddModule<RestoreModule>()
	.AddModule<BuildModule>()
	.AddModule<LintModule>()
	.AddModule<RunTestsModule>()
	.AddModule<PackModule>()
	.AddModule<PublishNuGetModule>()
	.AddModule<PublishLocalNuGetModule>()
	.AddModule<CreateGitHubReleaseModule>();

await using var pipeline = await builder.BuildAsync();

await pipeline.RunAsync();
