using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class LoggerTargetClassEmitter
{
	internal static void EmitThrowStub(
		StringBuilder builder,
		int indent,
		LogMethodTarget methodTarget,
		bool emitNullable = true
	)
	{
		builder.AppendLine().Append(indent, "public ", withNewLine: false);

		if (methodTarget.IsScoped)
			builder.Append(
				emitNullable ? Constants.System.IDisposable.WithNullable() : (string)Constants.System.IDisposable
			);
		else
			builder.Append(Constants.System.VoidKeyword);

		builder.Append(' ').Append(methodTarget.MethodName).Append('(');

		for (var i = 0; i < methodTarget.Parameters.Length; i++)
		{
			if (i > 0)
				builder.Append(", ");
			builder
				.Append(methodTarget.Parameters[i].ParameterType)
				.Append(' ')
				.Append(methodTarget.Parameters[i].Name);
		}

		builder.AppendLine(") => throw new global::System.NotSupportedException();").AppendLine();
	}

	static int EmitMethods(
		LoggerTarget target,
		StringBuilder builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable
	)
	{
		indent++;

		foreach (var methodTarget in target.LogMethods)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			if (!methodTarget.TargetGenerationState.IsValid)
			{
				if (
					EmitterHelpers.ShouldEmitThrowStub(
						methodTarget.TargetGenerationState,
						GenerationType.Logging,
						target.GenerationType
					)
				)
				{
					EmitThrowStub(builder, indent, methodTarget, emitNullable);
				}
				continue;
			}

			if (methodTarget.UnknownReturnType)
			{
				EmitThrowStub(builder, indent, methodTarget, emitNullable);
				continue; // Diagnostic already reported in EmitFields
			}

			if (methodTarget.HasMultipleExceptions)
			{
				EmitThrowStub(builder, indent, methodTarget, emitNullable);
				continue;
			}

			if (methodTarget.ParameterCountSansException > Constants.Logging.MaxNonExceptionParameters)
			{
				EmitThrowStub(builder, indent, methodTarget, emitNullable);
				continue;
			}

			EmitLogActionMethod(builder, indent, methodTarget, context, logger, emitNullable);
		}

		return --indent;
	}

	internal static void EmitLogActionMethod(
		StringBuilder builder,
		int indent,
		LogMethodTarget methodTarget,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable = true
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		logger?.Debug($"Building logging method: {methodTarget.MethodName}");

		var isMultiTarget = methodTarget.TargetGenerationState.IsMultiTarget;
		var methodTargets = methodTarget.TargetGenerationState.MethodTargets;

		// Determine ownership hierarchy: Activity > Logging > Metrics
		var activityOwnsPublicMethod = methodTargets.HasFlag(GenerationType.Activities);
		var loggingOwnsPublicMethod = !activityOwnsPublicMethod && methodTargets.HasFlag(GenerationType.Logging);
		var hasMetricsTarget = methodTargets.HasFlag(GenerationType.Metrics);

		// For multi-target where Logging owns the public method, we need to:
		// 1. Generate a private _Logging method
		// 2. Generate a public delegating method
		var generatePrivateLogging =
			isMultiTarget && (activityOwnsPublicMethod || (loggingOwnsPublicMethod && hasMetricsTarget));
		var generatePublicDelegator = isMultiTarget && loggingOwnsPublicMethod && hasMetricsTarget;

		var accessModifier = generatePrivateLogging ? "private" : "public";
		var methodName = generatePrivateLogging ? methodTarget.MethodName + "_Logging" : methodTarget.MethodName;

		builder
			.AppendLine()
			.CodeGen(indent)
			.AggressiveInlining(indent)
			.Append(indent, accessModifier + " ", withNewLine: false);

		// For multi-target private methods, always return void (the logging side-effect)
		// For single-target or public methods, use original return type logic
		if (generatePrivateLogging)
		{
			builder.Append(Constants.System.VoidKeyword);
		}
		else if (methodTarget.IsScoped)
		{
			builder.Append(Constants.System.IDisposable);
			if (emitNullable)
				builder.Append('?');
		}
		else
		{
			builder.Append(Constants.System.VoidKeyword);
		}

		builder.Append(' ').Append(methodName).Append('(');

		EmitParametersAsMethodArgumentList(methodTarget, builder, context);

		builder.Append(')').AppendLine().Append(indent, '{');

		if (methodTarget.IsScoped && !generatePrivateLogging)
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
			// Non-scoped log methods always need the exception parameter (null if not provided)
			builder.Append(", ").Append("null");
		}
		// Scoped methods don't take an exception parameter

		builder.AppendLine(");");

		builder.Append(indent, '}').AppendLine();

		// Generate public delegating method if Logging owns it
		if (generatePublicDelegator)
		{
			EmitPublicLoggingDelegatingMethod(builder, indent, methodTarget, context, logger, emitNullable);
		}
	}

	static void EmitPublicLoggingDelegatingMethod(
		StringBuilder builder,
		int indent,
		LogMethodTarget methodTarget,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable = true
	)
	{
		logger?.Debug($"Building public delegating logging method: {methodTarget.MethodName}");

		builder.AppendLine().CodeGen(indent).AggressiveInlining(indent).Append(indent, "public ", withNewLine: false);

		// When Logging owns the public method (with Metrics), return void
		// (Logging without Activity means the return type is void or IDisposable for scoped)
		if (methodTarget.IsScoped)
		{
			builder.Append(Constants.System.IDisposable);
			if (emitNullable)
				builder.Append('?');
		}
		else
		{
			builder.Append(Constants.System.VoidKeyword);
		}

		builder.Append(' ').Append(methodTarget.MethodName).Append('(');

		EmitParametersAsMethodArgumentList(methodTarget, builder, context);

		builder.Append(')').AppendLine().Append(indent, '{');

		// Call the private Logging method
		if (methodTarget.IsScoped)
		{
			builder.Append(indent + 1, "var loggingResult = ", withNewLine: false);
		}
		else
		{
			builder.Append(indent + 1, methodTarget.MethodName, withNewLine: false).Append("_Logging(");
		}

		if (!methodTarget.IsScoped)
		{
			// Emit parameters
			for (var i = 0; i < methodTarget.TotalParameterCount; i++)
			{
				context.CancellationToken.ThrowIfCancellationRequested();

				builder.Append(methodTarget.Parameters[i].Name);

				if (i < methodTarget.TotalParameterCount - 1)
					builder.Append(", ");
			}
			builder.AppendLine(");");
		}
		else
		{
			// For scoped, we need special handling
			builder.Append(methodTarget.MethodName).Append("_Logging(");
			for (var i = 0; i < methodTarget.TotalParameterCount; i++)
			{
				context.CancellationToken.ThrowIfCancellationRequested();

				builder.Append(methodTarget.Parameters[i].Name);

				if (i < methodTarget.TotalParameterCount - 1)
					builder.Append(", ");
			}
			builder.AppendLine(");");
		}

		// Call the private Metrics method
		builder.Append(indent + 1, methodTarget.MethodName, withNewLine: false).Append("_Metrics(");

		for (var i = 0; i < methodTarget.TotalParameterCount; i++)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			builder.Append(methodTarget.Parameters[i].Name);

			if (i < methodTarget.TotalParameterCount - 1)
				builder.Append(", ");
		}
		builder.AppendLine(");");

		// Return if scoped
		if (methodTarget.IsScoped)
		{
			builder.AppendLine().Append(indent + 1, "return loggingResult;");
		}

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
