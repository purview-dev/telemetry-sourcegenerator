using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Purview.Telemetry.SourceGenerator.Infra;

/// <summary>
/// Builds a metadata reference that stands in for an assembly built with the generator: it declares
/// the generated attribute as its own <see langword="internal"/> type and applies it at the assembly
/// level, exactly as the generator does. Used to exercise the aggregation of referenced assemblies'
/// telemetry names.
/// </summary>
public static class TelemetryNamesReference
{
	public static PortableExecutableReference Create(
		string assemblyName,
		string[] activitySourceNames,
		string[] meterNames
	) => Compile(assemblyName, BuildSource(activitySourceNames, meterNames));

	static string BuildSource(string[] activitySourceNames, string[] meterNames)
	{
		static string Literal(string[] values) =>
			"new string[] { " + string.Join(", ", values.Select(value => "\"" + value + "\"")) + " }";

		var activitySources = Literal(activitySourceNames);
		var meters = Literal(meterNames);

		// The attribute is declared exactly as the generator declares it, including
		// [Microsoft.CodeAnalysis.Embedded] — Roslyn hides embedded types from name lookup in
		// referencing compilations, so the aggregation has to read the attribute data regardless.
		return $$"""
			[assembly: Purview.Telemetry.GeneratedTelemetryNames({{activitySources}}, {{meters}})]

			namespace Microsoft.CodeAnalysis
			{
				internal sealed class EmbeddedAttribute : System.Attribute { }
			}

			namespace Purview.Telemetry
			{
				[Microsoft.CodeAnalysis.Embedded]
				[System.AttributeUsage(System.AttributeTargets.Assembly)]
				internal sealed class GeneratedTelemetryNamesAttribute : System.Attribute
				{
					public GeneratedTelemetryNamesAttribute(string[] activitySourceNames, string[] meterNames)
					{
						ActivitySourceNames = activitySourceNames;
						MeterNames = meterNames;
					}

					public string[] ActivitySourceNames { get; set; }

					public string[] MeterNames { get; set; }
				}
			}
			""";
	}

	static PortableExecutableReference Compile(string assemblyName, string source)
	{
		var compilation = CSharpCompilation.Create(
			assemblyName,
			[CSharpSyntaxTree.ParseText(source)],
			[MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);

		using MemoryStream stream = new();
		var emitResult = compilation.Emit(stream);
		if (!emitResult.Success)
			throw new InvalidOperationException(Describe(emitResult));

		// The stream is left open so that the reference can be used after this method returns.
		return MetadataReference.CreateFromImage(stream.ToArray());
	}

	static string Describe(EmitResult emitResult) =>
		"Failed to compile the referenced telemetry names assembly: "
		+ string.Join(
			Environment.NewLine,
			emitResult.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
		);
}
