using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Emitters;

static partial class MeterTargetClassEmitter
{
	static readonly PurviewTypeInfo DictionaryStringObjectType =
		Constants.System.Dictionary.MakeGeneric(
			Constants.System.BuiltInTypes.String,
			Constants.System.BuiltInTypes.Object.WithNullable()
		);

	const string MeterFieldName = "_meter";
	const string PartialMeterTagsMethod = "PopulateMeterTags";

	public static void GenerateImplementation(
		MeterTarget target,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		StringBuilder builder = new();

		logger?.Debug($"Generating metric class for: {target.FullyQualifiedName}");

		if (
			EmitHelpers.GenerateDuplicateMethodDiagnostics(
				GenerationType.Metrics,
				target.GenerationType,
				target.DuplicateMethods,
				context,
				logger
			)
		)
		{
			logger?.Debug("Found duplicate methods while generating metrics, exiting.");
			return;
		}

		var indent = EmitHelpers.EmitNamespaceStart(
			target.ClassNamespace,
			target.ParentClasses,
			builder,
			context.CancellationToken
		);

		indent = EmitHelpers.EmitClassStart(
			GenerationType.Metrics,
			target.GenerationType,
			target.ClassNameToGenerate,
			target.InterfaceType,
			builder,
			indent,
			context.CancellationToken
		);

		indent = EmitFields(target, builder, indent, context, logger);
		indent = ConstructorEmitter.EmitCtor(
			GenerationType.Metrics,
			target.GenerationType,
			target.ClassNameToGenerate,
			target.InterfaceType,
			builder,
			indent,
			context,
			logger
		);

		indent = EmitInitializationMethod(target, builder, indent, context);
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
		var hintName = $"{target.FullyQualifiedName}.Metric.g.cs";

		context.AddSource(
			hintName,
			Microsoft.CodeAnalysis.Text.SourceText.From(builder.ToString(), Encoding.UTF8)
		);

		DependencyInjectionClassEmitter.GenerateImplementation(
			GenerationType.Metrics,
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
