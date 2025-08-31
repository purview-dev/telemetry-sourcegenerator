using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Purview.Telemetry.SourceGenerator.BuildTools;
using Purview.Telemetry.SourceGenerator.Helpers;

namespace Purview.Telemetry.SourceGenerator;

public abstract class IncrementalSourceGeneratorTestBase<TGenerator>
	: SourceGeneratorTestBase<ISourceGenerator>
	where TGenerator : class, IIncrementalGenerator
{
	protected IncrementalSourceGeneratorTestBase(
		ITestOutputHelper? testOutputHelper = null,
		bool throwOnLoggedOnError = true
	)
		: base(testOutputHelper, throwOnLoggedOnError)
	{
		ThrowOnLoggedOnError = throwOnLoggedOnError;
	}

	protected override ISourceGenerator Generator
	{
		get
		{
			var obj = Activator.CreateInstance<TGenerator>();
			ConfigureGenerator(obj);

			return obj.AsSourceGenerator();
		}
	}
}

public abstract class SourceGeneratorTestBase<TGenerator>(
	ITestOutputHelper? testOutputHelper = null,
	bool throwOnLoggedOnError = true
)
	where TGenerator : ISourceGenerator
{
	protected virtual bool ThrowOnLoggedOnError { get; set; } = throwOnLoggedOnError;

	protected virtual TGenerator Generator
	{
		get
		{
			var obj = Activator.CreateInstance<TGenerator>();
			ConfigureGenerator(obj);

			return obj;
		}
	}

	protected void ConfigureGenerator(object generator)
	{
		ArgumentNullException.ThrowIfNull(generator);

		GuardGenerator(generator);

		if (generator is ILogSupport logging && testOutputHelper is not null)
		{
			logging.SetLogOutput(
				(message, outputType) =>
				{
					var prefix = outputType switch
					{
						OutputType.Debug => "DBG",
						OutputType.Diagnostic => "DIA",
						OutputType.Warning => "WRN",
						OutputType.Error => "ERR",
						_ => "???",
					};

					testOutputHelper.WriteLine($"{prefix}: {message}");

					if (ThrowOnLoggedOnError)
						outputType.ShouldNotBe(OutputType.Error, message);
				}
			);
		}
	}

	protected static AdditionalText Text(string content, bool autoIncludeUsingStatements = true) =>
		new InMemoryAdditionalText(
			$"{Guid.NewGuid()}",
			"#pragma warning disable 8019 // Unnecessary using directive"
				+ Environment.NewLine
				+ (autoIncludeUsingStatements ? TestHelpers.DefaultUsingSet : "")
				+ content
		);

	protected static AdditionalText Text(
		string path,
		string content,
		bool autoIncludeUsingStatements = true
	) =>
		new InMemoryAdditionalText(
			path,
			(autoIncludeUsingStatements ? TestHelpers.DefaultUsingSet : "") + content
		);

	protected static AdditionalText[] Texts(params (string path, string content)[] pairs) =>
		[.. pairs.Select(pair => new InMemoryAdditionalText(pair.path, pair.content))];

	protected static AdditionalText[] Texts(
		params (string path, string content, (string key, string value)[]? options)[] pairs
	) =>
		[
			.. pairs.Select(pair => new InMemoryAdditionalText(
				pair.path,
				pair.content,
				pair.options
			)),
		];

	protected static ImmutableDictionary<string, string> Options(
		params (string key, string value)[] pairs
	) => pairs.ToImmutableDictionary(pair => pair.key, pair => pair.value);

	protected async Task<Compilation> GetCompilationAsync(
		GenerationResult generationResult,
		CancellationToken cancellationToken = default
	)
	{
		ArgumentNullException.ThrowIfNull(generationResult);

		List<SyntaxTree> nodes = [];
		foreach (var tree in generationResult.DriverResult.GeneratedTrees)
			nodes.Add((await tree.GetRootAsync(cancellationToken)).SyntaxTree);

		return generationResult.Compilation.AddSyntaxTrees(nodes);
	}

	protected Type GetType(GenerationResult result, string typeName)
	{
		var assembly = GetAssembly(result);

		return assembly.GetType(typeName, true).ShouldNotBeNull();
	}

	protected Assembly GetAssembly(GenerationResult result)
	{
		ArgumentNullException.ThrowIfNull(result);

		Assembly assembly;
		using (var stream = new MemoryStream())
		{
			var emitResult = result.Compilation.Emit(stream);
			emitResult.ShouldNotBeNull();
			emitResult.Success.ShouldBeTrue(string.Join("\n", emitResult.Diagnostics));

			assembly = Assembly.Load(stream.GetBuffer());
		}

		return assembly;
	}

	protected async Task<GenerationResult> GenerateAsync(
		string csharpDocument,
		AdditionalText[]? additionalTexts = null,
		ImmutableDictionary<string, string>? globalOptions = null,
		Func<Project, Project>? projectModifier = null,
		bool disableDependencyInjection = true,
		bool autoIncludeUsingStatements = true,
		IncludeLoggerTypes includeLoggerTypes = IncludeLoggerTypes.LoggerOnly,
		bool debugLog = true
	)
	{
		return await GenerateAsync(
			Text(csharpDocument, autoIncludeUsingStatements: autoIncludeUsingStatements),
			additionalTexts,
			globalOptions,
			projectModifier,
			disableDependencyInjection,
			includeLoggerTypes,
			debugLog
		);
	}

	protected async Task<GenerationResult> GenerateAsync(
		AdditionalText csharpDocument,
		AdditionalText[]? additionalTexts = null,
		ImmutableDictionary<string, string>? globalOptions = null,
		Func<Project, Project>? projectModifier = null,
		bool disableDependencyInjection = true,
		IncludeLoggerTypes includeLoggerTypes = IncludeLoggerTypes.LoggerOnly,
		bool debugLog = true
	)
	{
		return await GenerateAsync(
			[csharpDocument],
			additionalTexts,
			globalOptions,
			projectModifier,
			disableDependencyInjection,
			includeLoggerTypes,
			debugLog
		);
	}

	protected async Task<GenerationResult> GenerateAsync(
		AdditionalText[] csharpDocuments,
		AdditionalText[]? additionalTexts = null,
		ImmutableDictionary<string, string>? globalOptions = null,
		Func<Project, Project>? projectModifier = null,
		bool disableDependencyInjection = true,
		IncludeLoggerTypes includeLoggerTypes = IncludeLoggerTypes.LoggerOnly,
		bool debugLog = true
	)
	{
		List<string> preprocessorSymbols = [];
		if (includeLoggerTypes == IncludeLoggerTypes.None)
			preprocessorSymbols.Add("EXCLUDE_PURVIEW_TELEMETRY_LOGGING");

		CSharpParseOptions parseOptions = new(
			documentationMode: DocumentationMode.Parse,
			kind: SourceCodeKind.Regular,
			preprocessorSymbols: preprocessorSymbols
		);

		globalOptions ??= ImmutableDictionary<string, string>.Empty;
		if (debugLog)
			globalOptions = globalOptions.SetItem("purview_debug_log", "true");

		globalOptions = globalOptions.SetItem("CompilerVersion", "v4.7");

		var optionsProvider = TestAnalyzerConfigOptionsProvider.Empty.WithGlobalOptions(
			new TestAnalyzerConfigOptions(globalOptions)
		);

		if (disableDependencyInjection)
		{
			csharpDocuments =
			[
				.. csharpDocuments,
				Text(
					"[assembly: Purview.Telemetry.TelemetryGeneration(GenerateDependencyExtension = false)]",
					autoIncludeUsingStatements: false
				),
			];
		}

		if (additionalTexts is not null && additionalTexts.Length != 0)
		{
			var map = ImmutableDictionary.CreateBuilder<object, AnalyzerConfigOptions>();
			foreach (var text in additionalTexts)
			{
				if (text is InMemoryAdditionalText mem)
					map.Add(text, mem.GetOptions());
			}

			optionsProvider = optionsProvider.WithAdditionalTreeOptions(map.ToImmutable());
		}

		var driver = CSharpGeneratorDriver.Create(
			[Generator],
			additionalTexts: additionalTexts,
			parseOptions: parseOptions,
			optionsProvider: optionsProvider
		);
		(var _, var compilation) = await ObtainProjectAndCompilationAsync(
			projectModifier,
			csharpDocuments,
			includeLoggerTypes
		);

		var result = driver.RunGeneratorsAndUpdateCompilation(
			compilation,
			out var outputCompilation,
			out var diagnostics
		);
		if (testOutputHelper is object)
		{
			foreach (var d in diagnostics)
				testOutputHelper.WriteLine(d.ToString());
		}

		var runResult = result.GetRunResult();

		runResult.Results.Where(m => m.Exception != null).Select(m => m.Exception).ShouldBeEmpty();

		return new(runResult, diagnostics, outputCompilation);
	}

	static void GuardGenerator(object generator)
	{
		var generatorType = generator.GetType();

		if (!generatorType.IsDefined(typeof(GeneratorAttribute)))
		{
			throw new InvalidOperationException(
				$"Type is not marked [Generator]: {generatorType}."
			);
		}
	}

	protected virtual bool ReferenceCore => true;

	protected async Task<(
		Project Project,
		Compilation Compilation
	)> ObtainProjectAndCompilationAsync(
		Func<Project, Project>? projectModifier = null,
		AdditionalText[]? csharpDocuments = null,
		IncludeLoggerTypes includeLoggerTypes = IncludeLoggerTypes.LoggerOnly
	)
	{
		using AdhocWorkspace workspace = new();
		var project = workspace.AddProject(
			typeof(SourceGeneratorTestBase<>).Namespace,
			LanguageNames.CSharp
		);

		project = project
			.WithCompilationOptions(
				project.CompilationOptions!.WithOutputKind(OutputKind.DynamicallyLinkedLibrary)
			)
			.AddMetadataReference(
				MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location)
			);

		if (csharpDocuments?.Length > 0)
		{
			foreach (var csDoc in csharpDocuments)
				project = project.AddDocument(csDoc.Path, csDoc.GetText()!).Project;
		}

		project = SetupProject(project);

		if (ReferenceCore)
		{
			project = project
				.AddMetadataReference(
					MetadataReference.CreateFromFile(
						Assembly
							.Load(
								"netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51"
							)
							.Location
					)
				)
				.AddMetadataReference(
					MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location)
				)
				.AddMetadataReference(
					MetadataReference.CreateFromFile(
						typeof(System.ComponentModel.EditorBrowsableAttribute).Assembly.Location
					)
				)
				.AddMetadataReference(
					MetadataReference.CreateFromFile(typeof(IServiceProvider).Assembly.Location)
				)
				.AddMetadataReference(
					MetadataReference.CreateFromFile(
						typeof(System.Diagnostics.Activity).Assembly.Location
					)
				)
				.AddMetadataReference(
					MetadataReference.CreateFromFile(
						typeof(System.Diagnostics.Metrics.Meter).Assembly.Location
					)
				)
				.AddMetadataReference(
					MetadataReference.CreateFromFile(
						typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection)
							.Assembly
							.Location
					)
				);

			if (includeLoggerTypes >= IncludeLoggerTypes.LoggerOnly)
			{
				project = project.AddMetadataReference(
					MetadataReference.CreateFromFile(
						typeof(Microsoft.Extensions.Logging.LogLevel).Assembly.Location
					)
				);
				if (includeLoggerTypes == IncludeLoggerTypes.Telemetry)
				{
					project = project.AddMetadataReference(
						MetadataReference.CreateFromFile(
							typeof(Microsoft.Extensions.Logging.LogPropertiesAttribute)
								.Assembly
								.Location
						)
					);
				}
			}
		}

		project = projectModifier?.Invoke(project) ?? project;

		var compilation = await project.GetCompilationAsync();
		return (project, compilation!);
	}

	protected virtual Project SetupProject(Project project) => project;
}

public sealed record GenerationResult(
	GeneratorDriverRunResult DriverResult,
	ImmutableArray<Diagnostic> Diagnostics,
	Compilation Compilation
);
