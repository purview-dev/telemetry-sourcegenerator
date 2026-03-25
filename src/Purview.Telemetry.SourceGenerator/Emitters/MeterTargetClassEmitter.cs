using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Emitters;

static partial class MeterTargetClassEmitter
{
	static PurviewTypeInfo GetDictionaryType(bool emitNullable) =>
		Constants.System.Dictionary.MakeGeneric(
			Constants.System.BuiltInTypes.String,
			emitNullable
				? Constants.System.BuiltInTypes.Object.WithNullable()
				: Constants.System.BuiltInTypes.Object
		);

	const string MeterFieldName = "_meter";
	const string PartialMeterTagsMethod = "PopulateMeterTags";

	public static void GenerateImplementation(
		MeterTarget target,
		SourceProductionContext context,
		GenerationLogger? logger,
		bool emitNullable = true
	)
	{
		StringBuilder builder = new();

		logger?.Debug($"Generating metric class for: {target.FullyQualifiedName}");

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

		// When metrics owns the constructor (no Logging target), emit readonly fields and
		// inline the initialisation directly into the constructor for JIT-optimisable code.
		// When Logging owns the constructor, keep the InitializeMeters() helper method path
		// so the Logging emitter can call it.
		var metricsOwnsConstructor = SharedHelpers.ShouldEmitConstructor(
			GenerationType.Metrics,
			target.GenerationType
		);

		indent = EmitFields(target, builder, indent, context, logger, readonlyFields: metricsOwnsConstructor, emitNullable: emitNullable);

		if (metricsOwnsConstructor)
		{
			indent = EmitInlineConstructor(target, builder, indent, context, emitNullable);
		}
		else
		{
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

			indent = EmitInitializationMethod(target, builder, indent, context, emitNullable);
		}
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
		var hintName = $"{target.FullyQualifiedName}.Metric.g.cs";

		context.AddSource(
			hintName,
			Microsoft.CodeAnalysis.Text.SourceText.From(sourceText, Encoding.UTF8)
		);

		DependencyInjectionClassEmitter.GenerateImplementation(
			GenerationType.Metrics,
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
