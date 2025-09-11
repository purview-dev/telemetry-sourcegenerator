using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

static class EmitHelpers
{
	public static int EmitNamespaceStart(
		string? classNamespace,
		string[] parentClasses,
		CodeWriter builder,
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
		string[] parentClasses,
		int indent,
		CodeWriter builder,
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
		CodeWriter builder,
		int indent,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		if (SharedHelpers.ShouldEmit(requestingType, generationType))
			builder.ClassAttributes(indent);

		builder
			.Append(indent, "sealed partial class ", withNewLine: false)
			.Append(className)
			.Append(" : ")
			.AppendLine(fullyQualifiedInterface)
			.Append(indent, '{');

		return indent;
	}

	public static void EmitClassEnd(CodeWriter builder, int indent) => builder.Append(indent, '}');

	public static bool GenerateDuplicateMethodDiagnostics(
		GenerationType requestedType,
		GenerationType generationType,
		ImmutableDictionary<string, Location[]> duplicateMethods,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		if (duplicateMethods.IsEmpty)
		{
			// No duplicate methods found.
			return false;
		}

		if (!SharedHelpers.ShouldEmit(requestedType, generationType))
		{
			// We're not generating this type of method, so we don't need to emit diagnostics for it
			// but we do need to stop processing.
			return true;
		}

		logger?.Debug($"Found {duplicateMethods.Count} duplicate method(s).");

		foreach (var methodName in duplicateMethods.Keys)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			var locations = duplicateMethods[methodName];
			logger?.Diagnostic($"Method '{methodName}' is defined in multiple locations:");

			TelemetryDiagnostics.Report(
				context.ReportDiagnostic,
				TelemetryDiagnostics.General.DuplicateMethodNamesAreNotSupported,
				locations,
				methodName
			);
		}

		return true;
	}
}
