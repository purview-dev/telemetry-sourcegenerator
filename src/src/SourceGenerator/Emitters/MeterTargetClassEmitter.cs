using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

static partial class MeterTargetClassEmitter
{
	static TypeReference GetDictionaryType(CodeWriter writer) =>
		TypeLibrary.System.Dictionary.MakeGeneric(
			PurviewTypeLibrary.System.String.AsTypeReference(),
			PurviewTypeLibrary.System.Object.AsTypeReference().Nullable(writer)
		);

	const string MeterFieldName = "_meter";
	const string PartialMeterTagsMethod = "PopulateMeterTags";

	public static void GenerateImplementation(MeterOutputContext output, SourceProductionContext context)
	{
		var target = output.Target;
		output.Context.Debug($"Generating metric class for: {target.FullyQualifiedName}");

		var writer = output.CreateWriter();
		using (writer.WriteBlockNamespaceScope(target.ClassNamespace))
		{
			List<CodeWriter.BlockScope> parentScopes = [];
			if (target.TelemetryGeneration.TelemetryNamesNamespace == null)
			{
				foreach (var parent in target.ParentClasses)
				{
					parentScopes.Add(
						writer.WriteClassScope(
							new TypeDeclarationOptions(parent) { IsSealed = false, IncludeGeneratedAttributes = false }
						)
					);
				}
			}

			using (
				EmitterHelpers.EmitClassScope(
					writer,
					GenerationType.Metrics,
					target.GenerationType,
					target.ClassNameToGenerate,
					target.InterfaceType,
					TypeDeclarationAccessibility.Internal
				)
			)
			{
				// When metrics owns the constructor (no Logging target), emit readonly fields and
				// inline the initialisation directly into the constructor for JIT-optimisable code.
				// When Logging owns the constructor, keep the InitializeMeters() helper method path
				// so the Logging emitter can call it.
				var metricsOwnsConstructor = SharedHelpers.ShouldEmitConstructor(
					GenerationType.Metrics,
					target.GenerationType
				);

				EmitFields(output, writer, context, readonlyFields: metricsOwnsConstructor);

				if (metricsOwnsConstructor)
					EmitInlineConstructor(output, writer, context);
				else
				{
					ConstructorEmitter.EmitCtor(
						GenerationType.Metrics,
						target.GenerationType,
						target.ClassNameToGenerate,
						target.InterfaceType,
						writer,
						context,
						output.Context
					);

					EmitInitializationMethod(output, writer, context);
				}

				EmitMethods(output, writer, context);
			}

			foreach (var scope in parentScopes)
				scope.Dispose();
		}

		context.AddSource($"{target.FullyQualifiedName}.Metric.g.cs", writer);

		DependencyInjectionClassEmitter.GenerateImplementation(
			output.CreateWriter(),
			GenerationType.Metrics,
			target.TelemetryGeneration,
			target.GenerationType,
			target.ClassNameToGenerate,
			target.InterfaceType,
			context,
			output.Context
		);
	}
}
