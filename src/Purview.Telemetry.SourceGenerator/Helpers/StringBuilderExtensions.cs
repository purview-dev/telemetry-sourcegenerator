using System.Text;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Helpers;

static class StringBuilderExtensions
{
	public static StringBuilder AggressiveInlining(this StringBuilder builder, int indent) =>
		builder.Append(indent, Constants.System.AggressiveInlining);

	public static StringBuilder CodeGen(this StringBuilder builder, int indent) =>
		builder.Append(indent, Constants.System.GeneratedCode.Value);

	public static StringBuilder ClassAttributes(this StringBuilder builder, int indent) =>
		builder.Append(Utilities.GetClassAttributesString(true, indent)).AppendLine();

	public static StringBuilder WithIndent(this StringBuilder builder, int tabs)
	{
		for (var i = 0; i < tabs; i++)
			builder.Append('\t');

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
