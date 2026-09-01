using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class SharedHelpers
{
	public static LogAttributeData? GetLogAttribute(ISymbol symbol, CancellationToken token)
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

		if (matchingTemplate == TemplateLibrary.Logging.LogAttribute)
		{
			var data = LogAttributeData.FromAttributeData(attributeData!);
			return data.Exists
				? data with
				{
					MessageTemplate = NullIfWhitespace(data.MessageTemplate),
					Name = NullIfWhitespace(data.Name),
				}
				: null;
		}

		// A specific level attribute (Trace/Debug/Info/Warning/Error/Critical) forces its level.
		(var messageTemplate, var eventId, var name, var generationMode) = GetSpecificLogData(
			matchingTemplate!,
			attributeData!
		);

		return new(
			exists: true,
			Level: TemplateLibrary.Logging.SpecificLogAttributesToLevel[matchingTemplate!],
			MessageTemplate: messageTemplate,
			EventId: eventId,
			Name: name,
			GenerationMode: generationMode
		);
	}

	static (string? MessageTemplate, int EventId, string? Name, int GenerationMode) GetSpecificLogData(
		TemplateInfo template,
		AttributeData attributeData
	)
	{
		if (template == TemplateLibrary.Logging.TraceAttribute)
		{
			var data = TraceAttributeData.FromAttributeData(attributeData);
			return (
				NullIfWhitespace(data.MessageTemplate),
				data.EventId,
				NullIfWhitespace(data.Name),
				data.GenerationMode
			);
		}

		if (template == TemplateLibrary.Logging.DebugAttribute)
		{
			var data = DebugAttributeData.FromAttributeData(attributeData);
			return (
				NullIfWhitespace(data.MessageTemplate),
				data.EventId,
				NullIfWhitespace(data.Name),
				data.GenerationMode
			);
		}

		if (template == TemplateLibrary.Logging.InfoAttribute)
		{
			var data = InfoAttributeData.FromAttributeData(attributeData);
			return (
				NullIfWhitespace(data.MessageTemplate),
				data.EventId,
				NullIfWhitespace(data.Name),
				data.GenerationMode
			);
		}

		if (template == TemplateLibrary.Logging.WarningAttribute)
		{
			var data = WarningAttributeData.FromAttributeData(attributeData);
			return (
				NullIfWhitespace(data.MessageTemplate),
				data.EventId,
				NullIfWhitespace(data.Name),
				data.GenerationMode
			);
		}

		if (template == TemplateLibrary.Logging.ErrorAttribute)
		{
			var data = ErrorAttributeData.FromAttributeData(attributeData);
			return (
				NullIfWhitespace(data.MessageTemplate),
				data.EventId,
				NullIfWhitespace(data.Name),
				data.GenerationMode
			);
		}

		var criticalData = CriticalAttributeData.FromAttributeData(attributeData);
		return (
			NullIfWhitespace(criticalData.MessageTemplate),
			criticalData.EventId,
			NullIfWhitespace(criticalData.Name),
			criticalData.GenerationMode
		);
	}

	public static LoggerAttributeData? GetLoggerAttribute(ISymbol symbol, CancellationToken token)
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

		var data = LoggerAttributeData.FromAttributeData(attributeData!);
		return data.Exists ? data with { CustomPrefix = NullIfWhitespace(data.CustomPrefix) } : null;
	}

	public static LoggerGenerationAttributeData? GetLoggerGenerationAttribute(ISymbol symbol, CancellationToken token)
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

		var data = LoggerGenerationAttributeData.FromAttributeData(attributeData!);
		return data.Exists ? data : null;
	}

	public static LogPropertiesAttributeData? GetLogPropertiesAttribute(ISymbol symbol, CancellationToken token)
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

		var data = LogPropertiesAttributeData.FromAttributeData(attributeData!);
		return data.Exists ? data : null;
	}

	public static ExpandEnumerableAttributeData? GetExpandEnumerableAttribute(ISymbol symbol, CancellationToken token)
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

		var data = ExpandEnumerableAttributeData.FromAttributeData(attributeData!);
		return data.Exists ? data : null;
	}

	public static LoggerGenerationAttributeData? GetLoggerGenerationAttribute(
		Compilation compilation,
		CancellationToken token
	) => GetLoggerGenerationAttribute(compilation.Assembly, token);

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
