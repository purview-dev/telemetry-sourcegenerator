using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Emitters;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator;

partial class TelemetrySourceGenerator
{
	static void RegisterMultiTargetGeneration(
		IncrementalGeneratorInitializationContext context,
		GenerationLogger? logger
	)
	{
		// Transform for multi-target interfaces – do NOT require [TelemetryGeneration] on the interface
		// Scan all interface declarations and decide in the transform whether to emit
		Func<
			GeneratorSyntaxContext,
			CancellationToken,
			MultiTargetInterface?
		> multiTargetTransformFromInterface =
			logger == null
				? static (gctx, cancellationToken) =>
					PipelineHelpers.BuildMultiTargetTransformFromInterface(gctx, null, cancellationToken)
				: (gctx, cancellationToken) =>
					PipelineHelpers.BuildMultiTargetTransformFromInterface(gctx, logger, cancellationToken);

		var multiTargetInterfacesPredicate = context
			.SyntaxProvider.CreateSyntaxProvider(
				static (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.InterfaceDeclarationSyntax,
				multiTargetTransformFromInterface
			)
			.WhereNotNull()
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_MultiTarget");

		// Build generation action
		Action<
			SourceProductionContext,
			(Compilation Compilation, ImmutableArray<MultiTargetInterface?> Interfaces)
		> generationMultiTargetAction =
			logger == null
				? static (spc, source) => GenerateMultiTargetInterfaces(source.Interfaces, spc, null)
				: (spc, source) => GenerateMultiTargetInterfaces(source.Interfaces, spc, logger);

		// Register with the source generator
		var multiTargetInterfaces = context.CompilationProvider.Combine(
			multiTargetInterfacesPredicate.Collect()
		);

		context.RegisterImplementationSourceOutput(multiTargetInterfaces, generationMultiTargetAction);
	}

	static void GenerateMultiTargetInterfaces(
		ImmutableArray<MultiTargetInterface?> interfaces,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		var filteredInterfaces = interfaces.Where(i => i != null).Cast<MultiTargetInterface>().ToArray();

		if (filteredInterfaces.Length == 0)
			return;

		foreach (var targetInterface in filteredInterfaces)
		{
			try
			{
				MultiTargetClassEmitter.GenerateImplementation(targetInterface, context, logger);
			}
			catch (Exception ex)
			{
				logger?.Error(
					$"Error generating multi-target implementation for {targetInterface.InterfaceName}: {ex.Message}"
				);
				TelemetryDiagnostics.Report(
					context.ReportDiagnostic,
					TelemetryDiagnostics.General.FatalExecutionDuringExecution,
					ex
				);
			}
		}
	}
}
