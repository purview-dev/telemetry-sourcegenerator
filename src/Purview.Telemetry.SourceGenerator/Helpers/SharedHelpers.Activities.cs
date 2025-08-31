using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class SharedHelpers
{
	public static ActivitySourceGenerationAttributeRecord? GetActivitySourceGenerationAttribute(
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken token
	) =>
		GetActivitySourceGenerationAttribute(
			semanticModel.Compilation.Assembly,
			semanticModel,
			logger,
			token
		);

	public static ActivitySourceAttributeRecord? GetActivitySourceAttribute(
		ISymbol symbol,
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				Constants.Activities.ActivitySourceAttribute,
				token,
				out var attributeData
			)
		)
		{
			return null;
		}

		AttributeStringValue? nameValue = null;
		AttributeValue<bool>? defaultToTags = null;
		AttributeStringValue? baggageAndTagPrefix = null;
		AttributeValue<bool>? includeActivitySourcePrefix = null;
		AttributeValue<bool>? lowercaseBaggageAndTagKeys = null;

		if (
			!AttributeParser(
				attributeData!,
				(name, value) =>
				{
					if (
						name.Equals(
							nameof(ActivitySourceAttributeRecord.Name),
							StringComparison.OrdinalIgnoreCase
						)
					)
						nameValue = new((string)value);
					else if (
						name.Equals(
							nameof(ActivitySourceAttributeRecord.DefaultToTags),
							StringComparison.OrdinalIgnoreCase
						)
					)
						defaultToTags = new((bool)value);
					else if (
						name.Equals(
							nameof(ActivitySourceAttributeRecord.BaggageAndTagPrefix),
							StringComparison.OrdinalIgnoreCase
						)
					)
						baggageAndTagPrefix = new((string)value);
					else if (
						name.Equals(
							nameof(ActivitySourceAttributeRecord.IncludeActivitySourcePrefix),
							StringComparison.OrdinalIgnoreCase
						)
					)
						includeActivitySourcePrefix = new((bool)value);
					else if (
						name.Equals(
							nameof(ActivitySourceAttributeRecord.LowercaseBaggageAndTagKeys),
							StringComparison.OrdinalIgnoreCase
						)
					)
						lowercaseBaggageAndTagKeys = new((bool)value);
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

		return new(
			Name: nameValue ?? new(),
			DefaultToTags: defaultToTags ?? new(true),
			BaggageAndTagPrefix: baggageAndTagPrefix ?? new(),
			IncludeActivitySourcePrefix: includeActivitySourcePrefix ?? new(true),
			LowercaseBaggageAndTagKeys: lowercaseBaggageAndTagKeys ?? new(true)
		);
	}

	public static ActivitySourceGenerationAttributeRecord? GetActivitySourceGenerationAttribute(
		ISymbol symbol,
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				Constants.Activities.ActivitySourceGenerationAttribute,
				token,
				out var attributeData
			)
		)
		{
			return null;
		}

		AttributeStringValue? nameValue = null;
		AttributeValue<bool>? defaultToTags = null;
		AttributeStringValue? baggageAndTagPrefix = null;
		AttributeStringValue? baggageAndTagSeparator = null;
		AttributeValue<bool>? lowercaseBaggageAndTagKeys = null;
		AttributeValue<bool>? generateDiagnosticsForMissingActivity = null;

		if (
			!AttributeParser(
				attributeData!,
				(name, value) =>
				{
					if (
						name.Equals(
							nameof(ActivitySourceGenerationAttributeRecord.Name),
							StringComparison.OrdinalIgnoreCase
						)
					)
						nameValue = new((string)value);
					else if (
						name.Equals(
							nameof(ActivitySourceGenerationAttributeRecord.DefaultToTags),
							StringComparison.OrdinalIgnoreCase
						)
					)
						defaultToTags = new((bool)value);
					else if (
						name.Equals(
							nameof(ActivitySourceGenerationAttributeRecord.BaggageAndTagPrefix),
							StringComparison.OrdinalIgnoreCase
						)
					)
						baggageAndTagPrefix = new((string)value);
					else if (
						name.Equals(
							nameof(ActivitySourceGenerationAttributeRecord.BaggageAndTagSeparator),
							StringComparison.OrdinalIgnoreCase
						)
					)
						baggageAndTagSeparator = new((string)value);
					else if (
						name.Equals(
							nameof(
								ActivitySourceGenerationAttributeRecord.LowercaseBaggageAndTagKeys
							),
							StringComparison.OrdinalIgnoreCase
						)
					)
						lowercaseBaggageAndTagKeys = new((bool)value);
					else if (
						name.Equals(
							nameof(
								ActivitySourceGenerationAttributeRecord.GenerateDiagnosticsForMissingActivity
							),
							StringComparison.OrdinalIgnoreCase
						)
					)
						generateDiagnosticsForMissingActivity = new((bool)value);
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

		return new(
			Name: nameValue ?? new(),
			DefaultToTags: defaultToTags ?? new(),
			BaggageAndTagPrefix: baggageAndTagPrefix ?? new(),
			BaggageAndTagSeparator: baggageAndTagSeparator ?? new("."),
			LowercaseBaggageAndTagKeys: lowercaseBaggageAndTagKeys ?? new(true),
			GenerateDiagnosticsForMissingActivity: generateDiagnosticsForMissingActivity
				?? new(true)
		);
	}

	public static ActivityAttributeRecord? GetActivityGenAttribute(
		ISymbol symbol,
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				Constants.Activities.ActivityAttribute,
				token,
				out var attributeData
			)
		)
		{
			return null;
		}

		AttributeStringValue? nameValue = null;
		AttributeValue<int>? kind = null;
		AttributeValue<bool>? createOnly = null;

		if (
			!AttributeParser(
				attributeData!,
				(name, value) =>
				{
					if (
						name.Equals(
							nameof(ActivityAttributeRecord.Name),
							StringComparison.OrdinalIgnoreCase
						)
					)
						nameValue = new((string)value);
					else if (
						name.Equals(
							nameof(ActivityAttributeRecord.Kind),
							StringComparison.OrdinalIgnoreCase
						)
					)
						kind = new((int)value);
					else if (
						name.Equals(
							nameof(ActivityAttributeRecord.CreateOnly),
							StringComparison.OrdinalIgnoreCase
						)
					)
						createOnly = new((bool)value);
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

		return new(
			Name: nameValue ?? new(),
			Kind: kind ?? new(Constants.Activities.DefaultActivityKind),
			CreateOnly: createOnly ?? new()
		);
	}

	public static EventAttributeRecord? GetActivityEventAttribute(
		ISymbol symbol,
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				Constants.Activities.EventAttribute,
				token,
				out var attributeData
			)
		)
		{
			return null;
		}

		AttributeStringValue? nameValue = null;
		AttributeValue<bool>? useRecordExceptionRules = null;
		AttributeValue<bool>? recordExceptionEscape = null;
		AttributeValue<int>? statusCode = null;
		AttributeStringValue? statusDescription = null;

		if (
			!AttributeParser(
				attributeData!,
				(name, value) =>
				{
					if (
						name.Equals(
							nameof(EventAttributeRecord.Name),
							StringComparison.OrdinalIgnoreCase
						)
					)
						nameValue = new((string)value);
					else if (
						name.Equals(
							nameof(EventAttributeRecord.UseRecordExceptionRules),
							StringComparison.OrdinalIgnoreCase
						)
					)
						useRecordExceptionRules = new((bool)value);
					else if (
						name.Equals(
							nameof(EventAttributeRecord.RecordExceptionEscape),
							StringComparison.OrdinalIgnoreCase
						)
					)
						recordExceptionEscape = new((bool)value);
					else if (
						name.Equals(
							nameof(EventAttributeRecord.StatusCode),
							StringComparison.OrdinalIgnoreCase
						)
					)
						statusCode = new((int)value);
					else if (
						name.Equals(
							nameof(EventAttributeRecord.StatusDescription),
							StringComparison.OrdinalIgnoreCase
						)
					)
						statusDescription = new((string)value);
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

		return new(
			Name: nameValue ?? new(),
			UseRecordExceptionRules: useRecordExceptionRules ?? new(),
			RecordExceptionEscape: recordExceptionEscape ?? new(),
			StatusCode: statusCode ?? new(),
			StatusDescription: statusDescription ?? new()
		);
	}

	public static bool IsActivityMethod(IMethodSymbol method, CancellationToken token)
	{
		return Utilities.ContainsAttribute(method, Constants.Activities.ActivityAttribute, token)
			|| Utilities.ContainsAttribute(method, Constants.Activities.EventAttribute, token)
			|| Utilities.ContainsAttribute(method, Constants.Activities.ContextAttribute, token);
		;
	}
}
