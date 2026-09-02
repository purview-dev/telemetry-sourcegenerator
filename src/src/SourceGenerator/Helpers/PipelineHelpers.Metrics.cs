using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class PipelineHelpers
{
	public static bool HasMeterTargetAttribute(SyntaxNode _, CancellationToken __) => true;

	public static GeneratorResult<MeterTarget?> BuildMeterTransform(
		GeneratorAttributeSyntaxContext context,
		CancellationToken token
	) => BuildMeterTarget(context.TargetSymbol as INamedTypeSymbol, context.SemanticModel.Compilation, token);

	public static GeneratorResult<MeterTarget?> BuildMeterTarget(
		INamedTypeSymbol? interfaceSymbol,
		Compilation compilation,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		if (interfaceSymbol is null)
			return GeneratorResult<MeterTarget?>.Empty;

		if (interfaceSymbol.Arity > 0)
		{
			return GeneratorResult<MeterTarget?>.Create(
				DiagnosticInfo.Create(
					DiagnosticLibrary.General.GenericInterfacesNotSupported.Descriptor,
					interfaceSymbol
				)
			);
		}

		var meterData = SharedHelpers.GetMeterAttribute(interfaceSymbol, token);
		if (meterData is not { } meterAttribute)
			return GeneratorResult<MeterTarget?>.Empty;

		var telemetryGeneration = SharedHelpers.GetTelemetryGenerationAttribute(interfaceSymbol, compilation, token);
		var className = telemetryGeneration.ClassName ?? GenerateClassName(interfaceSymbol.Name);
		var generationType = SharedHelpers.GetGenerationTypes(interfaceSymbol, token);
		var meterGenerationAttribute = SharedHelpers.GetMeterGenerationAttribute(compilation, token);
		if (
			interfaceSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(token)
			is not InterfaceDeclarationSyntax interfaceDeclaration
		)
		{
			return GeneratorResult<MeterTarget?>.Empty;
		}

		var meterName = meterAttribute.Name;
		if (string.IsNullOrWhiteSpace(meterName))
		{
			// First check assembly-wide MeterName from MeterGenerationAttribute
			meterName = meterGenerationAttribute?.MeterName;

			if (string.IsNullOrWhiteSpace(meterName))
			{
				// Fall back to assembly name with generation type convention
				var assemblyName = compilation.Assembly.Name;
				var meterNameGenType = meterGenerationAttribute?.MeterNameGenerationType ?? 1; // Default to DotNet

				if (meterNameGenType == 0) // OpenTelemetry
				{
					// OpenTelemetry: lowercase assembly name
#pragma warning disable CA1308 // Intentional lowercase for OpenTelemetry convention
					meterName = assemblyName.ToLowerInvariant();
#pragma warning restore CA1308
				}
				else // DotNet
				{
					// .NET: preserve assembly name as-is
					meterName = assemblyName;
				}
			}
		}

		var instrumentMethods = InstrumentMethodModelBuilder.BuildInstrumentationMethods(
			generationType,
			meterAttribute,
			meterGenerationAttribute,
			telemetryGeneration,
			meterName!,
			interfaceSymbol,
			token
		);

		return GeneratorResult<MeterTarget?>.Create(
			new(
				TelemetryGeneration: telemetryGeneration,
				GenerationType: generationType,
				ClassNameToGenerate: className,
				ParentClasses: Utilities.GetParentClasses(interfaceDeclaration),
				InterfaceType: TypeReference.Create(interfaceSymbol),
				MeterName: meterName,
				MeterGeneration: meterGenerationAttribute,
				InstrumentationMethods: instrumentMethods
			),
			TelemetryRules.GetInterfaceLevelDiagnostics(interfaceSymbol, compilation, token)
		);
	}
}
