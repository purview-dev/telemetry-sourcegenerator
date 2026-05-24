using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;

namespace Purview.Telemetry.SourceGenerator.Refactorings;

/// <summary>
/// Base class for testing <see cref="CodeRefactoringProvider"/> implementations.
/// The source code should include a <c>$$</c> marker to indicate the cursor position.
/// </summary>
public abstract class CodeRefactoringTestBase
{
	protected static async Task<string?> ApplyRefactoringAsync(
		string codeWithMarker,
		string? equivalenceKey = null,
		CancellationToken cancellationToken = default
	)
	{
		var provider = CreateDefaultProvider();
		return await ApplyRefactoringAsync(
			codeWithMarker,
			provider,
			equivalenceKey,
			cancellationToken
		);
	}

	protected static async Task<string?> ApplyRefactoringAsync(
		string codeWithMarker,
		CodeRefactoringProvider provider,
		string? equivalenceKey = null,
		CancellationToken cancellationToken = default
	)
	{
		var actions = await GetRefactoringActionsAsync(
			codeWithMarker,
			provider,
			cancellationToken: cancellationToken
		);

		if (actions.Count == 0)
			return null;

		CodeAction? action;
		if (equivalenceKey is not null)
		{
			// Check top-level actions first, then nested actions.
			action = actions.FirstOrDefault(a => a.EquivalenceKey == equivalenceKey);
			if (action is null)
			{
				foreach (var topLevel in actions)
				{
					action = topLevel.NestedActions.FirstOrDefault(a =>
						a.EquivalenceKey == equivalenceKey
					);
					if (action is not null)
						break;
				}
			}

			action ??= actions[0];
		}
		else
		{
			action = actions[0];
		}

		// When no equivalence key was given and the action is a nested-action group,
		// default to the first nested action ("In this class").
		if (equivalenceKey is null && action.NestedActions.Length > 0)
			action = action.NestedActions[0];

		var operations = await action.GetOperationsAsync(cancellationToken);

		foreach (var operation in operations)
		{
			if (operation is ApplyChangesOperation applyChanges)
			{
				var project = applyChanges.ChangedSolution.Projects.FirstOrDefault();
				if (project is null)
					return null;

				var doc = applyChanges.ChangedSolution.GetDocument(project.DocumentIds[0]);
				if (doc is null)
					return null;

				var root = await doc.GetSyntaxRootAsync(cancellationToken);
				return root?.ToFullString();
			}
		}

		return null;
	}

	protected static async Task<IReadOnlyList<CodeAction>> GetRefactoringActionsAsync(
		string codeWithMarker,
		string? equivalenceKey = null,
		CancellationToken cancellationToken = default
	)
	{
		var provider = CreateDefaultProvider();
		return await GetRefactoringActionsAsync(
			codeWithMarker,
			provider,
			equivalenceKey,
			cancellationToken
		);
	}

	protected static async Task<IReadOnlyList<CodeAction>> GetRefactoringActionsAsync(
		string codeWithMarker,
		CodeRefactoringProvider provider,
		string? _ = null,
		CancellationToken cancellationToken = default
	)
	{
		ArgumentNullException.ThrowIfNull(codeWithMarker);
		ArgumentNullException.ThrowIfNull(provider);

		const string marker = "$$";

		var cursorIndex = codeWithMarker.IndexOf(marker, StringComparison.Ordinal);
		if (cursorIndex < 0)
			throw new ArgumentException(
				$"Code must contain the cursor marker '{marker}'.",
				nameof(codeWithMarker)
			);

		var code = codeWithMarker.Remove(cursorIndex, marker.Length);

		var (project, _) = await CreateProjectAsync(code, cancellationToken);
		var document = project.Documents.First();

		var actions = new List<CodeAction>();

		var context = new CodeRefactoringContext(
			document,
			new Microsoft.CodeAnalysis.Text.TextSpan(cursorIndex, 0),
			actions.Add,
			cancellationToken
		);

		await provider.ComputeRefactoringsAsync(context);
		return actions;
	}

