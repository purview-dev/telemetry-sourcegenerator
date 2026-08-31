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
		GenerationLogger? logger,
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

		writer
			.NewLine()
			.WriteLine(Constants.System.GeneratedCode.Value)
			.Write("public ")
			.Write(classNameToGenerate)
			.Write('(');

		EmitParameters(generationType, fullyQualifiedInterfaceName, writer, supportsIMeterFactory);

		writer.Write(")");

		using (writer.OpenBlockScope())
		{
			EmitBody(generationType, writer, supportsIMeterFactory);
		}
	}

	static void EmitParameters(
		GenerationType generationType,
		string? loggerFullyQualifiedInterfaceName,
		CodeWriter writer,
		bool supportsIMeterFactory
	)
	{
		if (generationType.HasFlag(GenerationType.Logging))
		{
			writer
				.Write(Constants.Logging.MicrosoftExtensions.ILogger)
				.Write('<')
				.Write(loggerFullyQualifiedInterfaceName)
				.Write("> ")
				.Write(LoggerParameterName);
		}

		if (generationType.HasFlag(GenerationType.Metrics) && supportsIMeterFactory)
		{
			if (generationType.HasFlag(GenerationType.Logging))
				writer.Write(", ");

			writer
				.Write(Constants.Metrics.SystemDiagnostics.IMeterFactory)
				.Write(' ')
				.Write(Constants.Metrics.MeterFactoryParameterName);
		}
	}

	static void EmitBody(GenerationType generationType, CodeWriter writer, bool supportsIMeterFactory)
	{
		if (generationType.HasFlag(GenerationType.Logging))
		{
			writer
				.Write(Constants.Logging.LoggerFieldName)
				.Write(" = ")
				.Write(LoggerParameterName)
				.Write(";")
				.NewLine();
		}

		if (generationType.HasFlag(GenerationType.Metrics))
		{
			writer.Write(Constants.Metrics.MeterInitializationMethod).Write('(');

			if (supportsIMeterFactory)
				writer.Write(Constants.Metrics.MeterFactoryParameterName);

			writer.Write(");").NewLine();
		}
	}
}
