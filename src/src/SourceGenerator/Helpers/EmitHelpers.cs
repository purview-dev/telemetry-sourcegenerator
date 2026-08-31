using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

static class EmitHelpers
{
	public static int EmitNamespaceStart(
		string? classNamespace,
		EquatableArray<string> parentClasses,
		StringBuilder builder,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		var indent = 0;
		if (classNamespace != null)
		{
			builder.Append("namespace ").AppendLine(classNamespace);

			builder.Append('{').AppendLine();

			indent++;
		}

		if (parentClasses.Length > 0)
		{
			foreach (var parentClass in parentClasses.Reverse())
			{
				builder
					.Append(indent, "partial class ", withNewLine: false)
					.AppendLine(parentClass)
					.Append(indent, "{");

				indent++;
			}
		}

		return indent++;
	}

	public static void EmitNamespaceEnd(
		string? classNamespace,
		EquatableArray<string> parentClasses,
		int indent,
		StringBuilder builder,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		if (parentClasses.Length > 0)
		{
			foreach (var parentClass in parentClasses)
				builder.Append(--indent, '}');
		}

		if (classNamespace != null)
			builder.Append('}');
	}

	public static int EmitClassStart(
		GenerationType requestingType,
		GenerationType generationType,
		string className,
		string fullyQualifiedInterface,
		StringBuilder builder,
		int indent,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		// Only emit class attributes from one target to avoid duplicates on partial classes
		if (SharedHelpers.ShouldEmitClassAttributes(requestingType, generationType))
		{
			builder
				.Append(indent, Constants.System.EditorBrowsableConstant)
				.Append(indent, Constants.System.ExcludeFromCodeCoverageConstant)
				.CodeGen(indent);
		}

		builder
			.Append(indent, "sealed partial class ", withNewLine: false)
			.Append(className)
			.Append(" : ")
			.AppendLine(fullyQualifiedInterface)
			.Append(indent, '{');

		return indent;
	}

	public static void EmitClassEnd(StringBuilder builder, int indent) => builder.Append(indent, '}');
}
