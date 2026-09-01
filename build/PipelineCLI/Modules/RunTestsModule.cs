using System.Diagnostics;
using System.Text.RegularExpressions;
using ModularPipelines.Attributes;
using ModularPipelines.Configuration;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;
using ModularPipelines.Modules;

namespace Purview.Aspire.ResourceKit.PipelineCLI.Modules;

[ModuleCategory("Build")]
[DependsOn<BuildModule>]
public class RunTestsModule(IOptions<BuildSettings> settings) : Module<CommandResult[]>
{
	protected override ModuleConfiguration Configure() =>
		ModuleConfiguration
			.Create()
			.WithSkipWhen(_ =>
				settings.Value.RunTests
					? SkipDecision.DoNotSkip
					: SkipDecision.Skip("Tests are disabled. Set Build__RunTests=true to run them.")
			)
			.Build();

	protected override async Task<CommandResult[]?> ExecuteAsync(
		IModuleContext context,
		CancellationToken cancellationToken
	)
	{
		var testProjects = FilterTestProjects(
			Directory.EnumerateFiles("src/tests", "*Tests.csproj", SearchOption.AllDirectories).ToList(),
			settings.Value.TestProjects
		);
		if (testProjects.Count == 0)
		{
			context.Logger.LogWarning(
				"No test projects matched 'src/tests' (filter: {TestProjects}), despite tests being enabled. Skipping test execution.",
				settings.Value.TestProjects
			);

			return [];
		}

		var timings = new List<(string Project, TimeSpan Elapsed, int ExitCode)>();

		var tasks = testProjects.Select(async project =>
		{
			var stopwatch = Stopwatch.StartNew();
			var result = await context
				.DotNet()
				.Test(
					new DotNetTestOptions
					{
						Project = project,
						Configuration = settings.Value.Configuration,
						NoBuild = true,
						NoRestore = true,
						Arguments = ["--ignore-exit-code", "8", "--treenode-filter", settings.Value.TestFilter],
					},
					cancellationToken: cancellationToken
				);
			stopwatch.Stop();

			lock (timings)
				timings.Add((project, stopwatch.Elapsed, result.ExitCode));

			return result;
		});

		var results = await Task.WhenAll(tasks);

		context.Logger.LogInformation(
			"Test run timings:{NewLine}{Timings}",
			Environment.NewLine,
			string.Join(
				Environment.NewLine,
				timings
					.OrderByDescending(t => t.Elapsed)
					.Select(t => $"  {Path.GetFileName(t.Project)}: {t.Elapsed.TotalSeconds:F1}s (exit {t.ExitCode})")
			)
		);

		return results;
	}

	static IReadOnlyList<string> FilterTestProjects(IReadOnlyList<string> projects, string filter)
	{
		if (string.IsNullOrWhiteSpace(filter) || filter.Trim() == "*")
			return projects;

		var patterns = filter
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(ToRegexPattern)
			.ToArray();

		return projects
			.Where(project =>
			{
				var fileName = Path.GetFileName(project);
				return patterns.Any(pattern => Regex.IsMatch(fileName, pattern, RegexOptions.IgnoreCase));
			})
			.ToList();
	}

	static string ToRegexPattern(string entry)
	{
		if (entry.Contains('*', StringComparison.Ordinal))
		{
			var escaped = Regex.Escape(entry);
			return "^" + escaped.Replace("\\*", ".*", StringComparison.Ordinal) + "$";
		}

		return "^" + Regex.Escape(entry) + "$";
	}
}
