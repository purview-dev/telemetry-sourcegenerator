using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CliWrap;
using CliWrap.Buffered;

namespace Purview.Telemetry.Repository;

/// <summary>
/// Guards the release gates themselves: the compiler-host leak scan must catch every kind of leak it claims to, every
/// CI job that builds or publishes an application must run it, the packed-consumer gate must stay wired into CI and the
/// Justfile, its consumers must reference the package rather than the repository, and pack validation must keep
/// forbidding compiler-host content outside <c>analyzers/</c>.
/// </summary>
/// <remarks>
/// <para>
/// A gate that passes vacuously is worse than none, because it is trusted. The leak scan
/// (<c>scripts/assert-no-compiler-host-assemblies.ps1</c>) exists because Purview.SourceGeneratorFramework 1.2.1 put
/// <c>Purview.SourceGeneratorFramework.dll</c> into the build output of every project that referenced the generator as
/// an analyzer - all eight sample bins here - while every build stayed green. These tests run the scan against crafted
/// output, so a regex or file-pattern change that stops it seeing a leak fails here rather than in a consumer's Blazor
/// WebAssembly AOT build.
/// </para>
/// <para>
/// The scan is a PowerShell script, as the CI jobs run it, so these tests run it through <c>pwsh</c> as a child
/// process. They do not skip when <c>pwsh</c> is missing: the gate cannot run without it either.
/// </para>
/// </remarks>
public sealed partial class ReleaseGateTests
{
	const string LeakScanScript = "scripts/assert-no-compiler-host-assemblies.ps1";
	const string PackedConsumersScript = "scripts/test-packed-consumers.ps1";
	const string PackRulesScript = "scripts/test-pack-validation-rules.ps1";

	static readonly Lazy<string> RepositoryRoot = new(FindRepositoryRoot);

	/// <summary>A file written into otherwise clean build output.</summary>
	public sealed record OutputFile(string Description, string RelativePath, string Content, bool Brotli = false)
	{
		public override string ToString() => Description;
	}

	public static IEnumerable<Func<OutputFile>> Leaks() =>
		[
			() => new("the generator framework assembly", "Purview.SourceGeneratorFramework.dll", "MZ"),
			() => new("a Roslyn assembly in a subfolder", "refs/Microsoft.CodeAnalysis.CSharp.dll", "MZ"),
			() => new("the generator assembly", "Purview.Telemetry.SourceGenerator.dll", "MZ"),
			() =>
				new(
					"a fingerprinted WebAssembly refactorings assembly",
					"wwwroot/_framework/Purview.Telemetry.SourceGenerator.Refactorings.ab12cd34ef.wasm",
					"wasm"
				),
			() =>
				new(
					"a compressed WebAssembly generator",
					"wwwroot/_framework/Purview.Telemetry.SourceGenerator.ab12cd34ef.wasm.br",
					"br"
				),
			() =>
				new(
					"a deps.json library entry",
					"PackedApp.deps.json",
					"""{"libraries":{"Purview.SourceGeneratorFramework/1.2.2":{"type":"package"}}}"""
				),
			() =>
				new(
					"a runtimeconfig.json reference",
					"PackedApp.runtimeconfig.json",
					"""{"runtimeOptions":{"configProperties":{"Microsoft.CodeAnalysis.Leak":true}}}"""
				),
			() =>
				new(
					"a blazor.boot.json asset",
					"wwwroot/_framework/blazor.boot.json",
					"""{"resources":{"assembly":{"Purview.Telemetry.SourceGenerator.wasm":"sha256-x"}}}"""
				),
			() =>
				new(
					"a fingerprinted dotnet.boot.js asset",
					"wwwroot/_framework/dotnet.boot.ab12cd34ef.js",
					"""export const config = {"assembly":[{"name":"Purview.Telemetry.SourceGenerator.Refactorings.wasm"}]};"""
				),
			() =>
				new(
					"a Brotli-compressed dotnet.js boot manifest",
					"wwwroot/_framework/dotnet.js.br",
					"""const config = {"assembly":[{"name":"Purview.SourceGeneratorFramework.wasm"}]};""",
					Brotli: true
				),
		];

