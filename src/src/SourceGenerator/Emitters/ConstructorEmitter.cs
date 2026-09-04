using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

static class ConstructorEmitter
{
	const string LoggerParameterName = "logger";

	public static void EmitCtor(
		GenerationType requestingType,
		GenerationType generationType,
		string classNameToGenerate,
		TypeReference interfaceType,
		CodeWriter writer,
		SourceProductionContext context,
		GenerationContext<TelemetryCapabilities> generationContext
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		// Only emit constructor from one target to avoid duplicate definitions
		if (!SharedHelpers.ShouldEmitConstructor(requestingType, generationType))
		{
			generationContext.Debug($"Skipping constructor emit for {requestingType} ({generationType}).");

			return;
		}

		writer.NewLine();
		writer.Constructor(
			new ConstructorDeclarationOptions(classNameToGenerate, TypeDeclarationAccessibility.Public)
			{
				Parameters = BuildParameters(generationType, interfaceType, generationContext),
				IncludeGeneratedAttributes = false,
			},
			body => EmitBody(generationType, body, generationContext)
		);
	}

	static ImmutableArray<ParameterDeclarationOptions> BuildParameters(
		GenerationType generationType,
		TypeReference interfaceType,
		GenerationContext<TelemetryCapabilities> generationContext
	)
	{
		var builder = ImmutableArray.CreateBuilder<ParameterDeclarationOptions>();

		if (generationType.HasFlag(GenerationType.Logging))
		{
			var loggerType = TypeLibrary.Logging.MicrosoftExtensions.ILogger.MakeGeneric(interfaceType);
			builder.Add(new ParameterDeclarationOptions(LoggerParameterName, new TypeReference(loggerType)));
		}

		if (generationType.HasFlag(GenerationType.Metrics) && generationContext.Capabilities.SupportsIMeterFactory)
		{
			builder.Add(
				new ParameterDeclarationOptions(
					PropertyLibrary.Metrics.MeterFactoryParameterName,
					TypeLibrary.Metrics.SystemDiagnostics.IMeterFactory
				)
			);
		}

		return builder.ToImmutable();
	}

	static void EmitBody(
		GenerationType generationType,
		CodeWriter writer,
		GenerationContext<TelemetryCapabilities> generationContext
	)
	{
		if (generationType.HasFlag(GenerationType.Logging))
		{
			writer
				.Write(PropertyLibrary.Logging.LoggerFieldName)
				.Write(" = ")
				.Write(LoggerParameterName)
				.Write(";")
				.NewLine();
		}

		if (generationType.HasFlag(GenerationType.Metrics))
		{
			writer.Write(PropertyLibrary.Metrics.MeterInitializationMethod).Write('(');

			if (generationContext.Capabilities.SupportsIMeterFactory)
				writer.Write(PropertyLibrary.Metrics.MeterFactoryParameterName);

			writer.Write(");").NewLine();
		}
	}
}
