using System.Collections.Immutable;
using System.Text;
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
		// Transform for multi-target methods
		Func<
			GeneratorAttributeSyntaxContext,
			CancellationToken,
			MultiTargetMethod?
		> multiTargetTransform =
			logger == null
				? static (context, cancellationToken) =>
					PipelineHelpers.BuildMultiTargetTransform(context, null, cancellationToken)
				: (context, cancellationToken) =>
					PipelineHelpers.BuildMultiTargetTransform(context, logger, cancellationToken);

		// Register for methods with TelemetryAttribute
		var multiTargetMethodsPredicate = context
			.SyntaxProvider.ForAttributeWithMetadataName(
				Constants.Shared.TelemetryAttribute.FullyQualifiedName,
				static (node, token) => PipelineHelpers.HasMultiTargetAttribute(node, token),
				multiTargetTransform
			)
			.WhereNotNull()
			.WithTrackingName($"{nameof(TelemetrySourceGenerator)}_MultiTarget");

		// Build generation action
		Action<
			SourceProductionContext,
			(Compilation Compilation, ImmutableArray<MultiTargetMethod?> Methods)
		> generationMultiTargetAction =
			logger == null
				? static (spc, source) => GenerateMultiTargetMethods(source.Methods, spc, null)
				: (spc, source) => GenerateMultiTargetMethods(source.Methods, spc, logger);

		// Register with the source generator
		var multiTargetMethods = context.CompilationProvider.Combine(
			multiTargetMethodsPredicate.Collect()
		);

		context.RegisterImplementationSourceOutput(multiTargetMethods, generationMultiTargetAction);
	}

	static void GenerateMultiTargetMethods(
		ImmutableArray<MultiTargetMethod?> methods,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		var filteredMethods = methods.Where(m => m != null).Cast<MultiTargetMethod>().ToArray();

		if (filteredMethods.Length == 0)
			return;

		// Group methods by containing type to generate one implementation class per interface
		var methodsByType = filteredMethods
			.GroupBy(m => new { m.ContainingTypeName, m.Namespace })
			.ToArray();

		foreach (var typeGroup in methodsByType)
		{
			try
			{
				GenerateMultiTargetImplementation(typeGroup.ToArray(), context, logger);
			}
			catch (Exception ex)
			{
				logger?.Error(
					$"Error generating multi-target implementation for {typeGroup.Key.ContainingTypeName}: {ex.Message}"
				);
				TelemetryDiagnostics.Report(
					context.ReportDiagnostic,
					TelemetryDiagnostics.General.FatalExecutionDuringExecution,
					ex
				);
			}
		}
	}

	static void GenerateMultiTargetImplementation(
		MultiTargetMethod[] methods,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		if (methods.Length == 0) return;

		var firstMethod = methods[0];
		var containingTypeName = firstMethod.ContainingTypeName;
		var namespaceName = firstMethod.Namespace;
		var implementationClassName = containingTypeName.Replace("I", "") + "Implementation";

		logger?.Debug($"Generating multi-target implementation for: {containingTypeName}");

		StringBuilder builder = new();
		var indent = 0;

		// Generate file header
		EmbeddedResources.Instance.AddHeader(builder);

		// Generate namespace
		if (!string.IsNullOrEmpty(namespaceName))
		{
			builder.AppendLine($"namespace {namespaceName};").AppendLine();
		}

		// Generate class declaration
		builder.AppendLine($"partial class {implementationClassName} : {containingTypeName}")
			.AppendLine("{");

		indent = 1;

		// Generate fields for telemetry providers
		var hasActivity = methods.Any(m => m.Configuration.TargetTypes.HasFlag(GenerationType.Activities));
		var hasLogging = methods.Any(m => m.Configuration.TargetTypes.HasFlag(GenerationType.Logging));
		var hasMetrics = methods.Any(m => m.Configuration.TargetTypes.HasFlag(GenerationType.Metrics));

		if (hasActivity)
		{
			builder.AppendLine($"{new string('\t', indent)}private readonly {Constants.Activities.SystemDiagnostics.ActivitySource} _activitySource;");
		}

		if (hasLogging)
		{
			builder.AppendLine($"{new string('\t', indent)}private readonly {Constants.Logging.MicrosoftExtensions.ILogger} _logger;");
		}

		if (hasMetrics)
		{
			builder.AppendLine($"{new string('\t', indent)}private readonly {Constants.Metrics.SystemDiagnostics.Meter} _meter;");
		}

		builder.AppendLine();

		// Generate constructor
		GenerateConstructor(builder, indent, implementationClassName, hasActivity, hasLogging, hasMetrics);

		// Generate methods
		foreach (var method in methods)
		{
			GenerateMultiTargetMethod(builder, indent, method, context, logger);
		}

		// Close class
		builder.AppendLine("}");

		// Write source
		var hintName = $"{namespaceName}.{implementationClassName}.MultiTarget.g.cs";
		context.AddSource(
			hintName,
			Microsoft.CodeAnalysis.Text.SourceText.From(builder.ToString(), Encoding.UTF8)
		);
	}

	static void GenerateConstructor(
		StringBuilder builder,
		int indent,
		string className,
		bool hasActivity,
		bool hasLogging,
		bool hasMetrics
	)
	{
		builder.Append($"{new string('\t', indent)}public {className}(");

		var parameters = new List<string>();
		if (hasActivity)
			parameters.Add($"{Constants.Activities.SystemDiagnostics.ActivitySource} activitySource");
		if (hasLogging)
			parameters.Add($"{Constants.Logging.MicrosoftExtensions.ILogger} logger");
		if (hasMetrics)
			parameters.Add($"{Constants.Metrics.SystemDiagnostics.Meter} meter");

		builder.AppendLine(string.Join(", ", parameters) + ")");
		builder.AppendLine($"{new string('\t', indent)}{{");

		indent++;
		if (hasActivity)
			builder.AppendLine($"{new string('\t', indent)}_activitySource = activitySource;");
		if (hasLogging)
			builder.AppendLine($"{new string('\t', indent)}_logger = logger;");
		if (hasMetrics)
			builder.AppendLine($"{new string('\t', indent)}_meter = meter;");

		indent--;
		builder.AppendLine($"{new string('\t', indent)}}}").AppendLine();
	}

	static void GenerateMultiTargetMethod(
		StringBuilder builder,
		int indent,
		MultiTargetMethod method,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		logger?.Debug($"Generating multi-target method: {method.MethodName}");

		// Generate method signature
		var returnType = method.MethodSymbol.ReturnType.ToDisplayString();
		var parameters = string.Join(", ", method.Parameters.Select(p => 
			$"{p.TypeName} {p.Name}"));

		builder.AppendLine($"{new string('\t', indent)}public {returnType} {method.MethodName}({parameters})");
		builder.AppendLine($"{new string('\t', indent)}{{");

		indent++;

		// Call target methods for each enabled telemetry type
		if (method.Configuration.TargetTypes.HasFlag(GenerationType.Activities))
		{
			GenerateActivityMethodCall(builder, indent, method);
		}

		if (method.Configuration.TargetTypes.HasFlag(GenerationType.Logging))
		{
			GenerateLoggingMethodCall(builder, indent, method);
		}

		if (method.Configuration.TargetTypes.HasFlag(GenerationType.Metrics))
		{
			GenerateMetricsMethodCall(builder, indent, method);
		}

		// Return default if needed
		if (returnType != "void")
		{
			builder.AppendLine($"{new string('\t', indent)}return default;");
		}

		indent--;
		builder.AppendLine($"{new string('\t', indent)}}}").AppendLine();

		// Generate private target methods
		if (method.Configuration.TargetTypes.HasFlag(GenerationType.Activities))
		{
			GenerateActivityMethod(builder, indent, method);
		}

		if (method.Configuration.TargetTypes.HasFlag(GenerationType.Logging))
		{
			GenerateLoggingMethod(builder, indent, method);
		}

		if (method.Configuration.TargetTypes.HasFlag(GenerationType.Metrics))
		{
			GenerateMetricsMethod(builder, indent, method);
		}
	}

	static void GenerateActivityMethodCall(StringBuilder builder, int indent, MultiTargetMethod method)
	{
		var activityParams = method.Parameters
			.Where(p => !p.Exclusions.HasFlag(ParameterExclusions.Activities))
			.Select(p => p.Name);

		builder.AppendLine($"{new string('\t', indent)}{method.MethodName}_Activity({string.Join(", ", activityParams)});");
	}

	static void GenerateLoggingMethodCall(StringBuilder builder, int indent, MultiTargetMethod method)
	{
		var loggingParams = method.Parameters
			.Where(p => !p.Exclusions.HasFlag(ParameterExclusions.Logging))
			.Select(p => p.Name);

		builder.AppendLine($"{new string('\t', indent)}{method.MethodName}_Logging({string.Join(", ", loggingParams)});");
	}

	static void GenerateMetricsMethodCall(StringBuilder builder, int indent, MultiTargetMethod method)
	{
		var metricsParams = method.Parameters
			.Where(p => !p.Exclusions.HasFlag(ParameterExclusions.Metrics))
			.Select(p => p.Name);

		builder.AppendLine($"{new string('\t', indent)}{method.MethodName}_Metrics({string.Join(", ", metricsParams)});");
	}

	static void GenerateActivityMethod(StringBuilder builder, int indent, MultiTargetMethod method)
	{
		var activityParams = method.Parameters
			.Where(p => !p.Exclusions.HasFlag(ParameterExclusions.Activities))
			.Select(p => $"{p.TypeName} {p.Name}");

		builder.AppendLine($"{new string('\t', indent)}private void {method.MethodName}_Activity({string.Join(", ", activityParams)})");
		builder.AppendLine($"{new string('\t', indent)}{{");
		builder.AppendLine($"{new string('\t', indent + 1)}// Activity telemetry generation");
		builder.AppendLine($"{new string('\t', indent + 1)}// TODO: Implement activity generation logic");
		builder.AppendLine($"{new string('\t', indent)}}}").AppendLine();
	}

	static void GenerateLoggingMethod(StringBuilder builder, int indent, MultiTargetMethod method)
	{
		var loggingParams = method.Parameters
			.Where(p => !p.Exclusions.HasFlag(ParameterExclusions.Logging))
			.Select(p => $"{p.TypeName} {p.Name}");

		builder.AppendLine($"{new string('\t', indent)}private void {method.MethodName}_Logging({string.Join(", ", loggingParams)})");
		builder.AppendLine($"{new string('\t', indent)}{{");
		builder.AppendLine($"{new string('\t', indent + 1)}// Logging telemetry generation");
		builder.AppendLine($"{new string('\t', indent + 1)}// TODO: Implement logging generation logic");
		builder.AppendLine($"{new string('\t', indent)}}}").AppendLine();
	}

	static void GenerateMetricsMethod(StringBuilder builder, int indent, MultiTargetMethod method)
	{
		var metricsParams = method.Parameters
			.Where(p => !p.Exclusions.HasFlag(ParameterExclusions.Metrics))
			.Select(p => $"{p.TypeName} {p.Name}");

		builder.AppendLine($"{new string('\t', indent)}private void {method.MethodName}_Metrics({string.Join(", ", metricsParams)})");
		builder.AppendLine($"{new string('\t', indent)}{{");
		builder.AppendLine($"{new string('\t', indent + 1)}// Metrics telemetry generation");
		builder.AppendLine($"{new string('\t', indent + 1)}// TODO: Implement metrics generation logic");
		builder.AppendLine($"{new string('\t', indent)}}}").AppendLine();
	}
}
