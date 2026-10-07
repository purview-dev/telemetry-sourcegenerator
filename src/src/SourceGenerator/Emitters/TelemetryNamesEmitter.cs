using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

static class TelemetryNamesEmitter
{
	/// <summary>
	/// Emits <c>[assembly: GeneratedTelemetryNames]</c> with the names generated for this assembly.
	/// The attribute is the compile-time record downstream assemblies read to build their aggregate.
	/// </summary>
	public static void GenerateAssemblyAttribute(TelemetryNamesOutputContext output, SourceProductionContext spc)
	{
		output.Context.Debug($"Generating the telemetry names attribute for '{output.AssemblyName}'.");

		var writer = output.CreateWriter();

		var attributeType = TypeLibrary.Purview.Telemetry.GeneratedTelemetryNamesAttribute.RenderFullName;
		var activitySourceNames = BuildAttributeArrayArgument(output.Names.ActivitySourceNames);
		var meterNames = BuildAttributeArrayArgument(output.Names.MeterNames);

		writer.Line($"[assembly: {attributeType}({activitySourceNames}, {meterNames})]");

		var hintName = string.IsNullOrWhiteSpace(output.AssemblyName)
			? "GeneratedTelemetryNames.g.cs"
			: $"{output.AssemblyName}.GeneratedTelemetryNames.g.cs";

		spc.AddSource(hintName, writer);
	}

	public static void GenerateClass(
		TelemetryNamesOutputContext output,
		string className,
		string? rootNamespace,
		SourceProductionContext spc
	)
	{
		output.Context.Debug($"Generating Telemetry Names using class '{className}'.");

		var writer = output.CreateWriter();
		var hasNamespace = !string.IsNullOrWhiteSpace(rootNamespace);

		using (writer.BlockNamespaceScope(rootNamespace))
		{
			writer.XmlSummary(
				"Contains the names of the meters and activity sources generated for the assembly, and for the assemblies it references."
			);
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
				var stringArrayType = TypeLibrary.System.String.AsTypeReference().MakeArray();
				writer.XmlSummary("Gets the names of the meters generated for the assembly.");
				writer.Field(
					new("MeterNames", stringArrayType, TypeDeclarationAccessibility.Public)
					{
						IsStatic = true,
						IsReadOnly = true,
						Initializer = BuildArrayInitializer(output.Names.MeterNames),
						IncludeGeneratedAttributes = true,
					}
				);

				writer.XmlSummary("Gets the names of the activity sources generated for the assembly.");
				writer.Field(
					new("ActivitySourceNames", stringArrayType, TypeDeclarationAccessibility.Public)
					{
						IsStatic = true,
						IsReadOnly = true,
						Initializer = BuildArrayInitializer(output.Names.ActivitySourceNames),
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

	/// <summary>
	/// Builds the array argument for the assembly attribute. Attribute arguments must be constant
	/// expressions, so an empty array is written as <c>new string[0]</c> rather than
	/// <c>Array.Empty&lt;string&gt;()</c>.
	/// </summary>
	static string BuildAttributeArrayArgument(EquatableArray<string> values) =>
		values.IsEmpty ? "new string[0]" : BuildArray(values);

	static string BuildArrayInitializer(EquatableArray<string> values) =>
		values.IsEmpty ? "global::System.Array.Empty<string>()" : BuildArray(values);

	static string BuildArray(EquatableArray<string> values) =>
		"new string[] { " + string.Join(", ", values.Select(static v => "\"" + v + "\"")) + " }";
}
