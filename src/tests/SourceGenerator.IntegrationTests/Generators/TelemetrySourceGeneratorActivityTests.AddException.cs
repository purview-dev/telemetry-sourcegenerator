using Microsoft.CodeAnalysis.CSharp;
using Purview.SourceGeneratorFramework;
using Purview.Telemetry.SourceGenerator.Emitters;

namespace Purview.Telemetry.SourceGenerator.Activities;

partial class TelemetrySourceGeneratorActivityTests
{
	const string ActivitySourceWithEvent = """
		namespace Testing;

		[ActivitySource("testing-activity-source")]
		public interface ITestActivities
		{
			[Activity]
			System.Diagnostics.Activity? Activity();

			[Event]
			void Event(System.Diagnostics.Activity? activity, string name);
		}
		""";

	[Test]
	public async Task Generate_GivenActivitySource_EmitsAddExceptionWhenTheCompilationSupportsIt(
		CancellationToken cancellationToken
	)
	{
		// Arrange / Act
		using var generationResult = await GenerateAsync(ActivitySourceWithEvent, cancellationToken: cancellationToken);

		// Assert
		var source = generationResult.GetSource("TestActivitiesCore.Activity.g.cs");
		await Assert
			.That(source)
			.ContainsGeneratedCode("activity.AddException(exception, exceptionTags, default)")
			.Because("the framework should produce the exception event when Activity.AddException exists");
		await Assert
			.That(source)
			.ContainsGeneratedCode("\"exception.escaped\"")
			.Because("the escape flag is ours to supply through the TagList AddException accepts");
		await Assert
			.That(source)
			.DoesNotContain("recordExceptionEvent")
			.Because("the hand-built ActivityEvent is the fallback, not the preferred path");
	}

	[Test]
	public async Task Generate_GivenActivitySource_DoesNotDuplicateTheFrameworkExceptionTags(
		CancellationToken cancellationToken
	)
	{
		// Arrange / Act
		using var generationResult = await GenerateAsync(ActivitySourceWithEvent, cancellationToken: cancellationToken);

		// Assert: AddException writes exception.type/message/stacktrace itself. Emitting them too would either
		// duplicate the key or, worse, shadow the framework's values.
		var source = generationResult.GetSource("TestActivitiesCore.Activity.g.cs");
		await Assert
			.That(source)
			.DoesNotContain("exception.type")
			.Because("exception.type is written by Activity.AddException");
		await Assert
			.That(source)
			.DoesNotContain("exception.message")
			.Because("exception.message is written by Activity.AddException");
		await Assert
			.That(source)
			.DoesNotContain("exception.stacktrace")
			.Because("exception.stacktrace is written by Activity.AddException");
	}

	[Test]
	public async Task EmitRecordExceptionBody_GivenBothCapabilityStates_EmitsCompilingCodeWithTheSameTagSet(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		using var generationResult = await GenerateAsync(ActivitySourceWithEvent, cancellationToken: cancellationToken);
		var compilation = generationResult.CompilationResult.Compilation;

		var addExceptionBody = EmitRecordExceptionBodyForTest(supportsActivityAddException: true);
		var fallbackBody = EmitRecordExceptionBodyForTest(supportsActivityAddException: false);

		// Assert: the escape flag is the one tag both paths write.
		await Assert
			.That(addExceptionBody)
			.Contains("\"exception.escaped\"")
			.Because("the framework path still reports whether the exception escaped");
		await Assert
			.That(fallbackBody)
			.Contains("\"exception.escaped\"")
			.Because("the hand-built event reports whether the exception escaped");
		await Assert
			.That(addExceptionBody)
			.Contains("activity.AddException(exception, exceptionTags, default)")
			.Because("the supported path defers event creation to the framework");
		await Assert
			.That(fallbackBody)
			.Contains("recordExceptionEvent")
			.Because("the fallback must still be emitted when the probe fails");

		// Assert: both shapes compile against the same references the generated code has.
		await Assert.That(CompileErrors(compilation, addExceptionBody)).IsEmpty();
		await Assert.That(CompileErrors(compilation, fallbackBody)).IsEmpty();
	}

	static string EmitRecordExceptionBodyForTest(bool supportsActivityAddException)
	{
		CodeWriter writer = new(GenerationSettings.Create<TelemetrySourceGenerator>(), throwOnUnclosedScopes: false);
		ActivitySourceTargetClassEmitter.EmitRecordExceptionBody(writer, supportsActivityAddException);

		return writer.ToString();
	}

	static IReadOnlyList<string> CompileErrors(Compilation compilation, string body)
	{
		var source =
			"""
				#nullable enable

				internal static class ExceptionBodyHost
				{
					internal static void RecordExceptionInternal(
						global::System.Diagnostics.Activity? activity,
						global::System.Exception? exception,
						bool escape
					)
					{
				"""
			+ body
			+ """
					}
				}
				""";

		var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees.First().Options;
		var updated = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(source, parseOptions));

		return
		[
			.. updated
				.GetDiagnostics()
				.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
				.Select(static diagnostic => diagnostic.ToString()),
		];
	}
}
