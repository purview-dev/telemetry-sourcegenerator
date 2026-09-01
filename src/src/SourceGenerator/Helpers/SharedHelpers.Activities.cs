using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class SharedHelpers
{
	public static ActivitySourceGenerationAttributeRecord? GetActivitySourceGenerationAttribute(
		SemanticModel semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token
	) => GetActivitySourceGenerationAttribute(semanticModel.Compilation.Assembly, semanticModel, logger, token);

	public static ActivitySourceAttributeRecord? GetActivitySourceAttribute(
		ISymbol symbol,
		SemanticModel semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				TemplateLibrary.Activities.ActivitySourceAttribute,
				token,
				out var attributeData
			)
		)
		{
			return null;
		}

		return new(
			Name: GetAttributeStringValue(attributeData!, "name"),
			DefaultToTags: GetAttributeValue<bool>(attributeData!, "defaultToTags", true),
			BaggageAndTagPrefix: GetAttributeStringValue(attributeData!, "baggageAndTagPrefix"),
			IncludeActivitySourcePrefix: GetAttributeValue<bool>(attributeData!, "includeActivitySourcePrefix", true),
			LowercaseBaggageAndTagKeys: GetAttributeValue<bool>(attributeData!, "lowercaseBaggageAndTagKeys", true)
		);
	}

	public static ActivitySourceGenerationAttributeRecord? GetActivitySourceGenerationAttribute(
		ISymbol symbol,
		SemanticModel semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				TemplateLibrary.Activities.ActivitySourceGenerationAttribute,
				token,
				out var attributeData
			)
		)
		{
			return null;
		}

		return new(
			Name: GetAttributeStringValue(attributeData!, "name"),
			DefaultToTags: GetAttributeValue<bool>(attributeData!, "defaultToTags"),
			BaggageAndTagPrefix: GetAttributeStringValue(attributeData!, "baggageAndTagPrefix"),
			BaggageAndTagSeparator: GetAttributeStringValue(attributeData!, "baggageAndTagSeparator", "."),
			LowercaseBaggageAndTagKeys: GetAttributeValue<bool>(attributeData!, "lowercaseBaggageAndTagKeys", true),
			GenerateDiagnosticsForMissingActivity: GetAttributeValue<bool>(
				attributeData!,
				"generateDiagnosticsForMissingActivity",
				true
			)
		);
	}

	public static ActivityAttributeRecord? GetActivityGenAttribute(
		ISymbol symbol,
		SemanticModel semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				TemplateLibrary.Activities.ActivityAttribute,
				token,
				out var attributeData
			)
		)
		{
			return null;
		}

		return new(
			Name: GetAttributeStringValue(attributeData!, "name"),
			Kind: GetAttributeValue<int>(attributeData!, "kind", PropertyLibrary.Activities.DefaultActivityKind),
			CreateOnly: GetAttributeValue<bool>(attributeData!, "createOnly")
		);
	}

	public static EventAttributeRecord? GetActivityEventAttribute(
		ISymbol symbol,
		SemanticModel semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				TemplateLibrary.Activities.EventAttribute,
				token,
				out var attributeData
			)
		)
		{
			return null;
		}

		return new(
			Name: GetAttributeStringValue(attributeData!, "name"),
			UseRecordExceptionRules: GetAttributeValue<bool>(attributeData!, "useRecordExceptionRules"),
			RecordExceptionEscape: GetAttributeValue<bool>(attributeData!, "recordExceptionEscape"),
			StatusCode: GetAttributeValue<int>(attributeData!, "statusCode"),
			StatusDescription: GetAttributeStringValue(attributeData!, "statusDescription")
		);
	}

	public static bool IsActivityMethod(IMethodSymbol method, CancellationToken token)
	{
		return Utilities.ContainsAttribute(method, TemplateLibrary.Activities.ActivityAttribute, token)
			|| Utilities.ContainsAttribute(method, TemplateLibrary.Activities.EventAttribute, token)
			|| Utilities.ContainsAttribute(method, TemplateLibrary.Activities.ContextAttribute, token);
	}
}
