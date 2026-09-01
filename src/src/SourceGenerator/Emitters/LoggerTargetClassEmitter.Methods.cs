using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class LoggerTargetClassEmitter
{
	internal static void EmitThrowStub(CodeWriter writer, LogMethodTarget methodTarget, bool emitNullable = true)
	{
		var returnType = methodTarget.IsScoped
			? emitNullable
				? TypeLibrary.System.IDisposable.AsTypeReference().Nullable()
				: TypeLibrary.System.IDisposable.AsTypeReference()
			: PurviewTypeLibrary.System.Void.AsTypeReference();

		writer.NewLine();
		using (
			writer.WriteMethodScope(
				new MethodDeclarationOptions(methodTarget.MethodName, returnType, TypeDeclarationAccessibility.Public)
				{
					Parameters =
					[
						.. methodTarget.Parameters.Select(p => new ParameterDeclarationOptions(
							p.Name,
							p.ParameterType
						)),
					],
					ExpressionBody = "throw new global::System.NotSupportedException()",
					IncludeGeneratedAttributes = false,
				}
			)
		)
		{
			//
		}
	}

	static void EmitMethods(
		LoggerTarget target,
		CodeWriter writer,
		SourceProductionContext context,
		ISourceGenLogger? logger,
		bool emitNullable
	)
	{
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
					EmitThrowStub(writer, methodTarget, emitNullable);
				}
				continue;
			}

			if (methodTarget.UnknownReturnType)
			{
				EmitThrowStub(writer, methodTarget, emitNullable);
				continue; // Diagnostic already reported in EmitFields
			}

			if (methodTarget.HasMultipleExceptions)
			{
				EmitThrowStub(writer, methodTarget, emitNullable);
				continue;
			}

			if (methodTarget.ParameterCountSansException > PropertyLibrary.Logging.MaxNonExceptionParameters)
			{
				EmitThrowStub(writer, methodTarget, emitNullable);
				continue;
			}

			EmitLogActionMethod(writer, methodTarget, context, logger, emitNullable);
		}
	}

	internal static void EmitLogActionMethod(
		CodeWriter writer,
		LogMethodTarget methodTarget,
		SourceProductionContext context,
		ISourceGenLogger? logger,
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

		var methodName = generatePrivateLogging ? methodTarget.MethodName + "_Logging" : methodTarget.MethodName;

		var returnType =
			generatePrivateLogging || !methodTarget.IsScoped
				? PurviewTypeLibrary.System.Void.AsTypeReference()
				: TypeLibrary.System.IDisposable.AsTypeReference().Nullable();

		writer.NewLine();

		using (
			writer.WriteMethodScope(
				new MethodDeclarationOptions(
					methodName,
					returnType,
					generatePrivateLogging ? TypeDeclarationAccessibility.Private : TypeDeclarationAccessibility.Public
				)
				{
					Parameters =
					[
						.. methodTarget.Parameters.Select(p => new ParameterDeclarationOptions(
							p.Name,
							p.ParameterType
						)),
					],
					IncludeGeneratedAttributes = false,
				}
			)
		)
		{
			if (methodTarget.IsScoped && !generatePrivateLogging)
			{
				writer
					.Write("return ")
					.Write(methodTarget.LoggerActionFieldName)
					.Write('(')
					.Write(PropertyLibrary.Logging.LoggerFieldName);
			}
			else
			{
				writer
					.Write("if (!")
					.Write(PropertyLibrary.Logging.LoggerFieldName)
					.Write(".IsEnabled(")
					.Write(methodTarget.MSLevel)
					.WriteLine("))");

				using (writer.OpenBlockScope())
					writer.WriteLine("return;");

				writer
					.NewLine()
					.Write(methodTarget.LoggerActionFieldName)
					.Write('(')
					.Write(PropertyLibrary.Logging.LoggerFieldName);
			}

			foreach (var parameter in methodTarget.ParametersSansException)
			{
				writer.Write(", ").Write(parameter.Name);
			}

			if (methodTarget.ExceptionParameter != null)
			{
				writer.Write(", ").Write(methodTarget.ExceptionParameter.Name);
			}
			else if (!methodTarget.IsScoped)
			{
				// Non-scoped log methods always need the exception parameter (null if not provided)
				writer.Write(", ").Write("null");
			}
			// Scoped methods don't take an exception parameter

			writer.Write(");").NewLine();
		}

		// Generate public delegating method if Logging owns it
		if (generatePublicDelegator)
		{
			EmitPublicLoggingDelegatingMethod(writer, methodTarget, context, logger, emitNullable);
		}
	}

	static void EmitPublicLoggingDelegatingMethod(
		CodeWriter writer,
		LogMethodTarget methodTarget,
		SourceProductionContext context,
		ISourceGenLogger? logger,
		bool emitNullable = true
	)
	{
		logger?.Debug($"Building public delegating logging method: {methodTarget.MethodName}");

		var returnType = methodTarget.IsScoped
			? emitNullable
				? TypeLibrary.System.IDisposable.AsTypeReference().Nullable()
				: TypeLibrary.System.IDisposable.AsTypeReference()
			: PurviewTypeLibrary.System.Void.AsTypeReference();

		writer.NewLine();

		using (
			writer.WriteMethodScope(
				new MethodDeclarationOptions(methodTarget.MethodName, returnType, TypeDeclarationAccessibility.Public)
				{
					Parameters =
					[
						.. methodTarget.Parameters.Select(p => new ParameterDeclarationOptions(
							p.Name,
							p.ParameterType
						)),
					],
					IncludeGeneratedAttributes = false,
				}
			)
		)
		{
			// Call the private Logging method
			if (methodTarget.IsScoped)
			{
				writer.Write("var loggingResult = ");
			}
			else
			{
				writer.Write(methodTarget.MethodName).Write("_Logging(");
			}

			if (!methodTarget.IsScoped)
			{
				// Emit parameters
				for (var i = 0; i < methodTarget.TotalParameterCount; i++)
				{
					context.CancellationToken.ThrowIfCancellationRequested();

					writer.Write(methodTarget.Parameters[i].Name);

					if (i < methodTarget.TotalParameterCount - 1)
						writer.Write(", ");
				}
				writer.Write(");").NewLine();
			}
			else
			{
				// For scoped, we need special handling
				writer.Write(methodTarget.MethodName).Write("_Logging(");
				for (var i = 0; i < methodTarget.TotalParameterCount; i++)
				{
					context.CancellationToken.ThrowIfCancellationRequested();

					writer.Write(methodTarget.Parameters[i].Name);

					if (i < methodTarget.TotalParameterCount - 1)
						writer.Write(", ");
				}
				writer.Write(");").NewLine();
			}

			// Call the private Metrics method
			writer.Write(methodTarget.MethodName).Write("_Metrics(");

			for (var i = 0; i < methodTarget.TotalParameterCount; i++)
			{
				context.CancellationToken.ThrowIfCancellationRequested();

				writer.Write(methodTarget.Parameters[i].Name);

				if (i < methodTarget.TotalParameterCount - 1)
					writer.Write(", ");
			}
			writer.Write(");").NewLine();

			// Return if scoped
			if (methodTarget.IsScoped)
			{
				writer.NewLine().Write("return loggingResult;");
			}
		}

		writer.NewLine();
	}
}
