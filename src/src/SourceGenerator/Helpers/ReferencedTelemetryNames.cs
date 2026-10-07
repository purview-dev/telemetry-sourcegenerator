using Microsoft.CodeAnalysis;

namespace Purview.Telemetry.SourceGenerator.Helpers;

/// <summary>
/// Reads the <c>[assembly: GeneratedTelemetryNames]</c> attribute back out of referenced assemblies.
/// The generator applies that attribute to every assembly it generates telemetry for, which makes the
/// names available as compile-time metadata — no reflection, no assembly scanning, so the aggregate
/// survives trimming and native AOT.
/// </summary>
static class ReferencedTelemetryNames
{
	static readonly string AttributeName = TypeLibrary.Purview.Telemetry.GeneratedTelemetryNamesAttribute.Name;
	static readonly string? AttributeNamespace = TypeLibrary
		.Purview
		.Telemetry
		.GeneratedTelemetryNamesAttribute
		.Namespace;

	/// <summary>
	/// Adds the activity source and meter names recorded by every referenced assembly to the supplied
	/// sets. <see cref="IModuleSymbol.ReferencedAssemblySymbols"/> is the flattened reference set the
	/// compiler was handed, so transitive references are included as long as the build passes them to
	/// the compiler — which is what MSBuild does for project and package references.
	/// </summary>
	public static void Collect(
		Compilation compilation,
		ISet<string> activitySourceNames,
		ISet<string> meterNames,
		CancellationToken token
	)
	{
		foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
		{
			token.ThrowIfCancellationRequested();

			foreach (var attribute in assembly.GetAttributes())
			{
				if (!IsGeneratedTelemetryNamesAttribute(attribute))
					continue;

				AddNames(attribute, constructorIndex: 0, "ActivitySourceNames", activitySourceNames);
				AddNames(attribute, constructorIndex: 1, "MeterNames", meterNames);
			}
		}
	}

	/// <summary>
	/// Matches by name rather than by symbol identity: the attribute is generated as an
	/// <see langword="internal"/> type into each assembly, so every referenced copy is a distinct
	/// symbol.
	/// </summary>
	static bool IsGeneratedTelemetryNamesAttribute(AttributeData attribute)
	{
		var attributeClass = attribute.AttributeClass;
		if (attributeClass is null || !string.Equals(attributeClass.Name, AttributeName, StringComparison.Ordinal))
			return false;

		var containingNamespace = attributeClass.ContainingNamespace;
		var @namespace = containingNamespace is { IsGlobalNamespace: false }
			? containingNamespace.ToDisplayString()
			: null;

		return string.Equals(@namespace, AttributeNamespace, StringComparison.Ordinal);
	}

	/// <summary>
	/// Reads one of the attribute's string arrays, preferring the constructor argument and falling
	/// back to the named property of the same name.
	/// </summary>
	static void AddNames(AttributeData attribute, int constructorIndex, string propertyName, ISet<string> names)
	{
		var values =
			attribute.ConstructorArguments.Length > constructorIndex
				? attribute.ConstructorArguments[constructorIndex]
				: default;

		if (values.Kind != TypedConstantKind.Array)
		{
			foreach (var namedArgument in attribute.NamedArguments)
			{
				if (!string.Equals(namedArgument.Key, propertyName, StringComparison.Ordinal))
					continue;

				values = namedArgument.Value;
				break;
			}
		}

		if (values.Kind != TypedConstantKind.Array || values.IsNull)
			return;

		foreach (var value in values.Values)
		{
			if (value.Value is string name && !string.IsNullOrWhiteSpace(name))
				names.Add(name);
		}
	}
}
