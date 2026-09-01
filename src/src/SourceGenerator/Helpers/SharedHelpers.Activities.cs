using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class SharedHelpers
{
	public static ActivitySourceGenerationAttributeData? GetActivitySourceGenerationAttribute(
		Compilation compilation,
		CancellationToken token
	) => GetActivitySourceGenerationAttribute(compilation.Assembly, token);

	public static ActivitySourceAttributeData? GetActivitySourceAttribute(ISymbol symbol, CancellationToken token)
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

		var data = ActivitySourceAttributeData.FromAttributeData(attributeData!);
		return data.Exists
			? data with
			{
				Name = NullIfWhitespace(data.Name),
				BaggageAndTagPrefix = NullIfWhitespace(data.BaggageAndTagPrefix),
			}
			: null;
	}

	public static ActivitySourceGenerationAttributeData? GetActivitySourceGenerationAttribute(
		ISymbol symbol,
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

		var data = ActivitySourceGenerationAttributeData.FromAttributeData(attributeData!);
		return data.Exists
			? data with
			{
				Name = NullIfWhitespace(data.Name),
				BaggageAndTagPrefix = NullIfWhitespace(data.BaggageAndTagPrefix),
				BaggageAndTagSeparator = NullIfWhitespace(data.BaggageAndTagSeparator),
			}
			: null;
	}

	public static ActivityAttributeData? GetActivityGenAttribute(ISymbol symbol, CancellationToken token)
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

		var data = ActivityAttributeData.FromAttributeData(attributeData!);
		return data.Exists ? data with { Name = NullIfWhitespace(data.Name) } : null;
	}

	public static EventAttributeData? GetActivityEventAttribute(ISymbol symbol, CancellationToken token)
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

		var data = EventAttributeData.FromAttributeData(attributeData!);
		return data.Exists
			? data with
			{
				Name = NullIfWhitespace(data.Name),
				StatusDescription = NullIfWhitespace(data.StatusDescription),
			}
			: null;
	}

	public static bool IsActivityMethod(IMethodSymbol method, CancellationToken token)
	{
		return Utilities.ContainsAttribute(method, TemplateLibrary.Activities.ActivityAttribute, token)
			|| Utilities.ContainsAttribute(method, TemplateLibrary.Activities.EventAttribute, token)
			|| Utilities.ContainsAttribute(method, TemplateLibrary.Activities.ContextAttribute, token);
	}
}
