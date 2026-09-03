using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class SharedHelpers
{
	public static LogAttributeData? GetLogAttribute(ISymbol symbol, CancellationToken token)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				TypeLibrary.Logging.LogAttributeTargets,
				token,
				out var matchingType,
				out var attributeData
			)
		)
		{
			return null;
		}

		if (matchingType == TypeLibrary.Logging.LogAttribute)
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

		return GetSpecificLogData(matchingType, attributeData!);
	}

	static LogAttributeData GetSpecificLogData(TypeIdentity template, AttributeData attributeData)
	{
		if (template == TypeLibrary.Logging.TraceAttribute)
		{
			var data = TraceAttributeData.FromAttributeData(attributeData);
			return data.ToLogAttribute();
		}

		if (template == TypeLibrary.Logging.DebugAttribute)
		{
			var data = DebugAttributeData.FromAttributeData(attributeData);
			return data.ToLogAttribute();
		}

		if (template == TypeLibrary.Logging.InfoAttribute)
		{
			var data = InfoAttributeData.FromAttributeData(attributeData);
			return data.ToLogAttribute();
		}

		if (template == TypeLibrary.Logging.WarningAttribute)
		{
			var data = WarningAttributeData.FromAttributeData(attributeData);
			return data.ToLogAttribute();
		}

		if (template == TypeLibrary.Logging.ErrorAttribute)
		{
			var data = ErrorAttributeData.FromAttributeData(attributeData);
			return data.ToLogAttribute();
		}

		var criticalData = CriticalAttributeData.FromAttributeData(attributeData);
		return criticalData.ToLogAttribute();
	}

	public static LoggerAttributeData? GetLoggerAttribute(ISymbol symbol, CancellationToken token)
	{
		if (!Utilities.TryContainsAttribute(symbol, TypeLibrary.Logging.LoggerAttribute, token, out var attributeData))
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
				TypeLibrary.Logging.LoggerGenerationAttribute,
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
				TypeLibrary.Logging.ExpandEnumerableAttribute,
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
