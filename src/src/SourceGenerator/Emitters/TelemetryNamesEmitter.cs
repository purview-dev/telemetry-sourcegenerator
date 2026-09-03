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

		using (writer.WriteBlockNamespaceScope(rootNamespace))
		{
			using (
				writer.WriteClassScope(
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
				writer.WriteField(
					new("MeterNames", stringArrayType, TypeDeclarationAccessibility.Public)
					{
						IsStatic = true,
						IsReadOnly = true,
						Initializer = BuildArrayInitializer(meterNames),
						IncludeGeneratedAttributes = true,
					}
				);

				writer.WriteField(
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
