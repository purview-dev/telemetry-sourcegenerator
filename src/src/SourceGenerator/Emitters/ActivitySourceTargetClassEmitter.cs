using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Emitters;

static partial class ActivitySourceTargetClassEmitter
{
	public static void GenerateImplementation(
		ActivitySourceTarget target,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable = true
	)
	{
		StringBuilder builder = new();

		logger?.Debug($"Generating activity class for: {target.FullyQualifiedName}");

		var indent = EmitHelpers.EmitNamespaceStart(
			target.ClassNamespace,
			target.ParentClasses,
			builder,
			context.CancellationToken
		);
		indent = EmitHelpers.EmitClassStart(
			GenerationType.Activities,
			target.GenerationType,
			target.ClassNameToGenerate,
			target.InterfaceType,
			builder,
			indent,
			context.CancellationToken
		);

		indent = EmitFields(target, builder, indent, context, logger);
		indent = EmitMethods(target, builder, indent, context, logger, emitNullable);

		EmitterHelpers.EmitTargetSource(
			target.ClassNamespace,
			target.ParentClasses,
			indent,
			builder,
			hintName: $"{target.FullyQualifiedName}.Activity.g.cs",
			context,
			emitNullable
		);

		DependencyInjectionClassEmitter.GenerateImplementation(
			GenerationType.Activities,
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
