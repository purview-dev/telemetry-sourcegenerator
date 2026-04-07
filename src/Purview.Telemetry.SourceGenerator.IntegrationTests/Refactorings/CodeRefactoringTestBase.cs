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
		var actions = await GetRefactoringActionsAsync(
			codeWithMarker,
			equivalenceKey,
			cancellationToken
		);

		if (actions.Count == 0)
			return null;

		var action = equivalenceKey is not null
			? actions.FirstOrDefault(a => a.EquivalenceKey == equivalenceKey) ?? actions[0]
			: actions[0];

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
		ArgumentNullException.ThrowIfNull(codeWithMarker);

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

		var provider = new ConvertILoggerToTelemetryRefactoringProvider();
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
	}
}
