using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class SharedHelpers
{
	public static ActivitySourceGenerationAttributeRecord? GetActivitySourceGenerationAttribute(
		Compilation compilation,
		CancellationToken token
	) => GetActivitySourceGenerationAttribute(compilation.Assembly, token);

	public static ActivitySourceAttributeRecord? GetActivitySourceAttribute(ISymbol symbol, CancellationToken token)
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
			? new(
				Name: NullIfWhitespace(data.Name),
				DefaultToTags: data.DefaultToTags,
				BaggageAndTagPrefix: NullIfWhitespace(data.BaggageAndTagPrefix),
				IncludeActivitySourcePrefix: data.IncludeActivitySourcePrefix,
				LowercaseBaggageAndTagKeys: data.LowercaseBaggageAndTagKeys
			)
			: null;
	}

	public static ActivitySourceGenerationAttributeRecord? GetActivitySourceGenerationAttribute(
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
			? new(
				Name: NullIfWhitespace(data.Name),
				DefaultToTags: data.DefaultToTags,
				BaggageAndTagPrefix: NullIfWhitespace(data.BaggageAndTagPrefix),
				BaggageAndTagSeparator: NullIfWhitespace(data.BaggageAndTagSeparator),
				LowercaseBaggageAndTagKeys: data.LowercaseBaggageAndTagKeys,
				GenerateDiagnosticsForMissingActivity: data.GenerateDiagnosticsForMissingActivity
			)
			: null;
	}

	public static ActivityAttributeRecord? GetActivityGenAttribute(ISymbol symbol, CancellationToken token)
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
		return data.Exists
			? new(Name: NullIfWhitespace(data.Name), Kind: data.Kind, CreateOnly: data.CreateOnly)
			: null;
	}

	public static EventAttributeRecord? GetActivityEventAttribute(ISymbol symbol, CancellationToken token)
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
			? new(
				Name: NullIfWhitespace(data.Name),
				UseRecordExceptionRules: data.UseRecordExceptionRules,
				RecordExceptionEscape: data.RecordExceptionEscape,
				StatusCode: data.StatusCode,
				StatusDescription: NullIfWhitespace(data.StatusDescription)
			)
			: null;
	}

	public static bool IsActivityMethod(IMethodSymbol method, CancellationToken token)
	{
		return Utilities.ContainsAttribute(method, TemplateLibrary.Activities.ActivityAttribute, token)
			|| Utilities.ContainsAttribute(method, TemplateLibrary.Activities.EventAttribute, token)
			|| Utilities.ContainsAttribute(method, TemplateLibrary.Activities.ContextAttribute, token);
	}
}
