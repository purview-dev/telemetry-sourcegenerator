using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

static partial class PipelineHelpers
{
	static string GenerateClassName(string name)
	{
		if (name[0] == 'I')
			name = name.Substring(1);

		return name + "Core";
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
	internal static string GenerateParameterName(string name, string? prefix, bool lowercase, int namingConvention = 1)
	{
		// NamingConvention: 0 = Legacy, 1 = OpenTelemetry
		var isLegacy = namingConvention == 0;

		if (!isLegacy && lowercase)
		{
			// OpenTelemetry: Convert PascalCase to snake_case for tags/baggage
			name = Utilities.ConvertToSeparatedLowercase(name, '_');
		}
		else if (lowercase)
		{
			// Legacy: Just lowercase without word-boundary splitting
			name = name.ToLowerInvariant();
		}

		return $"{prefix}{name}";
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Style",
		"IDE0075:Simplify conditional expression",
		Justification = "Don't 'simplify' this as changing the default value of the skipOnNullOrEmpty parameter will change the behaviour"
	)]
	internal static bool GetSkipOnNullOrEmptyValue(TagOrBaggageAttributeRecord? tagOrBaggageAttribute) =>
		tagOrBaggageAttribute?.SkipOnNullOrEmpty == true;

	/// <summary>
	/// The interface's instance methods: the members a generated class implements.
	/// </summary>
	/// <remarks>
	/// Static members are not part of the contract a class implements, so they are skipped. An interface could
	/// already declare them on .NET Core 3.0+ (static helpers) and .NET 7+ (<c>static abstract</c>/<c>static
	/// virtual</c>); C# 15 lets a .NET Framework or netstandard2.0 interface declare non-virtual static members too,
	/// so a static helper on a telemetry interface is now possible everywhere the generator runs.
	/// </remarks>
	internal static IEnumerable<IMethodSymbol> GetAllInterfaceMethods(
		INamedTypeSymbol interfaceSymbol,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();
		return GetInstanceMethods(interfaceSymbol);
	}

	/// <inheritdoc cref="GetAllInterfaceMethods"/>
	internal static IEnumerable<IMethodSymbol> GetInstanceMethods(INamedTypeSymbol interfaceSymbol) =>
		interfaceSymbol.GetMembers().OfType<IMethodSymbol>().Where(static method => !method.IsStatic);
}
