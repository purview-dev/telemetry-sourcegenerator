using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class LoggerTargetClassEmitter
{
	static int EmitMethods(
		LoggerTarget target,
		StringBuilder builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		indent++;

		foreach (var methodTarget in target.LogMethods)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			if (!methodTarget.TargetGenerationState.IsValid)
				continue;

			if (methodTarget.HasMultipleExceptions)
				continue;

			if (
				methodTarget.ParameterCountSansException
				> Constants.Logging.MaxNonExceptionParameters
			)
			{
				continue;
			}

			EmitLogActionMethod(builder, indent, methodTarget, context, logger);
		}

		return --indent;
	}

	static void EmitLogActionMethod(
		StringBuilder builder,
		int indent,
		LogMethodTarget methodTarget,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Building logging method: {methodTarget.MethodName}");

		builder
			.AppendLine()
			.CodeGen(indent)
			.AggressiveInlining(indent)
			.Append(indent, "public ", withNewLine: false);

		if (methodTarget.IsScoped)
		{
			builder.Append(Constants.System.IDisposable).Append('?');
		}
		else
		{
			builder.Append(Constants.System.VoidKeyword);
		}

		builder.Append(' ').Append(methodTarget.MethodName).Append('(');

		EmitParametersAsMethodArgumentList(methodTarget, builder, context);

		builder.Append(')').AppendLine().Append(indent, '{');

		if (methodTarget.IsScoped)
		{
			builder
				.Append(indent + 1, "return ", withNewLine: false)
				.Append(methodTarget.LoggerActionFieldName)
				.Append('(')
				.Append(Constants.Logging.LoggerFieldName);
		}
		else
		{
			builder
				.Append(indent + 1, "if (!", withNewLine: false)
				.Append(Constants.Logging.LoggerFieldName)
				.Append(".IsEnabled(")
				.Append(methodTarget.MSLevel)
				.AppendLine("))")
				.Append(indent + 1, '{')
				.Append(indent + 2, "return;")
				.Append(indent + 1, '}')
				.AppendLine()
				.Append(indent + 1, methodTarget.LoggerActionFieldName, withNewLine: false)
				.Append('(')
				.Append(Constants.Logging.LoggerFieldName);
		}

		foreach (var parameter in methodTarget.ParametersSansException)
		{
			builder.Append(", ").Append(parameter.Name);
		}

		if (methodTarget.ExceptionParameter != null)
		{
			builder.Append(", ").Append(methodTarget.ExceptionParameter.Name);
		}
		else if (!methodTarget.IsScoped)
		{
			builder.Append(", ").Append("null");
		}

		builder.AppendLine(");");

		builder.Append(indent, '}').AppendLine();
	}

	static void EmitParametersAsMethodArgumentList(
		LogMethodTarget methodTarget,
		StringBuilder builder,
		SourceProductionContext context
	)
	{
		for (var i = 0; i < methodTarget.TotalParameterCount; i++)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			builder
				.Append(methodTarget.Parameters[i].ParameterType)
				.Append(' ')
				.Append(methodTarget.Parameters[i].Name);

			if (i < methodTarget.TotalParameterCount - 1)
				builder.Append(", ");
		}
	}
}
