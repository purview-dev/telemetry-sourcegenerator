using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

static partial class LoggerTargetClassEmitter
{
	public static void GenerateImplementation(LoggerOutputContext output, SourceProductionContext context)
	{
		var target = output.Target;
		output.Context.Debug($"Generating logging class for: {target.FullyQualifiedName}");

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
					GenerationType.Logging,
					target.GenerationType,
					target.ClassNameToGenerate,
					target.InterfaceType,
					TypeDeclarationAccessibility.Internal
				)
			)
			{
				EmitFields(output, writer, context);

				ConstructorEmitter.EmitCtor(
					GenerationType.Logging,
					target.GenerationType,
					target.ClassNameToGenerate,
					target.InterfaceType,
					writer,
					context,
					output.Context
				);

				EmitMethods(output, writer, context);
			}

			foreach (var scope in parentScopes)
				scope.Dispose();
		}

		context.AddSource($"{target.FullyQualifiedName}.Logging.g.cs", writer);

		DependencyInjectionClassEmitter.GenerateImplementation(
			output.CreateWriter(),
			GenerationType.Logging,
			target.TelemetryGeneration,
			target.GenerationType,
			target.ClassNameToGenerate,
			target.InterfaceType,
			context,
			output.Context
		);
	}
}