	static async Task<(Project, Compilation)> CreateProjectAsync(
		string code,
		CancellationToken cancellationToken
	)
	{
		using var workspace = new AdhocWorkspace();
		var projectInfo = ProjectInfo
			.Create(
				ProjectId.CreateNewId(),
				VersionStamp.Default,
				"TestProject",
				"TestProject",
				LanguageNames.CSharp
			)
			.WithCompilationOptions(
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
			)
			.WithMetadataReferences(GetDefaultReferences());

		var project = workspace.AddProject(projectInfo);
		project = project.AddDocument("Test.cs", code).Project;

		var compilation = await project.GetCompilationAsync(cancellationToken);
		return (project, compilation!);
	}

	static IEnumerable<MetadataReference> GetDefaultReferences()
	{
		yield return MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
		yield return MetadataReference.CreateFromFile(
			System.Reflection.Assembly.Load("netstandard, Version=2.0.0.0").Location
		);
		yield return MetadataReference.CreateFromFile(
			System.Reflection.Assembly.Load("System.Runtime").Location
		);
		yield return MetadataReference.CreateFromFile(
			typeof(Microsoft.Extensions.Logging.ILogger).Assembly.Location
		);
		yield return MetadataReference.CreateFromFile(
			typeof(Microsoft.Extensions.Logging.LogLevel).Assembly.Location
		);
		yield return MetadataReference.CreateFromFile(
			typeof(System.Diagnostics.ActivitySource).Assembly.Location
		);
		yield return MetadataReference.CreateFromFile(
			typeof(System.Diagnostics.Metrics.Counter<>).Assembly.Location
		);
	}

	/// <summary>
	/// Applies the refactoring to <paramref name="codeWithMarker"/> and verifies
	/// a snapshot containing both the original (before) and the rewritten (after) code.
	/// The snapshot is stored in <c>Snapshots/</c> and auto-accepted on first run.
	/// </summary>
	protected static async Task VerifyRefactoringAsync(
		string codeWithMarker,
		CodeRefactoringProvider provider,
		CancellationToken cancellationToken = default
	)
	{
		ArgumentNullException.ThrowIfNull(codeWithMarker);
		ArgumentNullException.ThrowIfNull(provider);

		var before = codeWithMarker
			.Replace("$$", string.Empty, StringComparison.Ordinal)
			.TrimStart();
		var after = await ApplyRefactoringAsync(
			codeWithMarker,
			provider,
			cancellationToken: cancellationToken
		);

		var snapshot = new
		{
			Before = before,
			After = after?.TrimStart() ?? "(no refactoring applied)",
		};

		await Verify(snapshot)
			.UseDirectory("Snapshots")
			.DisableRequireUniquePrefix()
			.DisableDateCounting()
			.AutoVerify();
	}

	/// <summary>
	/// Applies the named nested-scope refactoring (e.g. "In this document") and verifies
	/// a snapshot containing both the original (before) and the rewritten (after) code.
	/// </summary>
	protected static async Task VerifyRefactoringAsync(
		string codeWithMarker,
		CodeRefactoringProvider provider,
		string nestedActionEquivalenceKey,
		CancellationToken cancellationToken = default
	)
	{
		ArgumentNullException.ThrowIfNull(codeWithMarker);
		ArgumentNullException.ThrowIfNull(provider);
		ArgumentNullException.ThrowIfNull(nestedActionEquivalenceKey);

		var before = codeWithMarker
			.Replace("$$", string.Empty, StringComparison.Ordinal)
			.TrimStart();
		var after = await ApplyRefactoringAsync(
			codeWithMarker,
			provider,
			equivalenceKey: nestedActionEquivalenceKey,
			cancellationToken: cancellationToken
		);

		var snapshot = new
		{
			Before = before,
			After = after?.TrimStart() ?? "(no refactoring applied)",
		};

		await Verify(snapshot)
			.UseDirectory("Snapshots")
			.DisableRequireUniquePrefix()
			.DisableDateCounting()
			.AutoVerify();
	}

	/// <summary>
	/// Returns the default <see cref="CodeRefactoringProvider"/> used when no provider is
	/// explicitly specified. Defaults to <see cref="ConvertILoggerToTelemetryRefactoringProvider"/>.
	/// </summary>
	static ConvertILoggerToTelemetryRefactoringProvider CreateDefaultProvider() => new();
}
