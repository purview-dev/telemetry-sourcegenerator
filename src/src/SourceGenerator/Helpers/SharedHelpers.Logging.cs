using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class SharedHelpers
{
	public static LogAttributeRecord? GetLogAttribute(
		ISymbol symbol,
		SemanticModel? semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				[TemplateLibrary.Logging.LogAttribute, .. TemplateLibrary.Logging.SpecificLogAttributes],
				token,
				out var attributeData,
				out var matchingTemplate
			)
		)
		{
			return null;
		}

		AttributeValue<int>? level = null;
		if (matchingTemplate != TemplateLibrary.Logging.LogAttribute)
			level = new(TemplateLibrary.Logging.SpecificLogAttributesToLevel[matchingTemplate!]);

		var parsedLevel = GetAttributeValue<int>(attributeData!, "level");
		if (parsedLevel.IsSet)
			level = parsedLevel;

		return new(
			Level: level ?? new(),
			MessageTemplate: GetAttributeStringValue(attributeData!, "messageTemplate"),
			EventId: GetAttributeValue<int>(attributeData!, "eventId"),
			Name: GetAttributeStringValue(attributeData!, "name"),
			GenerationMode: GetAttributeValue<int>(attributeData!, "generationMode")
		);
	}

	public static LoggerAttributeRecord? GetLoggerAttribute(
		ISymbol symbol,
		SemanticModel? semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				TemplateLibrary.Logging.LoggerAttribute,
				token,
				out var attributeData
			)
		)
		{
			return null;
		}

		return new(
			DefaultLevel: GetAttributeValue<int>(attributeData!, "defaultLevel"),
			CustomPrefix: GetAttributeStringValue(attributeData!, "customPrefix"),
			PrefixType: GetAttributeValue<int>(attributeData!, "prefixType"),
			GenerationMode: GetAttributeValue<int>(attributeData!, "generationMode")
		);
	}

	public static LoggerGenerationAttributeRecord? GetLoggerGenerationAttribute(
		ISymbol symbol,
		SemanticModel? semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				TemplateLibrary.Logging.LoggerGenerationAttribute,
				token,
				out var attributeData
			)
		)
		{
			return null;
		}

		return new(
			DefaultLevel: GetAttributeValue<int>(attributeData!, "defaultLevel"),
			GenerationMode: GetAttributeValue<int>(attributeData!, "generationMode"),
			DefaultPrefixType: GetAttributeValue<int>(attributeData!, "defaultPrefixType")
		);
	}

	public static LogPropertiesAttributeRecord? GetLogPropertiesAttribute(
		ISymbol symbol,
		SemanticModel? semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				TypeLibrary.Logging.MicrosoftExtensions.LogPropertiesAttribute,
				token,
				out var attributeData
			)
		)
		{
			return null;
		}

		return new(
			OmitReferenceName: GetAttributeValue<bool>(attributeData!, "omitReferenceName"),
			SkipNullProperties: GetAttributeValue<bool>(attributeData!, "skipNullProperties"),
			Transitive: GetAttributeValue<bool>(attributeData!, "transitive")
		);
	}

	public static ExpandEnumerableAttributeRecord? GetExpandEnumerableAttribute(
		ISymbol symbol,
		SemanticModel? semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token
	)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				TemplateLibrary.Logging.ExpandEnumerableAttribute,
				token,
				out var attributeData
			)
		)
		{
			return null;
		}

		return new(MaximumValueCount: GetAttributeValue<int>(attributeData!, "maximumValueCount", 5));
	}

	public static LoggerGenerationAttributeRecord? GetLoggerGenerationAttribute(
		SemanticModel semanticModel,
		ISourceGenLogger? logger,
		CancellationToken token
	) => GetLoggerGenerationAttribute(semanticModel.Compilation.Assembly, semanticModel, logger, token);

	public static bool IsLogMethod(IMethodSymbol method, CancellationToken token) =>
		Utilities.ContainsAttribute(
			method,
			[TemplateLibrary.Logging.LogAttribute, .. TemplateLibrary.Logging.SpecificLogAttributes],
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
		var result = 2_166_136_261u;
		foreach (var c in methodName)
			result = (c ^ result) * multiplier;

		var ret = (int)result;
		return ret == int.MinValue ? 0 : Math.Abs(ret); // Ensure the result is non-negative
	}
}
