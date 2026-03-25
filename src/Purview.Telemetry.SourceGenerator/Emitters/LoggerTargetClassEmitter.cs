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
		GenerationLogger? logger,
		bool emitNullable = true,
		bool supportsIMeterFactory = true
	)
	{
		StringBuilder builder = new();

		logger?.Debug($"Generating logging class for: {target.FullyQualifiedName}");

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

		indent = EmitFields(target, builder, indent, context, logger, emitNullable);

		indent = ConstructorEmitter.EmitCtor(
			GenerationType.Logging,
			target.GenerationType,
			target.ClassNameToGenerate,
			target.InterfaceType,
			builder,
			indent,
			context,
			logger,
			supportsIMeterFactory
		);

		indent = EmitMethods(target, builder, indent, context, logger, emitNullable);

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
}
