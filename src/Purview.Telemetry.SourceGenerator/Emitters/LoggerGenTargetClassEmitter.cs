using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Emitters;

static partial class LoggerGenTargetClassEmitter
{
	public static void GenerateImplementation(
		LoggerTarget target,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable = true
	)
	{
		StringBuilder builder = new();

		logger?.Debug($"Generating MS Gen-based logging class for: {target.FullyQualifiedName}");

		var indent = EmitHelpers.EmitNamespaceStart(
			target.ClassNamespace,
			target.ParentClasses,
			builder,
			context.CancellationToken
		);
		indent = EmitHelpers.EmitClassStart(
			GenerationType.Logging,
			target.GenerationType,
			target.ClassNameToGenerate,
			target.InterfaceType,
			builder,
			indent,
			context.CancellationToken
		);

		EmitFields(target, builder, indent, context, logger, emitNullable);

		indent = ConstructorEmitter.EmitCtor(
			GenerationType.Logging,
			target.GenerationType,
			target.ClassNameToGenerate,
			target.InterfaceType,
			builder,
			indent,
			context,
			logger
		);

		indent = EmitMethods(target, builder, indent, context, logger, emitNullable);

		EmitLogStateStructs(target, builder, indent, context, logger, emitNullable);

		EmitHelpers.EmitClassEnd(builder, indent);
		EmitHelpers.EmitNamespaceEnd(
			target.ClassNamespace,
			target.ParentClasses,
			indent,
			builder,
			context.CancellationToken
		);

		var sourceText = EmbeddedResources.Instance.AddHeader(builder.ToString(), emitNullable);
		var hintName = $"{target.FullyQualifiedName}.Logging.g.cs";

		context.AddSource(
			hintName,
			Microsoft.CodeAnalysis.Text.SourceText.From(sourceText, Encoding.UTF8)
		);

		DependencyInjectionClassEmitter.GenerateImplementation(
			GenerationType.Logging,
			target.TelemetryGeneration,
			target.GenerationType,
			target.ClassNameToGenerate,
			target.InterfaceType.TypeName,
			target.FullNamespace,
			context,
			logger,
			emitNullable
		);
	}

	static void EmitFields(
		LoggerTarget target,
		StringBuilder builder,
		int indent,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		builder
			.Append(indent + 1, "readonly ", withNewLine: false)
			.Append(Constants.Logging.MicrosoftExtensions.ILogger)
			.Append('<')
			.Append(target.InterfaceType)
			.Append('>')
			.Append(' ')
			.Append(Constants.Logging.LoggerFieldName)
			.Append(';')
			.AppendLine();

		foreach (var methodTarget in target.LogMethods)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			if (!methodTarget.TargetGenerationState.IsValid)
				continue;

			if (methodTarget.UnknownReturnType)
			{
				TelemetryDiagnostics.Report(context.ReportDiagnostic, TelemetryDiagnostics.Logging.LogMustReturnVoidOrAsync);
				continue;
			}

			// Multiple exceptions is always invalid, regardless of v1 or v2 generation.
			if (methodTarget.HasMultipleExceptions)
			{
				logger?.Diagnostic(
					"Method has multiple exception parameters, only a single one is permitted."
				);
				TelemetryDiagnostics.Report(context.ReportDiagnostic, TelemetryDiagnostics.Logging.MultipleExceptionsDefined);
				continue;
			}

			if (!methodTarget.UseV1Generation)
				continue;

			if (
				methodTarget.ParameterCountSansException
				> Constants.Logging.MaxNonExceptionParameters
			)
			{
				logger?.Diagnostic("Method has more than 6 parameters.");
				TelemetryDiagnostics.Report(context.ReportDiagnostic, TelemetryDiagnostics.Logging.MaximumLogEntryParametersExceeded);
				continue;
			}

			if (methodTarget.InferredErrorLevel)
			{
				logger?.Diagnostic("Inferring error log level.");
				TelemetryDiagnostics.Report(context.ReportDiagnostic, TelemetryDiagnostics.Logging.InferringErrorLogLevel);
			}

			LoggerTargetClassEmitter.EmitLogActionField(builder, indent + 1, methodTarget, emitNullable);
		}
	}
}
