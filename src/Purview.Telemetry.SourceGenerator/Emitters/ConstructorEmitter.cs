using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

static class ConstructorEmitter
{
	const string LoggerParameterName = "logger";

	public static int EmitCtor(
		GenerationType requestingType,
		GenerationType generationType,
		string classNameToGenerate,
		string fullyQualifiedInterfaceName,
		StringBuilder builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		if (!SharedHelpers.ShouldEmit(requestingType, generationType))
		{
			logger?.Debug($"Skipping constructor emit for {requestingType} ({generationType}).");

			return indent;
		}

		indent++;

		builder
			.AppendLine()
			.CodeGen(indent)
			.Append(indent, "public ", withNewLine: false)
			.Append(classNameToGenerate)
			.Append('(');

		EmitParameters(generationType, fullyQualifiedInterfaceName, builder);

		builder.AppendLine(')').Append(indent, '{');

		EmitBody(generationType, indent, builder);

		builder.Append(indent, '}');

		return --indent;
	}

	static void EmitParameters(
		GenerationType generationType,
		string? loggerFullyQualifiedInterfaceName,
		StringBuilder builder
	)
	{
		if (generationType.HasFlag(GenerationType.Logging))
		{
			builder
				.Append(Constants.Logging.MicrosoftExtensions.ILogger)
				.Append('<')
				.Append(loggerFullyQualifiedInterfaceName!)
				.Append("> ")
				.Append(LoggerParameterName);
		}

		if (generationType.HasFlag(GenerationType.Metrics))
		{
			if (generationType.HasFlag(GenerationType.Logging))
				builder.Append(", ");

			builder
				.Append(Constants.Metrics.SystemDiagnostics.IMeterFactory)
				.Append(' ')
				.Append(Constants.Metrics.MeterFactoryParameterName);
		}
	}

	static void EmitBody(GenerationType generationType, int indent, StringBuilder builder)
	{
		if (generationType.HasFlag(GenerationType.Logging))
		{
			builder
				.Append(indent + 1, Constants.Logging.LoggerFieldName, withNewLine: false)
				.Append(" = ")
				.Append(LoggerParameterName)
				.AppendLine(';');
		}

		if (generationType.HasFlag(GenerationType.Metrics))
		{
			builder
				.Append(indent + 1, Constants.Metrics.MeterInitializationMethod, withNewLine: false)
				.Append('(')
				.Append(Constants.Metrics.MeterFactoryParameterName)
				.AppendLine(");");
		}
	}
}