	[Test]
	[MethodDataSource(nameof(Leaks))]
	public async Task LeakScan_GivenACompilerHostLeak_FailsAndNamesTheFile(
		OutputFile leak,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		using var output = CleanOutput();
		Write(output.Path, leak);

		// Act
		var result = await RunLeakScanAsync([output.Path], cancellationToken);

		// Assert
		await Assert
			.That(result.ExitCode)
			.IsEqualTo(1)
			.Because($"the scan missed {leak}:{Environment.NewLine}{result}");
		await Assert
			.That(result.StandardOutput)
			.Contains(Path.GetFileName(leak.RelativePath))
			.Because("a failure must name the leaking file");
	}

	[Test]
	public async Task LeakScan_GivenCleanBuildAndPublishOutput_Passes(CancellationToken cancellationToken)
	{
		// Arrange
		using var output = CleanOutput();

		// Act
		var result = await RunLeakScanAsync([output.Path], cancellationToken);

		// Assert
		await Assert
			.That(result.ExitCode)
			.IsEqualTo(0)
			.Because($"clean output was rejected:{Environment.NewLine}{result}");
		await Assert.That(result.StandardOutput).Contains("3 manifest(s)");
	}

	[Test]
	public async Task LeakScan_GivenSeveralPaths_ScansEveryOne(CancellationToken cancellationToken)
	{
		// Arrange
		using var clean = CleanOutput();
		using var leaking = CleanOutput();
		Write(leaking.Path, new("a leak in the second path", "Microsoft.CodeAnalysis.dll", "MZ"));

		// Act - space-separated, exactly as the Justfile recipe passes them through pwsh -File.
		var result = await RunLeakScanAsync([clean.Path, leaking.Path], cancellationToken);

		// Assert
		await Assert
			.That(result.ExitCode)
			.IsEqualTo(1)
			.Because($"the second path was not scanned:{Environment.NewLine}{result}");
	}

