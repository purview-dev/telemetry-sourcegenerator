using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

static class TelemetryNamesEmitter
{
	public static void GenerateClass(
		TelemetryNamesOutputContext output,
		ImmutableArray<string> meterNames,
		ImmutableArray<string> activitySourceNames,
		string className,
		string? rootNamespace,
		SourceProductionContext spc,
		GenerationContext<TelemetryCapabilities> generationContext
	)
	{
		generationContext.Debug($"Generating Telemetry Names using class '{className}'.");

		var writer = output.CreateWriter();
		var hasNamespace = !string.IsNullOrWhiteSpace(rootNamespace);

		using (writer.BlockNamespaceScope(rootNamespace))
		{
			writer.XmlSummary("Contains the names of the meters and activity sources generated for the assembly.");
			using (
				writer.ClassScope(
					new(className)
					{
						IsStatic = true,
						IncludeGeneratedAttributes = true,
						Attributes = [EmitterHelpers.EditorBrowsableAttribute()],
					}
				)
			)
			{
				var stringArrayType = PurviewTypeLibrary.System.String.AsTypeReference().MakeArray();
				writer.XmlSummary("Gets the names of the meters generated for the assembly.");
				writer.Field(
					new("MeterNames", stringArrayType, TypeDeclarationAccessibility.Public)
					{
						IsStatic = true,
						IsReadOnly = true,
						Initializer = BuildArrayInitializer(meterNames),
						IncludeGeneratedAttributes = true,
					}
				);

				writer.XmlSummary("Gets the names of the activity sources generated for the assembly.");
				writer.Field(
					new("ActivitySourceNames", stringArrayType, TypeDeclarationAccessibility.Public)
					{
						IsStatic = true,
						IsReadOnly = true,
						Initializer = BuildArrayInitializer(activitySourceNames),
						IncludeGeneratedAttributes = true,
					}
				);
			}
		}

		var hintName = $"{className}.g.cs";
		if (hasNamespace)
			hintName = $"{rootNamespace}.{hintName}";

		spc.AddSource(hintName, writer);
	}

	static string BuildArrayInitializer(ImmutableArray<string> values) =>
		values.Length == 0
			? "global::System.Array.Empty<string>()"
			: "new string[] { " + string.Join(", ", values.Select(v => "\"" + v + "\"")) + " }";
}
