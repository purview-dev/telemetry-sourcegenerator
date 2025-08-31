using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Globalization",
		"CA1308:Normalize strings to upper-case"
	)]
	static string GenerateParameterName(string name, string? prefix, bool lowercase)
	{
		if (lowercase)
			name = name.ToLowerInvariant();

		return $"{prefix}{name}";
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Style",
		"IDE0075:Simplify conditional expression",
		Justification = "Don't 'simplify' this as changing the default value of the skipOnNullOrEmpty parameter will change the behaviour"
	)]
	static bool GetSkipOnNullOrEmptyValue(TagOrBaggageAttributeRecord? tagOrBaggageAttribute) =>
		tagOrBaggageAttribute?.SkipOnNullOrEmpty.IsSet == true
		&& tagOrBaggageAttribute.SkipOnNullOrEmpty.Value!.Value;

	static IEnumerable<IMethodSymbol> GetAllInterfaceMethods(
		INamedTypeSymbol interfaceSymbol,
		Compilation compilation,
		CancellationToken token
	)
	{
		HashSet<string> seen = [];
		foreach (var syntaxRef in interfaceSymbol.DeclaringSyntaxReferences)
		{
			token.ThrowIfCancellationRequested();
			if (syntaxRef.GetSyntax(token) is not InterfaceDeclarationSyntax syntax)
				continue;

			var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
			foreach (var member in syntax.Members.OfType<MethodDeclarationSyntax>())
			{
				if (
					semanticModel.GetDeclaredSymbol(member, token) is IMethodSymbol symbol
					&& seen.Add(symbol.ToDisplayString())
				)
				{
					yield return symbol;
				}
			}
		}
	}

	static ImmutableDictionary<string, Location[]> BuildDuplicateMethods(
		INamedTypeSymbol interfaceSymbol,
		Compilation compilation,
		CancellationToken token
	)
	{
		var methods = GetAllInterfaceMethods(interfaceSymbol, compilation, token);
		Dictionary<string, List<Location>> dict = [];
		foreach (var method in methods)
		{
			if (dict.TryGetValue(method.Name, out var list))
				list.AddRange(method.Locations);
			else
				dict[method.Name] = [.. method.Locations];
		}
		return dict.Where(m => m.Value.Count > 1)
			.ToImmutableDictionary(m => m.Key, m => m.Value.ToArray());
	}

	static ImmutableDictionary<string, Location[]> BuildDuplicateMethods(
		INamedTypeSymbol interfaceSymbol,
		SemanticModel semanticModel,
		CancellationToken token
	) => BuildDuplicateMethods(interfaceSymbol, semanticModel.Compilation, token);
}