	/// <summary>
	/// A Blazor server publish can carry Microsoft's WebAssembly debugging proxy, whose expression evaluator ships
	/// Roslyn. <c>-Allow</c> exempts that folder alone: a leak anywhere else in the same output must still fail.
	/// </summary>
	[Test]
	public async Task LeakScan_GivenAnAllowedToolFolder_SkipsItButStillFailsALeakElsewhere(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		using var allowedOnly = CleanOutput();
		Write(allowedOnly.Path, new("the debug proxy's Roslyn", "BlazorDebugProxy/Microsoft.CodeAnalysis.dll", "MZ"));
		using var alsoLeaking = CleanOutput();
		Write(alsoLeaking.Path, new("the debug proxy's Roslyn", "BlazorDebugProxy/Microsoft.CodeAnalysis.dll", "MZ"));
		Write(alsoLeaking.Path, new("a real leak", "Purview.SourceGeneratorFramework.dll", "MZ"));

		// Act
		var allowed = await RunLeakScanAsync([allowedOnly.Path], cancellationToken, ["*BlazorDebugProxy/*"]);
		var leaking = await RunLeakScanAsync([alsoLeaking.Path], cancellationToken, ["*BlazorDebugProxy/*"]);

		// Assert
		await Assert
			.That(allowed.ExitCode)
			.IsEqualTo(0)
			.Because($"the allowed folder was not skipped:{Environment.NewLine}{allowed}");
		await Assert.That(allowed.StandardOutput).Contains("1 allowed by -Allow");
		await Assert
			.That(leaking.ExitCode)
			.IsEqualTo(1)
			.Because($"-Allow hid a leak outside its folder:{Environment.NewLine}{leaking}");
		await Assert.That(leaking.StandardOutput).Contains("Purview.SourceGeneratorFramework.dll");
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task LeakScan_GivenNothingToScan_FailsRatherThanPassingVacuously(
		bool directoryExists,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		using TemporaryDirectory output = new();
		var path = directoryExists ? output.Path : System.IO.Path.Combine(output.Path, "missing");

		// Act
		var result = await RunLeakScanAsync([path], cancellationToken);

		// Assert
		await Assert
			.That(result.ExitCode)
			.IsEqualTo(1)
			.Because($"scanning nothing passed:{Environment.NewLine}{result}");
	}

	/// <summary>
	/// Every CI job that publishes an application, or builds the samples, must scan what it wrote. A job added later
	/// without the scan would be a gate that cannot see the leak this repository has already shipped once - and the
	/// samples are exactly where it was.
	/// </summary>
	[Test]
	public async Task Workflows_GivenAJobThatBuildsApplications_ScanItsOutputForCompilerHostAssemblies(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var workflows = Directory.GetFiles(
			System.IO.Path.Combine(RepositoryRoot.Value, ".github", "workflows"),
			"*.yml"
		);

		// Act
		List<string> building = [];
		List<string> unscanned = [];
		foreach (var workflow in workflows)
		{
			var text = await File.ReadAllTextAsync(workflow, cancellationToken);
			foreach (var (job, body) in Jobs(text))
			{
				if (
					!body.Contains("dotnet publish", StringComparison.Ordinal)
					&& !body.Contains("dotnet build samples/", StringComparison.Ordinal)
				)
					continue;

				var name = $"{System.IO.Path.GetFileName(workflow)}:{job}";
				building.Add(name);
				if (!body.Contains("./" + LeakScanScript, StringComparison.Ordinal))
					unscanned.Add(name);
			}
		}

		// Assert
		await Assert
			.That(building)
			.IsNotEmpty()
			.Because("no workflow job builds the samples or publishes, so the job parsing is broken");
		await Assert
			.That(unscanned)
			.IsEmpty()
			.Because($"these jobs build applications without running {LeakScanScript}: {string.Join(", ", unscanned)}");
	}

	[Test]
	public async Task PrWorkflow_GivenThePackedConsumersJob_RunsTheGateAndTheRuleProof(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var text = await File.ReadAllTextAsync(
			System.IO.Path.Combine(RepositoryRoot.Value, ".github", "workflows", "pr.yml"),
			cancellationToken
		);

		// Act
		var job = Jobs(text).SingleOrDefault(static j => j.Name == "packed-consumers").Body ?? string.Empty;

		// Assert
		await Assert.That(job).Contains($"./{PackedConsumersScript} -PreflightOnly");
		await Assert.That(job).Contains("-WasmAot Require");
		await Assert.That(job).Contains($"./{PackRulesScript}");
		await Assert.That(job).Contains("dotnet workload install wasm-tools");
	}

	[Test]
	[Arguments("smoke-packed-consumers", PackedConsumersScript)]
	[Arguments("verify-pack-rules", PackRulesScript)]
	[Arguments("scan-compiler-host-leaks", LeakScanScript)]
	public async Task Justfile_GivenAReleaseGateRecipe_RunsItsScript(
		string recipe,
		string script,
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var text = await File.ReadAllTextAsync(
			System.IO.Path.Combine(RepositoryRoot.Value, "Justfile"),
			cancellationToken
		);

		// Act
		var match = Regex.Match(
			text,
			$@"^{Regex.Escape(recipe)}\b[^\n]*:\r?\n(?<body>(?:[ \t]+[^\n]*\r?\n?)+)",
			RegexOptions.Multiline
		);

		// Assert
		await Assert.That(match.Success).IsTrue().Because($"the Justfile has no '{recipe}' recipe");
		await Assert.That(match.Groups["body"].Value).Contains($"pwsh -NoProfile -File {script}");
	}

	/// <summary>
	/// The packed-consumer gate proves what ships only while its consumers reference the package: a ProjectReference
	/// to the repository's generator would hand them the unmerged generator and its bin folder instead.
	/// </summary>
	[Test]
	public async Task PackedConsumers_GivenEveryProject_ReferenceTheGeneratorAsAPackageOnly(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var consumers = System.IO.Path.Combine(RepositoryRoot.Value, "scripts", "packed-consumers");
		var projects = Directory.GetFiles(consumers, "*.csproj", SearchOption.AllDirectories);

		// Act
		List<string> problems = [];
		foreach (var project in projects)
		{
			var document = XDocument.Parse(await File.ReadAllTextAsync(project, cancellationToken));
			var name = System.IO.Path.GetRelativePath(consumers, project);

			var generator = document
				.Descendants("PackageReference")
				.SingleOrDefault(static r => (string?)r.Attribute("Include") == "Purview.Telemetry.SourceGenerator");
			if (generator is null)
				problems.Add($"{name} has no Purview.Telemetry.SourceGenerator PackageReference");
			else if (
				(string?)generator.Attribute("PrivateAssets") != "all"
				|| (string?)generator.Attribute("IncludeAssets") != "analyzers"
			)
				problems.Add(
					$"{name} does not reference the generator as the README says (PrivateAssets/IncludeAssets)"
				);

			foreach (var reference in document.Descendants("ProjectReference"))
			{
				var include = (string?)reference.Attribute("Include") ?? string.Empty;
				var resolved = System.IO.Path.GetFullPath(
					System.IO.Path.Combine(System.IO.Path.GetDirectoryName(project)!, include)
				);
				if (!resolved.StartsWith(consumers + System.IO.Path.DirectorySeparatorChar, StringComparison.Ordinal))
					problems.Add($"{name} references {include}, outside scripts/packed-consumers");
			}
		}

		// Assert
		await Assert.That(projects).IsNotEmpty();
		await Assert.That(problems).IsEmpty().Because(string.Join("; ", problems));
	}

	/// <summary>
	/// Compiler-host assemblies must stay forbidden everywhere NuGet hands files to an application, and the generator
	/// and refactorings everywhere an application loads assemblies from. <c>scripts/test-pack-validation-rules.ps1</c>
	/// proves each rule rejects a package; this proves nobody removes one.
	/// </summary>
	[Test]
	public async Task PackValidation_GivenEveryPackage_ForbidsCompilerHostContentOutsideAnalyzers(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var json = await File.ReadAllTextAsync(
			System.IO.Path.Combine(RepositoryRoot.Value, ".config", "purview-build.json"),
			cancellationToken
		);
		using var document = JsonDocument.Parse(
			json,
			new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }
		);
		string[] hostOnly = ["Purview.SourceGeneratorFramework*", "Microsoft.CodeAnalysis*"];
		string[] generatorOnly = ["Purview.Telemetry.SourceGenerator*"];
		string[] applicationFolders =
		[
			"lib",
			"ref",
			"runtimes",
			"build",
			"buildTransitive",
			"buildMultiTargeting",
			"tools",
			"content",
			"contentFiles",
			"staticwebassets",
		];
		string[] assemblyFolders = ["lib", "ref", "runtimes"];

		// Act
		var rules = document
			.RootElement.GetProperty("PackValidation")
			.GetProperty("ForbiddenContent")
			.GetProperty("*")
			.EnumerateArray()
			.Select(static rule => rule.GetString())
			.ToHashSet(StringComparer.Ordinal);

		// Assert
		string[] expected =
		[
			.. hostOnly,
			.. hostOnly.Select(static name => $"analyzers/**/{name}"),
			.. applicationFolders.SelectMany(folder => hostOnly.Select(name => $"{folder}/**/{name}")),
			.. assemblyFolders.SelectMany(folder => generatorOnly.Select(name => $"{folder}/**/{name}")),
		];
		var missing = expected.Where(rule => !rules.Contains(rule)).ToArray();
		await Assert
			.That(missing)
			.IsEmpty()
			.Because($"ForbiddenContent[\"*\"] no longer forbids: {string.Join(", ", missing)}");
	}

