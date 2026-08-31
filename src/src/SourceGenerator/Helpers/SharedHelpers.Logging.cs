using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class SharedHelpers
{
	public static LogAttributeRecord? GetLogAttribute(
		ISymbol symbol,
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				[Constants.Logging.LogAttribute, .. Constants.Logging.SpecificLogAttributes],
				token,
				out var attributeData,
				out var matchingTemplate
			)
		)
		{
			return null;
		}

		AttributeValue<int>? level = null;
		if (matchingTemplate != Constants.Logging.LogAttribute)
			level = new(Constants.Logging.SpecificLogAttributesToLevel[matchingTemplate!]);

		AttributeStringValue? messageTemplate = null;
		AttributeValue<int>? eventId = null;
		AttributeStringValue? nameValue = null;
		AttributeValue<int>? generationMode = null;

		if (
			!AttributeParser(
				attributeData!,
				(name, value) =>
				{
					if (name.Equals(nameof(LogAttributeRecord.Level), StringComparison.OrdinalIgnoreCase))
					{
						level = new((int)value);
					}
					else if (
						name.Equals(nameof(LogAttributeRecord.MessageTemplate), StringComparison.OrdinalIgnoreCase)
					)
					{
						messageTemplate = new((string)value);
					}
					else if (name.Equals(nameof(LogAttributeRecord.EventId), StringComparison.OrdinalIgnoreCase))
					{
						eventId = new((int)value);
					}
					else if (name.Equals(nameof(LogAttributeRecord.Name), StringComparison.OrdinalIgnoreCase))
					{
						nameValue = new((string)value);
					}
					else if (name.Equals(nameof(LogAttributeRecord.GenerationMode), StringComparison.OrdinalIgnoreCase))
					{
						generationMode = new((int)value);
					}
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
			Level: level ?? new(),
			MessageTemplate: messageTemplate ?? new(),
			EventId: eventId ?? new(),
			Name: nameValue ?? new(),
			GenerationMode: generationMode ?? new()
		);
	}

	public static LoggerAttributeRecord? GetLoggerAttribute(
		ISymbol symbol,
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		if (!Utilities.TryContainsAttribute(symbol, Constants.Logging.LoggerAttribute, token, out var attributeData))
		{
			return null;
		}

		AttributeValue<int>? defaultLevel = null;
		AttributeStringValue? customPrefix = null;
		AttributeValue<int>? prefixType = null;
		AttributeValue<int>? generationMode = null;

		if (
			!AttributeParser(
				attributeData!,
				(name, value) =>
				{
					if (name.Equals(nameof(LoggerAttributeRecord.DefaultLevel), StringComparison.OrdinalIgnoreCase))
					{
						defaultLevel = new((int)value);
					}
					else if (
						name.Equals(nameof(LoggerAttributeRecord.CustomPrefix), StringComparison.OrdinalIgnoreCase)
					)
					{
						customPrefix = new((string)value);
					}
					else if (name.Equals(nameof(LoggerAttributeRecord.PrefixType), StringComparison.OrdinalIgnoreCase))
					{
						prefixType = new((int)value);
					}
					else if (
						name.Equals(nameof(LoggerAttributeRecord.GenerationMode), StringComparison.OrdinalIgnoreCase)
					)
					{
						generationMode = new((int)value);
					}
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
			DefaultLevel: defaultLevel ?? new(),
			CustomPrefix: customPrefix ?? new(),
			PrefixType: prefixType ?? new(),
			GenerationMode: generationMode ?? new()
		);
	}

	public static LoggerGenerationAttributeRecord? GetLoggerGenerationAttribute(
		ISymbol symbol,
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				Constants.Logging.LoggerGenerationAttribute,
				token,
				out var attributeData
			)
		)
		{
			return null;
		}

		AttributeValue<int>? defaultLevel = null;
		AttributeValue<int>? generationMode = null;
		AttributeValue<int>? prefixType = null;

		if (
			!AttributeParser(
				attributeData!,
				(name, value) =>
				{
					if (
						name.Equals(
							nameof(LoggerGenerationAttributeRecord.DefaultLevel),
							StringComparison.OrdinalIgnoreCase
						)
					)
					{
						defaultLevel = new((int)value);
					}
					else if (
						name.Equals(
							nameof(LoggerGenerationAttributeRecord.GenerationMode),
							StringComparison.OrdinalIgnoreCase
						)
					)
					{
						generationMode = new((int)value);
					}
					else if (
						name.Equals(
							nameof(LoggerGenerationAttributeRecord.DefaultPrefixType),
							StringComparison.OrdinalIgnoreCase
						)
					)
					{
						prefixType = new((int)value);
					}
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
			DefaultLevel: defaultLevel ?? new(),
			GenerationMode: generationMode ?? new(),
			DefaultPrefixType: prefixType ?? new()
		);
	}

	public static LogPropertiesAttributeRecord? GetLogPropertiesAttribute(
		ISymbol symbol,
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				Constants.Logging.MicrosoftExtensions.LogPropertiesAttribute,
				token,
				out var attributeData
			)
		)
		{
			return null;
		}

		AttributeValue<bool>? omitReferenceName = null;
		AttributeValue<bool>? skipNullProperties = null;
		AttributeValue<bool>? transitive = null;

		if (
			!AttributeParser(
				attributeData!,
				(name, value) =>
				{
					if (
						name.Equals(
							nameof(LogPropertiesAttributeRecord.OmitReferenceName),
							StringComparison.OrdinalIgnoreCase
						)
					)
					{
						omitReferenceName = new((bool)value);
					}
					else if (
						name.Equals(
							nameof(LogPropertiesAttributeRecord.SkipNullProperties),
							StringComparison.OrdinalIgnoreCase
						)
					)
					{
						skipNullProperties = new((bool)value);
					}
					else if (
						name.Equals(nameof(LogPropertiesAttributeRecord.Transitive), StringComparison.OrdinalIgnoreCase)
					)
					{
						transitive = new((bool)value);
					}
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
			OmitReferenceName: omitReferenceName ?? new(),
			SkipNullProperties: skipNullProperties ?? new(),
			Transitive: transitive ?? new()
		);
	}

	public static ExpandEnumerableAttributeRecord? GetExpandEnumerableAttribute(
		ISymbol symbol,
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				Constants.Logging.ExpandEnumerableAttribute,
				token,
				out var attributeData
			)
		)
		{
			return null;
		}

		AttributeValue<int>? maximumValueCount = null;

		if (
			!AttributeParser(
				attributeData!,
				(name, value) =>
				{
					if (
						name.Equals(
							nameof(ExpandEnumerableAttributeRecord.MaximumValueCount),
							StringComparison.OrdinalIgnoreCase
						)
					)
					{
						maximumValueCount = new((int)value);
					}
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

		return new(MaximumValueCount: maximumValueCount ?? new(5), ParamLocation: symbol.Locations.FirstOrDefault());
	}

	public static LoggerGenerationAttributeRecord? GetLoggerGenerationAttribute(
		SemanticModel semanticModel,
		GenerationLogger? logger,
		CancellationToken token
	) => GetLoggerGenerationAttribute(semanticModel.Compilation.Assembly, semanticModel, logger, token);

	public static bool IsLogMethod(IMethodSymbol method, CancellationToken token) =>
		Utilities.ContainsAttribute(
			method,
			[Constants.Logging.LogAttribute, .. Constants.Logging.SpecificLogAttributes],
			token
		);

	/// <summary>
	/// Returns a non-randomized hash code for the given string.
	/// </summary>
	/// <remarks>
	/// We always return a positive value.
	/// This code is cloned from the logging generator in dotnet/runtime in
	/// order to retain the same event ids when upgrading to this generator.
	/// </remarks>
	public static int GetNonRandomizedHashCode(string methodName)
	{
		const int multiplier = 16_777_619;
		uint result = 2_166_136_261u;
		foreach (char c in methodName)
			result = (c ^ result) * multiplier;

		var ret = (int)result;
		return ret == int.MinValue ? 0 : Math.Abs(ret); // Ensure the result is non-negative
	}
}
