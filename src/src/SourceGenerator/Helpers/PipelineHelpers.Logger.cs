using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class PipelineHelpers
{
	public static bool HasLoggerTargetAttribute(SyntaxNode _, CancellationToken __) => true;

	public static GeneratorResult<LoggerTarget?> BuildLoggerTransform(
		GeneratorAttributeSyntaxContext context,
		ISourceGenLogger? logger,
		CancellationToken token
	) => BuildLoggerTarget(context.TargetSymbol as INamedTypeSymbol, context.SemanticModel.Compilation, logger, token);

	public static GeneratorResult<LoggerTarget?> BuildLoggerTarget(
		INamedTypeSymbol? interfaceSymbol,
		Compilation compilation,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		if (interfaceSymbol is null)
		{
			logger?.Fatal($"Could not find the interface symbol for a Logger target.");
			return GeneratorResult<LoggerTarget?>.Empty;
		}

		var iLoggerTypeSymbol = compilation.GetTypeByMetadataName(
			TypeLibrary.Logging.MicrosoftExtensions.ILogger.MetadataFullName
		);
		if (iLoggerTypeSymbol is null)
		{
			logger?.Diagnostic(
				$"Requested a Logger target to be generated, but could not find the ILogger symbol referenced '{interfaceSymbol.Name}'."
			);
			return GeneratorResult<LoggerTarget?>.Empty;
		}

		if (interfaceSymbol.Arity > 0)
		{
			logger?.Diagnostic($"Cannot generate a Logger target for a generic interface '{interfaceSymbol.Name}'.");

			return GeneratorResult<LoggerTarget?>.Create(
				DiagnosticInfo.Create(
					TelemetryRules.ToDescriptor(DiagnosticLibrary.General.GenericInterfacesNotSupported),
					interfaceSymbol
				)
			);
		}

		var loggerAttribute = SharedHelpers.GetLoggerAttribute(interfaceSymbol, token);
		if (loggerAttribute == null)
		{
			logger?.Fatal(
				$"Could not find {TemplateLibrary.Logging.LoggerAttribute} when one was expected '{interfaceSymbol.Name}'."
			);
			return GeneratorResult<LoggerTarget?>.Empty;
		}

		var telemetryGeneration = SharedHelpers.GetTelemetryGenerationAttribute(interfaceSymbol, compilation, token);
		var className = telemetryGeneration.ClassName ?? GenerateClassName(interfaceSymbol.Name);

		var loggerGenerationAttribute = SharedHelpers.GetLoggerGenerationAttribute(compilation, token);
		var defaultLogLevel = loggerGenerationAttribute?.DefaultLevel ?? PropertyLibrary.Logging.DefaultLevel;
		var defaultPrefixType = loggerGenerationAttribute?.DefaultPrefixType ?? 0;

		// Resolve the effective generation mode using priority:
		// interface GenerationMode > assembly GenerationMode > Auto (per-method decision)
		var interfaceGenerationMode = loggerAttribute.GenerationMode ?? loggerGenerationAttribute?.GenerationMode ?? 0; // Auto

		var generationType = SharedHelpers.GetGenerationTypes(interfaceSymbol, token);
		if (
			interfaceSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(token)
			is not InterfaceDeclarationSyntax interfaceDeclaration
		)
		{
			logger?.Fatal($"Could not locate the declaring syntax for '{interfaceSymbol.Name}'.");
			return GeneratorResult<LoggerTarget?>.Empty;
		}

		var fullNamespace = Utilities.GetFullNamespace(interfaceDeclaration, true);
		var logMethods = LogMethodModelBuilder.BuildLogMethods(
			generationType,
			className,
			defaultLogLevel,
			defaultPrefixType,
			loggerAttribute,
			compilation,
			interfaceSymbol,
			logger,
			interfaceGenerationMode: interfaceGenerationMode,
			token
		);

		return GeneratorResult<LoggerTarget?>.Create(
			new(
				TelemetryGeneration: telemetryGeneration,
				GenerationType: generationType,
				ClassNameToGenerate: className,
				ClassNamespace: Utilities.GetNamespace(interfaceDeclaration),
				ParentClasses: Utilities.GetParentClasses(interfaceDeclaration),
				FullNamespace: fullNamespace,
				FullyQualifiedName: fullNamespace + className,
				InterfaceType: TypeReference.Create(interfaceSymbol),
				LoggerAttribute: loggerAttribute,
				DefaultLevel: defaultLogLevel,
				LogMethods: logMethods,
				UseMSLoggingTelemetryBasedGeneration: interfaceGenerationMode != 1 // false only when V1 forced
			),
			TelemetryRules.GetInterfaceLevelDiagnostics(interfaceSymbol, compilation, token)
		);
	}
}
