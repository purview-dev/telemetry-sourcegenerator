using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

static partial class SharedHelpers
{
	public static GenerationType GetGenerationTypes(ISymbol symbol, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		var generationType = GenerationType.None;

		if (
			Utilities.ContainsAttribute(symbol, Constants.Activities.ActivitySourceAttribute, token)
		)
			generationType |= GenerationType.Activities;

		if (Utilities.ContainsAttribute(symbol, Constants.Logging.LoggerAttribute, token))
			generationType |= GenerationType.Logging;

		if (Utilities.ContainsAttribute(symbol, Constants.Metrics.MeterAttribute, token))
			generationType |= GenerationType.Metrics;

		return generationType;
	}

	public static bool ShouldEmit(GenerationType requestingType, GenerationType generationType)
	{
		if (requestingType == generationType)
			return true;

		var hasActivities = generationType.HasFlag(GenerationType.Activities);
		var hasLogging = generationType.HasFlag(GenerationType.Logging);
		var hasMetrics = generationType.HasFlag(GenerationType.Metrics);

		if (hasMetrics)
			return requestingType == GenerationType.Metrics;

		if (hasLogging && (!hasActivities || !hasMetrics))
			return requestingType == GenerationType.Logging;

		if (hasActivities && (!hasLogging || !hasMetrics))
			return requestingType == GenerationType.Activities;

		// Should really get here unless the generation type is None.
		return false;
	}

	public static bool AttributeParser(
		AttributeData attributeData,
		Action<string, object> namedArguments,
		SemanticModel? semanticModel,
		GenerationLogger? logger,
		CancellationToken cancellationToken
	)
	{
		logger?.Debug($"Found attribute: {attributeData}");

		if (
			semanticModel != null
			&& HasErrors(attributeData, semanticModel, logger, cancellationToken)
		)
		{
			logger?.Warning($"Attribute has error: {attributeData}");
			return false;
		}

		var constructorMethod = attributeData.AttributeConstructor;
		if (constructorMethod == null)
		{
			logger?.Warning("Could not locate the attribute's constructor.");
			return false;
		}

		if (attributeData.ConstructorArguments.Any(t => t.Kind == TypedConstantKind.Error))
		{
			logger?.Warning("Constructor arguments have an error.");
			return false;
		}

		if (attributeData.NamedArguments.Any(t => t.Value.Kind == TypedConstantKind.Error))
		{
			logger?.Warning("Named arguments have an error.");
			return false;
		}

		// supports: [AttributeType(10)]
		// supports: [AttributeType(namedParam: 10)]
		var items = attributeData.ConstructorArguments;
		if (items.Length > 0)
		{
			for (var i = 0; i < items.Length; i++)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (items[i].IsNull)
					continue;

				var name = constructorMethod.Parameters[i].Name;
				var value = Utilities.GetTypedConstantValue(items[i])!;
				if (constructorMethod.Parameters[i].Type.SpecialType == SpecialType.System_String)
				{
					var v = (string)value;
					if (string.IsNullOrWhiteSpace(v))
						continue;
				}
				namedArguments(name, value);
			}
		}

