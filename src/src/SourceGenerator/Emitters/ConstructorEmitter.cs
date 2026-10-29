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
			var loggerType = TypeLibrary.Microsoft.Extensions.Logging.ILogger.MakeGeneric(interfaceType);
			builder.Add(new(LoggerParameterName, new TypeReference(loggerType)));
		}

		if (generationType.HasFlag(GenerationType.Metrics) && generationContext.Capabilities.SupportsIMeterFactory)
		{
			builder.Add(
				new ParameterDeclarationOptions(
					PropertyLibrary.Metrics.MeterFactoryParameterName,
					TypeLibrary.System.Diagnostics.Metrics.IMeterFactory
				)
			);
		}

		if (NeedsActivitySourceFactory(generationType, generationContext))
			builder.Add(ActivitySourceFactoryParameter());

		return builder.ToImmutable();
	}

	/// <summary>
	/// Whether the constructor receives an <c>ActivitySourceFactory</c>: the interface targets Activities and the
	/// assembly opted in to (and can resolve) the .NET 11 factory.
	/// </summary>
	public static bool NeedsActivitySourceFactory(
		GenerationType generationType,
		GenerationContext<TelemetryCapabilities> generationContext
	) => generationType.HasFlag(GenerationType.Activities) && generationContext.Capabilities.UseActivitySourceFactory;

	/// <summary>The <c>ActivitySourceFactory activitySourceFactory</c> constructor parameter.</summary>
	public static ParameterDeclarationOptions ActivitySourceFactoryParameter() =>
		new(
			PropertyLibrary.Activities.ActivitySourceFactoryParameterName,
			TypeLibrary.System.Diagnostics.ActivitySourceFactory
		);

	/// <summary>Writes the call that creates the activity source from the injected factory.</summary>
	public static void EmitActivitySourceInitializationCall(CodeWriter writer) =>
		writer
			.Write(PropertyLibrary.Activities.ActivitySourceInitializationMethod)
			.Write('(')
			.Write(PropertyLibrary.Activities.ActivitySourceFactoryParameterName)
			.Write(");")
			.NewLine();

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

		if (NeedsActivitySourceFactory(generationType, generationContext))
			EmitActivitySourceInitializationCall(writer);
	}
}
