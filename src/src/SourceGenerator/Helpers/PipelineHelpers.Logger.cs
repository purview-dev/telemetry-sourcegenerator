using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class PipelineHelpers
{
	public static bool HasLoggerTargetAttribute(SyntaxNode _, CancellationToken __) => true;

	public static GeneratorResult<LoggerTarget?> BuildLoggerTransform(
		GeneratorAttributeSyntaxContext context,
		CancellationToken token
	) => BuildLoggerTarget(context.TargetSymbol as INamedTypeSymbol, context.SemanticModel.Compilation, token);

	public static GeneratorResult<LoggerTarget?> BuildLoggerTarget(
		INamedTypeSymbol? interfaceSymbol,
		Compilation compilation,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		if (interfaceSymbol is null)
			return GeneratorResult<LoggerTarget?>.Empty;

		var iLoggerTypeSymbol = compilation.GetTypeByMetadataName(
			TypeLibrary.Microsoft.Extensions.Logging.ILogger.MetadataFullName
		);
		if (iLoggerTypeSymbol is null)
			return GeneratorResult<LoggerTarget?>.Empty;

		if (interfaceSymbol.Arity > 0 && !SharedHelpers.AreInterfaceTypeParametersSupported(interfaceSymbol))
		{
			return GeneratorResult<LoggerTarget?>.Create(
				ReportableDiagnostic.Create(
					DiagnosticLibrary.General.GenericInterfacesNotSupported.Descriptor,
					isBlocking: true,
					interfaceSymbol,
					interfaceSymbol.Name
				)
			);
		}

		var loggerData = SharedHelpers.GetLoggerAttribute(interfaceSymbol, token);
		if (loggerData is not { } loggerAttribute)
			return GeneratorResult<LoggerTarget?>.Empty;

		var telemetryGeneration = SharedHelpers.GetTelemetryGenerationAttribute(interfaceSymbol, compilation, token);
		var className = telemetryGeneration.ClassName ?? GenerateClassName(interfaceSymbol.Name);

		var loggerGenerationAttribute = SharedHelpers.GetLoggerGenerationAttribute(compilation, token);
		var defaultLogLevel = loggerGenerationAttribute?.DefaultLevelOrNull ?? PropertyLibrary.Logging.DefaultLevel;
		var defaultPrefixType = loggerGenerationAttribute?.DefaultPrefixTypeOrNull ?? 0;

		// Resolve the effective generation mode using priority:
		// interface GenerationMode > assembly GenerationMode > Auto (per-method decision)
		var interfaceGenerationMode = loggerAttribute.GenerationModeOrNull
			?? loggerGenerationAttribute?.GenerationModeOrNull
			?? 0; // Auto

		var generationType = SharedHelpers.GetGenerationTypes(interfaceSymbol, token);
		if (
			interfaceSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(token)
			is not InterfaceDeclarationSyntax interfaceDeclaration
		)
		{
			return GeneratorResult<LoggerTarget?>.Empty;
		}

		var logMethods = LogMethodModelBuilder.BuildLogMethods(
			generationType,
			className,
			defaultLogLevel,
			defaultPrefixType,
			loggerAttribute,
			compilation,
			interfaceSymbol,
			interfaceGenerationMode: interfaceGenerationMode,
			token
		);

		// A [TagProvider] produces its tags at run time, which the LoggerMessage.Define-based emitter cannot carry.
		// Rather than dropping them an interface that forced V1 is generated with the state-based emitter.
		var requiresStateGeneration = logMethods.Any(static method =>
			method.Parameters.Any(static parameter => parameter.TagProvider != null)
		);

		return GeneratorResult<LoggerTarget?>.Create(
			new(
				TelemetryGeneration: telemetryGeneration,
				GenerationType: generationType,
				ClassNameToGenerate: className,
				ParentClasses: Utilities.GetParentClasses(interfaceDeclaration),
				InterfaceType: SharedHelpers.GetInterfaceTypeReference(interfaceSymbol),
				LoggerAttribute: loggerAttribute,
				DefaultLevel: defaultLogLevel,
				LogMethods: logMethods,
				UseMSLoggingTelemetryBasedGeneration: interfaceGenerationMode != 1 || requiresStateGeneration,
				InterfaceTypeParameters: TypeParameterHelpers.FromNamedType(interfaceSymbol)
			),
			TelemetryRules.GetInterfaceLevelDiagnostics(interfaceSymbol, compilation, token)
		);
	}
}