	/// <summary>
	/// The merge that makes the shipped analyzers self-contained reports a public type exposing a
	/// Purview.SourceGeneratorFramework type (PSGFR41) as a plain message by default, which is how
	/// <c>MessageTemplateHole</c> shipped. As an error it fails the build, so the setting must not be dropped.
	/// </summary>
	[Test]
	public async Task BuildProps_GivenTheMergedAnalyzers_FailTheBuildOnAPublicSurfaceLeak(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		var document = XDocument.Parse(
			await File.ReadAllTextAsync(
				System.IO.Path.Combine(RepositoryRoot.Value, "src", "Directory.Build.props"),
				cancellationToken
			)
		);

		// Act
		var severities = document
			.Descendants("PurviewMergePublicSurfaceSeverity")
			.Select(static element => element.Value.Trim())
			.ToArray();
		var disabled = document.Descendants("PurviewMergePublicSurfaceValidation").Select(static e => e.Value.Trim());

		// Assert
		await Assert.That(severities).IsEquivalentTo(["error"]);
		await Assert.That(disabled).DoesNotContain("false");
	}

	static TemporaryDirectory CleanOutput()
	{
		TemporaryDirectory output = new();
		Write(output.Path, new("the app", "PackedApp.dll", "MZ"));
		Write(
			output.Path,
			new(
				"clean deps.json",
				"PackedApp.deps.json",
				"""{"libraries":{"Microsoft.Extensions.Logging/10.0.13":{"type":"package"},"Microsoft.Extensions.Telemetry.Abstractions/10.11.0":{"type":"package"}}}"""
			)
		);
		Write(output.Path, new("clean runtimeconfig", "PackedApp.runtimeconfig.json", """{"runtimeOptions":{}}"""));
		Write(
			output.Path,
			new(
				"clean boot manifest",
				"wwwroot/_framework/dotnet.js",
				"""const config = {"assembly":[{"name":"PackedApp.wasm"},{"name":"PackedTelemetry.Library.wasm"}]};"""
			)
		);
		Write(output.Path, new("the runtime", "wwwroot/_framework/dotnet.native.ab12cd34ef.js", "// runtime"));
		return output;
	}

