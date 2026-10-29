using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class SharedHelpers
{
	public static ActivitySourceGenerationAttributeData? GetActivitySourceGenerationAttribute(
		Compilation compilation,
		CancellationToken token
	) => GetActivitySourceGenerationAttribute(compilation.Assembly, token);

	/// <summary>
	/// Whether the compilation can resolve <c>System.Diagnostics.ActivitySourceFactory</c> (.NET 11, or
	/// System.Diagnostics.DiagnosticSource 11+ on any target).
	/// </summary>
	public static bool IsActivitySourceFactoryAvailable(Compilation compilation) =>
		compilation.GetTypeByMetadataName(TypeLibrary.System.Diagnostics.ActivitySourceFactory.MetadataFullName)
			is not null
		&& compilation.GetTypeByMetadataName(TypeLibrary.System.Diagnostics.ActivitySourceOptions.MetadataFullName)
			is not null;

	/// <summary>
	/// Whether the assembly opted in with <c>[ActivitySourceGeneration(UseActivitySourceFactory = true)]</c>.
	/// </summary>
	public static bool IsActivitySourceFactoryRequested(Compilation compilation, CancellationToken token) =>
		GetActivitySourceGenerationAttribute(compilation, token)?.UseActivitySourceFactory == true;

	/// <summary>
	/// Whether generated activity sources are created by an injected <c>ActivitySourceFactory</c>: requested and
	/// available. A request the compilation cannot honour falls back to a static <c>ActivitySource</c> (and the analyzer
	/// reports <c>TSG3023</c>).
	/// </summary>
	public static bool UseActivitySourceFactory(Compilation compilation, CancellationToken token) =>
		IsActivitySourceFactoryRequested(compilation, token) && IsActivitySourceFactoryAvailable(compilation);

	public static ActivitySourceAttributeData? GetActivitySourceAttribute(ISymbol symbol, CancellationToken token)
	{
		if (
			!Utilities.TryContainsAttribute(
				symbol,
				TypeLibrary.Purview.Telemetry.ActivitySourceAttribute,
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
				TypeLibrary.Purview.Telemetry.ActivitySourceGenerationAttribute,
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
				TypeLibrary.Purview.Telemetry.ActivityAttribute,
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
				TypeLibrary.Purview.Telemetry.EventAttribute,
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
		return Utilities.ContainsAttribute(method, TypeLibrary.Purview.Telemetry.ActivityAttribute, token)
			|| Utilities.ContainsAttribute(method, TypeLibrary.Purview.Telemetry.EventAttribute, token)
			|| Utilities.ContainsAttribute(method, TypeLibrary.Purview.Telemetry.ContextAttribute, token);
	}
}
