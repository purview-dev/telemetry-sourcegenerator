using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Helpers;

static partial class PipelineHelpers
{
	public static bool HasMultiTargetAttribute(SyntaxNode node, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		return node is MethodDeclarationSyntax;
	}

	public static MultiTargetMethod? BuildMultiTargetTransform(
		GeneratorAttributeSyntaxContext context,
		GenerationLogger? logger,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (context.TargetSymbol is not IMethodSymbol methodSymbol)
		{
			logger?.Warning("MultiTarget transform called on non-method symbol");
			return null;
		}

		if (context.TargetNode is not MethodDeclarationSyntax methodSyntax)
		{
			logger?.Warning("MultiTarget transform called on non-method syntax");
			return null;
		}

		var assembly = context.SemanticModel.Compilation.Assembly;
		var multiTargetConfig = Utilities.GetMultiTargetConfiguration(methodSymbol, assembly);

		if (!multiTargetConfig.IsMultiTargetEnabled)
		{
			logger?.Debug(
				$"Method {methodSymbol.Name} has TelemetryAttribute but multi-target is not enabled"
			);
			return null;
		}

		// Get containing type information
		var containingType = methodSymbol.ContainingType;
		var containingTypeName = containingType.Name;
		var namespaceName = containingType.ContainingNamespace?.ToDisplayString() ?? "";
		var isPartial = IsPartialType(containingType);

		if (!isPartial)
		{
			logger?.Warning($"Multi-target method {methodSymbol.Name} must be in a partial type");
			return null;
		}

		// Process parameters
		var parameters = ProcessMultiTargetParameters(
			methodSymbol.Parameters,
			logger,
			cancellationToken
		);

		var location = methodSyntax.GetLocation();

		return new MultiTargetMethod(
			MethodName: methodSymbol.Name,
			FullyQualifiedMethodName: methodSymbol.ToDisplayString(),
			MethodSymbol: methodSymbol,
			Configuration: multiTargetConfig,
			Parameters: parameters,
			ContainingTypeName: containingTypeName,
			Namespace: namespaceName,
			IsPartial: isPartial,
			Location: location
		);
	}

	static ImmutableArray<MultiTargetParameter> ProcessMultiTargetParameters(
		ImmutableArray<IParameterSymbol> parameters,
		GenerationLogger? logger,
		CancellationToken cancellationToken
	)
	{
		var result = ImmutableArray.CreateBuilder<MultiTargetParameter>();

		foreach (var parameter in parameters)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var exclusions = Utilities.GetParameterExclusions(parameter);
			var (isTag, tagName) = GetTagInfo(parameter);
			var (isBaggage, baggageName) = GetBaggageInfo(parameter);

			logger?.Debug(
				$"Found parameter '{parameter.Name}': IsTag={isTag} (TagName={tagName}), IsBaggage={isBaggage} (BaggageName={baggageName}), Exclusions={exclusions}"
			);

			if (isTag && isBaggage)
			{
				logger?.Warning(
					$"Parameter {parameter.Name} cannot be both a Tag and Baggage. It will be treated as a Tag."
				);
				isBaggage = false;
				baggageName = null;
			}

			MultiTargetParameter multiTargetParam = new(
				Name: parameter.Name,
				TypeName: parameter.Type.ToDisplayString(),
				ParameterSymbol: parameter,
				Exclusions: exclusions,
				IsTag: isTag,
				IsBaggage: isBaggage,
				TagName: tagName,
				BaggageName: baggageName
			);

			result.Add(multiTargetParam);
		}

		return result.ToImmutable();
	}

	static (bool IsTag, string? TagName) GetTagInfo(IParameterSymbol parameter)
	{
		var tagAttribute = parameter
			.GetAttributes()
			.FirstOrDefault(attr =>
				attr.AttributeClass != null
				&& PurviewTypeFactory.Create(attr.AttributeClass) == Constants.Shared.TagAttribute
			);

		if (tagAttribute == null)
			return (false, null);

		// Extract tag name from attribute
		string? tagName = null;
		if (tagAttribute.ConstructorArguments.Length > 0)
		{
			var nameValue = tagAttribute.ConstructorArguments[0].Value;
			if (nameValue is string name && !string.IsNullOrWhiteSpace(name))
				tagName = name;
		}

		// If no name specified, use parameter name
		tagName ??= parameter.Name;

		return (true, tagName);
	}

	static (bool IsBaggage, string? BaggageName) GetBaggageInfo(IParameterSymbol parameter)
	{
		// Check for Activities.BaggageAttribute
		var baggageAttribute = parameter
			.GetAttributes()
			.FirstOrDefault(attr =>
				attr.AttributeClass != null
				&& PurviewTypeFactory.Create(attr.AttributeClass)
					== Constants.Activities.BaggageAttribute
			);

		if (baggageAttribute == null)
			return (false, null);

		// Extract baggage name from attribute
		string? baggageName = null;
		if (baggageAttribute.ConstructorArguments.Length > 0)
		{
			var nameValue = baggageAttribute.ConstructorArguments[0].Value;
			if (nameValue is string name && !string.IsNullOrWhiteSpace(name))
				baggageName = name;
		}

		// If no name specified, use parameter name
		baggageName ??= parameter.Name;

		return (true, baggageName);
	}

	static bool IsPartialType(INamedTypeSymbol typeSymbol)
	{
		return typeSymbol.DeclaringSyntaxReferences.Any(syntaxRef =>
		{
			var syntax = syntaxRef.GetSyntax();
			return syntax is TypeDeclarationSyntax typeSyntax
				&& typeSyntax.Modifiers.Any(m => m.ValueText == "partial");
		});
	}
}
