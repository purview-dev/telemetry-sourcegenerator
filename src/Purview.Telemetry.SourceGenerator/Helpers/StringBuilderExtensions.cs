using System.Text;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Helpers;

static class StringBuilderExtensions
{
	static readonly string[] IndentCache = CreateIndentCache();

	static string[] CreateIndentCache()
	{
		var arr = new string[33];
		arr[0] = string.Empty;
		for (var i = 1; i < arr.Length; i++)
			arr[i] = new string('\t', i);
		return arr;
	}

	public static StringBuilder AggressiveInlining(this StringBuilder builder, int indent) =>
		builder.Append(indent, Constants.System.AggressiveInlining);

	public static StringBuilder CodeGen(this StringBuilder builder, int indent) =>
		builder.Append(indent, Constants.System.GeneratedCode.Value);

	public static StringBuilder ClassAttributes(this StringBuilder builder, int indent) =>
		builder.AppendLine(Utilities.GetClassAttributesString(true, indent));

	public static StringBuilder WithIndent(this StringBuilder builder, int tabs)
	{
		builder.Append(IndentCache[tabs < IndentCache.Length ? tabs : IndentCache.Length - 1]);

		return builder;
	}

	public static StringBuilder Append(
		this StringBuilder builder,
		int tabs,
		char value,
		bool withNewLine = true
	)
	{
		builder.WithIndent(tabs).Append(value);

		if (withNewLine)
			builder.AppendLine();

		return builder;
	}

	public static StringBuilder Append(
		this StringBuilder builder,
		int tabs,
		string value,
		bool withNewLine = true
	)
	{
		builder.WithIndent(tabs).Append(value);

		if (withNewLine)
			builder.AppendLine();

		return builder;
	}

	public static StringBuilder Append(
		this StringBuilder builder,
		int tabs,
		PurviewTypeInfo typeInfo,
		bool withNewLine = true
	)
	{
		builder.WithIndent(tabs).Append(typeInfo);

		if (withNewLine)
			builder.AppendLine();

		return builder;
	}

	public static StringBuilder AppendLine(this StringBuilder builder, char @char) =>
		builder.Append(@char).AppendLine();
}
