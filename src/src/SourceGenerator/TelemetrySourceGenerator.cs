using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Purview.Telemetry.SourceGenerator.Helpers;

namespace Purview.Telemetry.SourceGenerator;

[Generator]
public sealed partial class TelemetrySourceGenerator : IIncrementalGenerator, ILogSupport
{
	GenerationLogger? _logger;

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// C# 8+ feature detection: controls nullable annotations and null-forgiving operators.
		var supportsNullableAnnotations = context.ParseOptionsProvider.Select(
			static (opts, _) =>
				opts is not CSharpParseOptions csOpts || csOpts.LanguageVersion >= LanguageVersion.CSharp8
		);

		// IMeterFactory is .NET 8+ only — never available on .NET Framework 4.8.
		var supportsIMeterFactory = context.ParseOptionsProvider.Select(
			static (opts, _) =>
				opts is not CSharpParseOptions csOpts || !csOpts.PreprocessorSymbolNames.Contains("NET48_OR_GREATER")
		);

		// Only .NET 8+ and .NET Framework 4.8+ are supported; fail fast for anything else.
		// A compilation with no target-framework symbols at all (e.g. an in-memory test
		// compilation) is treated as .NET 8+.
		var frameworkSupported = context.ParseOptionsProvider.Select(
			static (opts, _) =>
			{
				if (opts is not CSharpParseOptions csOpts)
					return true;

				var symbols = csOpts.PreprocessorSymbolNames;
				return symbols.Contains("NET8_0_OR_GREATER")
					|| symbols.Contains("NET48_OR_GREATER")
					|| symbols.All(static s => !s.StartsWith("NET", StringComparison.Ordinal));
			}
		);
		context.RegisterSourceOutput(
			frameworkSupported,
			static (ctx, supported) =>
			{
				if (!supported)
					TelemetryDiagnostics.Report(
						ctx.ReportDiagnostic,
						TelemetryDiagnostics.General.UnsupportedTargetFramework
					);
			}
		);

		// RegisterPostInitializationOutput (not RegisterSourceOutput) ensures ForAttributeWithMetadataName
		// can resolve attribute types before it runs.
		context.RegisterPostInitializationOutput(ctx =>
		{
			// Adds Microsoft.CodeAnalysis.EmbeddedAttribute to the compilation so generated marker
			// types (decorated with [Microsoft.CodeAnalysis.Embedded]) are invisible to downstream
			// assemblies, preventing CS0436 conflicts when multiple projects reference this generator.
			ctx.AddEmbeddedAttributeDefinition();

			_logger?.Debug("--- Adding templates.");

			foreach (var template in Constants.GetAllTemplates())
			{
				_logger?.Debug($"Adding {template.Name} as {template.GetGeneratedFilename()}.");

				ctx.AddSource(template.GetGeneratedFilename(), template.TemplateData);
			}

			_logger?.Debug("--- Finished adding templates.");
		});

		// Create shared providers so Activities/Metrics pipelines aren't registered twice.
		var activityProvider = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				Constants.Activities.ActivitySourceAttribute.TypeInfo.FullyQualifiedName,
				static (node, token) => PipelineHelpers.HasActivityTargetAttribute(node, token),
				(ctx, cancellationToken) => PipelineHelpers.BuildActivityTransform(ctx, _logger, cancellationToken)
			)
			.WhereNotNull()
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_Activities");

		var meterProvider = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				Constants.Metrics.MeterAttribute.TypeInfo.FullyQualifiedName,
				static (node, token) => PipelineHelpers.HasMeterTargetAttribute(node, token),
				(ctx, cancellationToken) => PipelineHelpers.BuildMeterTransform(ctx, _logger, cancellationToken)
			)
			.WhereNotNull()
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_Meters");

		RegisterActivitiesGeneration(context, activityProvider, supportsNullableAnnotations, _logger);
		RegisterLoggerGeneration(context, supportsNullableAnnotations, supportsIMeterFactory, _logger);
		RegisterMetricsGeneration(context, meterProvider, supportsNullableAnnotations, supportsIMeterFactory, _logger);
		RegisterTelemetryNamesGeneration(
			context,
			activityProvider,
			meterProvider,
			supportsNullableAnnotations,
			_logger
		);
	}

	void ILogSupport.SetLogOutput(Action<string, OutputType> action) =>
		_logger = action == null ? null : new GenerationLogger(action);
}
