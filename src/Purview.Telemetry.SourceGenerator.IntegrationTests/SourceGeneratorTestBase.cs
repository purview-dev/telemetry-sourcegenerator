using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Purview.Telemetry.SourceGenerator.BuildTools;
using Purview.Telemetry.SourceGenerator.Helpers;
using Assembly = System.Reflection.Assembly;

namespace Purview.Telemetry.SourceGenerator;

public abstract class IncrementalSourceGeneratorTestBase<TGenerator>
	: SourceGeneratorTestBase<ISourceGenerator>
	where TGenerator : class, IIncrementalGenerator
{
	protected IncrementalSourceGeneratorTestBase(bool throwOnLoggedOnError = true)
		: base(throwOnLoggedOnError)
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

public abstract class SourceGeneratorTestBase<TGenerator>(bool throwOnLoggedOnError = true)
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

		if (generator is ILogSupport logging && TestContext.Current is not null)
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

					TestContext.Current.OutputWriter.WriteLine($"{prefix}: {message}");

					if (ThrowOnLoggedOnError && outputType == OutputType.Error)
						throw new InvalidOperationException($"Generator logged error: {message}");
				}
			);
		}
	}

	protected static AdditionalText Text(string content, bool autoIncludeUsings = true) =>
		new InMemoryAdditionalText(
			$"{Guid.NewGuid()}",
			(autoIncludeUsings ? TestHelpers.DefaultUsingSet : "") + content
		);

	protected static AdditionalText Text(
		string path,
		string content,
		bool autoIncludeUsings = true
	) =>
		new InMemoryAdditionalText(
			path,
			(autoIncludeUsings ? TestHelpers.DefaultUsingSet : "") + content
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
		foreach (var tree in generationResult.Result.GeneratedTrees)
			nodes.Add((await tree.GetRootAsync(cancellationToken)).SyntaxTree);

		return generationResult.Compilation.AddSyntaxTrees(nodes);
	}

	protected Type GetType(GenerationResult result, string typeName)
	{
		var assembly = GetAssembly(result);

		var type = assembly.GetType(typeName, true);
		ArgumentNullException.ThrowIfNull(type, nameof(typeName));
		return type;
	}

	protected Assembly GetAssembly(GenerationResult result)
	{
		ArgumentNullException.ThrowIfNull(result);

		Assembly assembly;
		using (var stream = new MemoryStream())
		{
			var emitResult = result.Compilation.Emit(stream);
			ArgumentNullException.ThrowIfNull(emitResult);
			if (!emitResult.Success)
			{
				throw new InvalidOperationException(
					$"Compilation failed: {string.Join("\n", emitResult.Diagnostics)}"
				);
			}

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
		bool autoIncludeUsings = true,
		IncludeLoggerTypes includeLoggerTypes = IncludeLoggerTypes.LoggerOnly,
		LanguageVersion languageVersion = LanguageVersion.Default,
		CancellationToken cancellationToken = default
	)
	{
		return await GenerateAsync(
			Text(csharpDocument, autoIncludeUsings: autoIncludeUsings),
			additionalTexts,
			globalOptions,
			projectModifier,
			disableDependencyInjection,
			includeLoggerTypes,
			languageVersion: languageVersion,
			cancellationToken: cancellationToken
		);
	}

	protected async Task<GenerationResult> GenerateAsync(
		AdditionalText csharpDocument,
		AdditionalText[]? additionalTexts = null,
		ImmutableDictionary<string, string>? globalOptions = null,
		Func<Project, Project>? projectModifier = null,
		bool disableDependencyInjection = true,
		IncludeLoggerTypes includeLoggerTypes = IncludeLoggerTypes.LoggerOnly,
		LanguageVersion languageVersion = LanguageVersion.Default,
		CancellationToken cancellationToken = default
	)
	{
		return await GenerateAsync(
			[csharpDocument],
			additionalTexts,
			globalOptions,
			projectModifier,
			disableDependencyInjection,
			includeLoggerTypes,
			languageVersion: languageVersion,
			cancellationToken: cancellationToken
		);
	}

	protected async Task<GenerationResult> GenerateAsync(
		AdditionalText[] csharpDocuments,
		AdditionalText[]? additionalTexts = null,
		ImmutableDictionary<string, string>? globalOptions = null,
		Func<Project, Project>? projectModifier = null,
		bool disableDependencyInjection = true,
		IncludeLoggerTypes includeLoggerTypes = IncludeLoggerTypes.LoggerOnly,
		LanguageVersion languageVersion = LanguageVersion.Default,
		CancellationToken cancellationToken = default
	)
	{
		List<string> preprocessorSymbols =
		[
			languageVersion == LanguageVersion.CSharp7_3 ? "NET48_OR_GREATER" : "NET8_0_OR_GREATER",
		];
		if (includeLoggerTypes == IncludeLoggerTypes.None)
			preprocessorSymbols.Add("EXCLUDE_PURVIEW_TELEMETRY_LOGGING");

		CSharpParseOptions parseOptions = new(
			languageVersion: languageVersion,
			documentationMode: DocumentationMode.Parse,
			kind: SourceCodeKind.Regular,
			preprocessorSymbols: preprocessorSymbols
		);

		globalOptions ??= [];

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
					autoIncludeUsings: false
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

		GeneratorDriver driver = CSharpGeneratorDriver.Create(
			[Generator],
			additionalTexts: additionalTexts,
			parseOptions: parseOptions,
			optionsProvider: optionsProvider
		);
		(var _, var compilation) = await ObtainProjectAndCompilationAsync(
			projectModifier,
			csharpDocuments,
			includeLoggerTypes,
			languageVersion,
			cancellationToken
		);

		var result = driver.RunGeneratorsAndUpdateCompilation(
			compilation,
			out var outputCompilation,
			out var diagnostics,
			cancellationToken
		);
		if (TestContext.Current is not null)
		{
			foreach (var d in diagnostics)
			{
				if (d.Severity is DiagnosticSeverity.Error)
					await TestContext.Current.ErrorOutputWriter.WriteLineAsync(d.ToString());
				else
					await TestContext.Current.OutputWriter.WriteLineAsync(d.ToString());
			}
		}

		var runResult = result.GetRunResult();

		// Run the analyzer on outputCompilation (which includes generated attribute source files).
		var analyzerDiagnostics = await RunAnalyzerAsync(outputCompilation, cancellationToken);
		var allDiagnostics = diagnostics.AddRange(analyzerDiagnostics);

		await Assert
			.That(runResult.Results.Where(m => m.Exception != null).Select(m => m.Exception))
			.IsEmpty();

		return new(runResult, allDiagnostics, outputCompilation);
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
		IncludeLoggerTypes includeLoggerTypes = IncludeLoggerTypes.LoggerOnly,
		LanguageVersion languageVersion = LanguageVersion.Default,
		CancellationToken cancellationToken = default
	)
	{
		using AdhocWorkspace workspace = new();
		var project = workspace.AddProject(
			typeof(SourceGeneratorTestBase<>).Namespace,
			LanguageNames.CSharp
		);

		project = project.WithCompilationOptions(
			project.CompilationOptions!.WithOutputKind(OutputKind.DynamicallyLinkedLibrary)
		);

		var compilationFrameworkSymbol =
			languageVersion == LanguageVersion.CSharp7_3 ? "NET48_OR_GREATER" : "NET8_0_OR_GREATER";
		var compilationParseOptions =
			languageVersion != LanguageVersion.Default
				? new CSharpParseOptions(
					languageVersion: languageVersion,
					preprocessorSymbols: [compilationFrameworkSymbol]
				)
				: new CSharpParseOptions(preprocessorSymbols: [compilationFrameworkSymbol]);
		project = project.WithParseOptions(compilationParseOptions);

		project = project.AddMetadataReference(
			MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location)
		);

		if (csharpDocuments?.Length > 0)
		{
			foreach (var csDoc in csharpDocuments)
			{
				project = project
					.AddDocument(csDoc.Path, csDoc.GetText(cancellationToken)!)
					.Project;
			}
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

		var compilation = await project.GetCompilationAsync(cancellationToken);
		return (project, compilation!);
	}

	protected virtual Project SetupProject(Project project) => project;

	static async Task<ImmutableArray<Diagnostic>> RunAnalyzerAsync(
		Compilation compilation,
		CancellationToken cancellationToken
	)
	{
		var compilationWithAnalyzers = compilation.WithAnalyzers([
			new TelemetryDiagnosticAnalyzer(),
		]);

		return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken);
	}
}

public record GenerationResult(
	GeneratorDriverRunResult Result,
	ImmutableArray<Diagnostic> Diagnostics,
	Compilation Compilation
);