		// supports: e.g. [AttributeType(PropertyName = 10)]
		if (attributeData.NamedArguments.Any())
		{
			foreach (var namedArgument in attributeData.NamedArguments)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var value = Utilities.GetTypedConstantValue(namedArgument.Value)!;
				if (namedArgument.Value.Type == null)
				{
					logger?.Error(
						$"Named argument {namedArgument.Key}'s type could not be determined."
					);
					continue;
				}

				if (namedArgument.Value.Type.SpecialType == SpecialType.System_String)
				{
					var v = (string)value;
					if (string.IsNullOrWhiteSpace(v))
						continue;
				}

				namedArguments(namedArgument.Key, value!);
			}
		}

		return true;
	}

	public static bool AttributeParser(
		AttributeSyntax attributeSyntax,
		Action<string, string> namedArguments,
		Action<string, OutputType>? logger,
		CancellationToken cancellationToken
	)
	{
		logger?.Invoke($"Found attribute (syntax): {attributeSyntax}", OutputType.Debug);

		var arguments = attributeSyntax.ArgumentList?.Arguments;
		if (arguments != null)
		{
			foreach (var argument in arguments)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var name =
					argument.NameEquals?.Name.ToString()
					?? argument
						.DescendantNodes()
						.OfType<IdentifierNameSyntax>()
						.FirstOrDefault()
						?.ToString();

				var value = argument.Expression.ToString();
				if (name == null || value == null)
					continue;

				namedArguments(name!, value);
			}
		}

		return true;
	}

	static bool HasErrors(
		AttributeData attributeData,
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken cancellationToken
	)
	{
		if (
			attributeData.ApplicationSyntaxReference?.GetSyntax(cancellationToken)
			is not AttributeSyntax attributeSyntax
		)
		{
			return false;
		}

		var hasDiagnostic = attributeSyntax.ContainsDiagnostics;

		try
		{
			var diagnostics = semanticModel.GetDiagnostics(attributeSyntax.Span, cancellationToken);
			if (diagnostics.Length > 0 && logger != null)
			{
				var d = diagnostics.Select(m => m.GetMessage(CultureInfo.InvariantCulture));
				logger.Debug("Attribute has diagnostics: \n" + string.Join("\n - ", d));
			}

			return diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
		}
		catch (ArgumentException ex)
		{
			// For partial interfaces or other cases where the span might be invalid,
			// fall back to getting diagnostics without a span
			logger?.Debug(
				"Span invalid for GetDiagnostics, falling back to full syntax tree diagnostics"
			);

			var diagnostics = semanticModel.GetDiagnostics(cancellationToken: cancellationToken);
			if (diagnostics.Length > 0 && logger != null)
			{
				var d = diagnostics.Select(m => m.GetMessage(CultureInfo.InvariantCulture));
				logger.Debug("Attribute has diagnostics (fallback): \n" + string.Join("\n - ", d));
			}

			return diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
		}
	}

	public static TagOrBaggageAttributeRecord? GetTagOrBaggageAttribute(
		AttributeData attributeData,
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		AttributeStringValue? nameValue = null;
		AttributeValue<bool>? skipOnNullOrEmpty = null;

		if (
			!AttributeParser(
				attributeData,
				(name, value) =>
				{
					if (name.Equals("Name", StringComparison.OrdinalIgnoreCase))
						nameValue = new((string)value);
					else if (name.Equals("SkipOnNullOrEmpty", StringComparison.OrdinalIgnoreCase))
						skipOnNullOrEmpty = new((bool)value);
				},
				semanticModel,
				logger,
				token
			)
		)
		{
			// Failed to parse correctly, so null it out.
			return null;
		}

		return new(Name: nameValue ?? new(), SkipOnNullOrEmpty: skipOnNullOrEmpty ?? new(false));
	}

	public static TelemetryGenerationAttributeRecord GetTelemetryGenerationAttribute(
		ISymbol type,
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		AttributeData? assemblyAttribute = null;
		if (
			!Utilities.TryContainsAttribute(
				type,
				Constants.Shared.TelemetryGenerationAttribute,
				token,
				out var typeAttribute
			)
		)
		{
			if (
				!Utilities.TryContainsAttribute(
					semanticModel.Compilation.Assembly,
					Constants.Shared.TelemetryGenerationAttribute,
					token,
					out assemblyAttribute
				)
			)
				return CreateDefault();
		}

		var assemblyTelemetryGeneration =
			assemblyAttribute == null
				? null
				: GetTelemetryGenerationAttribute(assemblyAttribute, semanticModel, logger, token);
		var typeGeneration =
			typeAttribute == null
				? null
				: GetTelemetryGenerationAttribute(typeAttribute, semanticModel, logger, token);

		return assemblyAttribute == null && typeGeneration == null
			? CreateDefault()
			: new(
				GenerateDependencyExtension: typeGeneration?.GenerateDependencyExtension
					?? assemblyTelemetryGeneration?.GenerateDependencyExtension
					?? new(true),
				ClassName: typeGeneration?.ClassName
					?? assemblyTelemetryGeneration?.ClassName
					?? new(),
				DependencyInjectionClassName: typeGeneration?.DependencyInjectionClassName
					?? assemblyTelemetryGeneration?.DependencyInjectionClassName
					?? new(),
				DependencyInjectionClassIsPublic: typeGeneration?.DependencyInjectionClassIsPublic
					?? assemblyTelemetryGeneration?.DependencyInjectionClassIsPublic
					?? new(false)
			);

		static TelemetryGenerationAttributeRecord CreateDefault() =>
			new(
				GenerateDependencyExtension: new(true),
				ClassName: new(),
				DependencyInjectionClassName: new(),
				DependencyInjectionClassIsPublic: new(false)
			);
	}

	static TelemetryGenerationAttributeRecord? GetTelemetryGenerationAttribute(
		AttributeData attributeData,
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		AttributeValue<bool>? generateDependencyExtension = null;
		AttributeStringValue? className = null;
		AttributeStringValue? dependencyInjectionClassName = null;
		AttributeValue<bool>? dependencyInjectionClassIsPublic = null;

		return AttributeParser(
			attributeData,
			(name, value) =>
			{
				if (
					name.Equals(
						nameof(TelemetryGenerationAttributeRecord.GenerateDependencyExtension),
						StringComparison.OrdinalIgnoreCase
					)
				)
					generateDependencyExtension = new((bool)value);
				else if (
					name.Equals(
						nameof(TelemetryGenerationAttributeRecord.ClassName),
						StringComparison.OrdinalIgnoreCase
					)
				)
					className = new((string)value);
				else if (
					name.Equals(
						nameof(TelemetryGenerationAttributeRecord.DependencyInjectionClassName),
						StringComparison.OrdinalIgnoreCase
					)
				)
					dependencyInjectionClassName = new((string)value);
				else if (
					name.Equals(
						nameof(TelemetryGenerationAttributeRecord.DependencyInjectionClassIsPublic),
						StringComparison.OrdinalIgnoreCase
					)
				)
					dependencyInjectionClassIsPublic = new((bool)value);
			},
			semanticModel,
			logger,
			token
		)
			? new(
				GenerateDependencyExtension: generateDependencyExtension ?? new(true),
				ClassName: className ?? new(),
				DependencyInjectionClassName: dependencyInjectionClassName ?? new(),
				DependencyInjectionClassIsPublic: dependencyInjectionClassIsPublic ?? new(false)
			)
			: null;
	}
}
