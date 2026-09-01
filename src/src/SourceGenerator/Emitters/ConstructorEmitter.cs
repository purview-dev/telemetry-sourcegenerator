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
		string fullyQualifiedInterfaceName,
		CodeWriter writer,
		SourceProductionContext context,
		ISourceGenLogger? logger,
		bool supportsIMeterFactory = true
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		// Only emit constructor from one target to avoid duplicate definitions
		if (!SharedHelpers.ShouldEmitConstructor(requestingType, generationType))
		{
			logger?.Debug($"Skipping constructor emit for {requestingType} ({generationType}).");

			return;
		}

		writer.NewLine();
		writer.WriteConstructor(
			new ConstructorDeclarationOptions(classNameToGenerate, TypeDeclarationAccessibility.Public)
			{
				Parameters = BuildParameters(generationType, fullyQualifiedInterfaceName, supportsIMeterFactory),
				IncludeGeneratedAttributes = false,
			},
			body => EmitBody(generationType, body, supportsIMeterFactory)
		);
	}

	static ImmutableArray<ParameterDeclarationOptions> BuildParameters(
		GenerationType generationType,
		string? loggerFullyQualifiedInterfaceName,
		bool supportsIMeterFactory
	)
	{
		var builder = ImmutableArray.CreateBuilder<ParameterDeclarationOptions>();

		if (generationType.HasFlag(GenerationType.Logging))
		{
			var loggerType = TypeLibrary.Logging.MicrosoftExtensions.ILogger.MakeGeneric(
				new TypeReference(new TypeIdentity(loggerFullyQualifiedInterfaceName!, null))
			);
			builder.Add(new ParameterDeclarationOptions(LoggerParameterName, new TypeReference(loggerType)));
		}

		if (generationType.HasFlag(GenerationType.Metrics) && supportsIMeterFactory)
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

	static void EmitBody(GenerationType generationType, CodeWriter writer, bool supportsIMeterFactory)
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

			if (supportsIMeterFactory)
				writer.Write(PropertyLibrary.Metrics.MeterFactoryParameterName);

			writer.Write(");").NewLine();
		}
	}
}
