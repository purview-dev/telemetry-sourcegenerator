using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Emitters;

static class TelemetryNamesEmitter
{
	public static void GenerateClass(
		ImmutableArray<string> meterNames,
		ImmutableArray<string> activitySourceNames,
		string className,
		string? rootNamespace,
		SourceProductionContext spc,
		GenerationLogger? logger
	)
	{
		logger?.Debug($"Generating Telemetry Names using class '{className}'.");

		StringBuilder builder = new();

		var hasNamespace = !string.IsNullOrWhiteSpace(rootNamespace);
		var indent = hasNamespace ? 1 : 0;

		// Namespace declaration (if provided)
		if (hasNamespace)
		{
			builder.AppendLine($"namespace {rootNamespace}");
			builder.AppendLine("{");
		}

		// Class declaration
		builder
			.Append(indent, Constants.System.EditorBrowsableConstant)
			.Append(indent, Constants.System.ExcludeFromCodeCoverageConstant)
			.CodeGen(indent)
			.Append(indent, $"static class {className}")
			.Append(indent, "{");

		// Meter names array
		builder.Append(
			indent + 1,
			"public static readonly string[] MeterNames = ",
			withNewLine: false
		);
		if (meterNames.Length == 0)
		{
			builder.AppendLine("global::System.Array.Empty<string>();");
		}
		else
		{
			builder.AppendLine("new string[]");
			builder.Append(indent + 1, "{");
			for (int i = 0; i < meterNames.Length; i++)
			{
				var name = meterNames[i];
				builder.Append(indent + 2, "\"", withNewLine: false).Append(name).Append('"');
				if (i < meterNames.Length - 1)
					builder.Append(',');
				builder.AppendLine();
			}
			builder.Append(indent + 1, "};");
		}

		builder.AppendLine();

		// ActivitySource names array
		builder.Append(
			indent + 1,
			"public static readonly string[] ActivitySourceNames = ",
			withNewLine: false
		);
		if (activitySourceNames.Length == 0)
		{
			builder.AppendLine("global::System.Array.Empty<string>();");
		}
		else
		{
			builder.AppendLine("new string[]");
			builder.Append(indent + 1, "{");
			for (int i = 0; i < activitySourceNames.Length; i++)
			{
				var name = activitySourceNames[i];
				builder.Append(indent + 2, "\"", withNewLine: false).Append(name).Append('"');
				if (i < activitySourceNames.Length - 1)
					builder.Append(',');
				builder.AppendLine();
			}
			builder.Append(indent + 1, "};");
		}

		// Close class
		builder.Append(indent, "}");

		// Close namespace (if provided)
		if (hasNamespace)
		{
			builder.AppendLine("}");
		}

		var hintName = $"{className}.g.cs";
		if (hasNamespace)
			hintName = $"{rootNamespace}.{hintName}";

		var sourceText = EmbeddedResources.Instance.AddHeader(builder.ToString());
		spc.AddSource(
			hintName,
			Microsoft.CodeAnalysis.Text.SourceText.From(sourceText, Encoding.UTF8)
		);
	}
}
