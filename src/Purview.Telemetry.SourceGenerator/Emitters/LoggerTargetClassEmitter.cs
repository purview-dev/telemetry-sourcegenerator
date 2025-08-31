using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Emitters;

static partial class LoggerTargetClassEmitter
{
	public static void GenerateImplementation(
		LoggerTarget target,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		StringBuilder builder = new();

		logger?.Debug($"Generating logging class for: {target.FullyQualifiedName}");

		if (
			EmitHelpers.GenerateDuplicateMethodDiagnostics(
				GenerationType.Logging,
				target.GenerationType,
				target.DuplicateMethods,
				context,
				logger
			)
		)
		{
			logger?.Debug("Found duplicate methods while generating logger, exiting.");
			return;
		}

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

		indent = EmitFields(target, builder, indent, context, logger);

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

		indent = EmitMethods(target, builder, indent, context, logger);

		EmitHelpers.EmitClassEnd(builder, indent);
		EmitHelpers.EmitNamespaceEnd(
			target.ClassNamespace,
			target.ParentClasses,
			indent,
			builder,
			context.CancellationToken
		);

		EmbeddedResources.Instance.AddHeader(builder);
		var hintName = $"{target.FullyQualifiedName}.Logging.g.cs";

		context.AddSource(
			hintName,
			Microsoft.CodeAnalysis.Text.SourceText.From(builder.ToString(), Encoding.UTF8)
		);

		DependencyInjectionClassEmitter.GenerateImplementation(
			GenerationType.Logging,
			target.TelemetryGeneration,
			target.GenerationType,
			target.ClassNameToGenerate,
			target.InterfaceType.TypeName,
			target.FullNamespace,
			context,
			logger
		);
	}
}
