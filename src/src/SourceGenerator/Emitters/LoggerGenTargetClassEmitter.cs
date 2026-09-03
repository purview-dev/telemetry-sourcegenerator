using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

static partial class LoggerGenTargetClassEmitter
{
	public static void GenerateImplementation(LoggerOutputContext output, SourceProductionContext context)
	{
		var target = output.Target;
		output.Context.Debug($"Generating MS Gen-based logging class for: {target.FullyQualifiedName}");

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

				EmitLogStateStructs(output, writer, context);
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

	static void EmitFields(LoggerOutputContext output, CodeWriter writer, SourceProductionContext context)
	{
		var target = output.Target;

		context.CancellationToken.ThrowIfCancellationRequested();

		writer
			.WriteField(
				new FieldDeclarationOptions(
					PropertyLibrary.Logging.LoggerFieldName,
					TypeLibrary.Logging.MicrosoftExtensions.ILogger.MakeGeneric(target.InterfaceType).AsTypeReference()
				)
				{
					IsReadOnly = true,
					IncludeGeneratedAttributes = false,
				}
			)
			.NewLine();

		foreach (var methodTarget in target.LogMethods)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			if (!methodTarget.TargetGenerationState.IsValid)
				continue;

			if (methodTarget.UnknownReturnType)
			{
				continue;
			}

			// Multiple exceptions is always invalid, regardless of v1 or v2 generation.
			if (methodTarget.HasMultipleExceptions)
			{
				output.Context.Diagnostic("Method has multiple exception parameters, only a single one is permitted.");
				continue;
			}

			if (!methodTarget.UseV1Generation)
				continue;

			if (methodTarget.ParameterCountSansException > PropertyLibrary.Logging.MaxNonExceptionParameters)
			{
				output.Context.Diagnostic("Method has more than 6 parameters.");
				continue;
			}

			if (methodTarget.InferredErrorLevel)
			{
				output.Context.Diagnostic("Inferring error log level.");
			}

			LoggerTargetClassEmitter.EmitLogActionField(writer, methodTarget);
		}
	}
}
