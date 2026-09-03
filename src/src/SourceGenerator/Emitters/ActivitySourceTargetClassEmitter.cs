using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

static partial class ActivitySourceTargetClassEmitter
{
	public static void GenerateImplementation(ActivityOutputContext output, SourceProductionContext context)
	{
		var target = output.Target;
		//var emitNullable = output.Context.Capabilities.SupportsNullableAnnotations;

		output.Context.Debug($"Generating activity class for: {target.FullyQualifiedName}");

		var writer = output.CreateWriter();
		using (writer.WriteBlockNamespaceScope(target.ClassNamespace))
		{
			List<CodeWriter.BlockScope> parentScopes = [];
			if (target.TelemetryGeneration.TelemetryNamesNamespace == null)
			{
				foreach (var parent in target.ParentClasses)
				{
					parentScopes.Add(
						writer.WriteClassScope(new(parent) { IsSealed = false, IncludeGeneratedAttributes = false })
					);
				}
			}

			using (
				EmitterHelpers.EmitClassScope(
					writer,
					GenerationType.Activities,
					target.GenerationType,
					target.ClassNameToGenerate,
					target.InterfaceType,
					TypeDeclarationAccessibility.Internal
				)
			)
			{
				EmitFields(output, writer, context);
				EmitMethods(output, writer, context);
			}

			foreach (var scope in parentScopes)
				scope.Dispose();
		}

		context.AddSource($"{target.FullyQualifiedName}.Activity.g.cs", writer);

		DependencyInjectionClassEmitter.GenerateImplementation(
			output.CreateWriter(),
			GenerationType.Activities,
			target.TelemetryGeneration,
			target.GenerationType,
			target.ClassNameToGenerate,
			target.InterfaceType,
			context,
			output.Context
		);
	}
}
