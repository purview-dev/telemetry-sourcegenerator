using System.Text;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Helpers;

static class StringBuilderExtensions
{
	// Cache common indent strings to avoid repeated allocations
	private static readonly string[] CachedIndents =
	[
		"",           // 0
		"\t",         // 1
		"\t\t",       // 2
		"\t\t\t",     // 3
		"\t\t\t\t",   // 4
		"\t\t\t\t\t", // 5
		"\t\t\t\t\t\t", // 6
		"\t\t\t\t\t\t\t", // 7
		"\t\t\t\t\t\t\t\t" // 8
	];

	public static StringBuilder AggressiveInlining(this StringBuilder builder, int indent) =>
		builder.Append(indent, Constants.System.AggressiveInlining);

	public static StringBuilder CodeGen(this StringBuilder builder, int indent) =>
		builder.Append(indent, Constants.System.GeneratedCode.Value);

	public static StringBuilder IfDefines(
		this StringBuilder builder,
		string condition,
		params string[] values
	) => builder.IfDefines(condition, 0, values);

	public static StringBuilder IfDefines(
		this StringBuilder builder,
		string condition,
		int indent,
		params string[] values
	)
	{
		builder.AppendLine().Append("#if ").AppendLine(condition).WithIndent(indent);

		foreach (var value in values)
			builder.Append(value);

		builder.AppendLine().AppendLine("#endif");

		return builder;
	}

	public static StringBuilder WithIndent(this StringBuilder builder, int tabs)
	{
		// Use cached indent strings for common cases
		if (tabs >= 0 && tabs < CachedIndents.Length)
		{
			return builder.Append(CachedIndents[tabs]);
		}

		// Fall back to loop for unusual cases
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