	static void Write(string root, OutputFile file)
	{
		var path = System.IO.Path.Combine(root, file.RelativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
		Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
		var bytes = Encoding.UTF8.GetBytes(file.Content);
		if (!file.Brotli)
		{
			File.WriteAllBytes(path, bytes);
			return;
		}

		using var stream = File.Create(path);
		using BrotliStream brotli = new(stream, CompressionLevel.Optimal);
		brotli.Write(bytes);
	}

	static async Task<ScanResult> RunLeakScanAsync(
		string[] paths,
		CancellationToken cancellationToken,
		string[]? allow = null
	)
	{
		var result = await Cli.Wrap("pwsh")
			.WithArguments([
				"-NoProfile",
				"-NonInteractive",
				"-File",
				System.IO.Path.Combine(RepositoryRoot.Value, LeakScanScript),
				"-Path",
				.. paths,
				.. AllowArguments(allow),
			])
			.WithEnvironmentVariables(static env => env.Set("GITHUB_ACTIONS", null))
			.WithValidation(CommandResultValidation.None)
			.ExecuteBufferedAsync(cancellationToken);

		return new(result.ExitCode, result.StandardOutput, result.StandardError);
	}

	static string[] AllowArguments(string[]? allow) => allow is null ? [] : ["-Allow", .. allow];

	sealed record ScanResult(int ExitCode, string StandardOutput, string StandardError)
	{
		public override string ToString() => $"exit {ExitCode}{Environment.NewLine}{StandardOutput}{StandardError}";
	}

	/// <summary>The jobs of a workflow, split on their two-space-indented keys under <c>jobs:</c>.</summary>
	static IEnumerable<(string Name, string Body)> Jobs(string workflow)
	{
		var jobs = workflow.IndexOf("\njobs:", StringComparison.Ordinal);
		if (jobs < 0)
			yield break;

		var headers = JobHeader().Matches(workflow[jobs..]);
		for (var i = 0; i < headers.Count; i++)
		{
			var start = headers[i].Index;
			var end = i + 1 < headers.Count ? headers[i + 1].Index : workflow.Length - jobs;
			yield return (headers[i].Groups["name"].Value, workflow.Substring(jobs + start, end - start));
		}
	}

	[GeneratedRegex(@"^  (?<name>[A-Za-z0-9_-]+):[ \t]*\r?$", RegexOptions.Multiline)]
	private static partial Regex JobHeader();

	static string FindRepositoryRoot()
	{
		for (
			var directory = new DirectoryInfo(AppContext.BaseDirectory);
			directory is not null;
			directory = directory.Parent
		)
		{
			if (
				File.Exists(System.IO.Path.Combine(directory.FullName, "AGENTS.md"))
				&& File.Exists(System.IO.Path.Combine(directory.FullName, LeakScanScript))
			)
				return directory.FullName;
		}

		throw new InvalidOperationException(
			$"No repository root above '{AppContext.BaseDirectory}' carries both AGENTS.md and {LeakScanScript}."
		);
	}

	sealed class TemporaryDirectory : IDisposable
	{
		public string Path { get; } =
			Directory
				.CreateDirectory(
					System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"tsg-leak-scan-{Guid.NewGuid():N}")
				)
				.FullName;

		public void Dispose()
		{
			try
			{
				Directory.Delete(Path, recursive: true);
			}
			catch (IOException) { }
		}
	}
}
