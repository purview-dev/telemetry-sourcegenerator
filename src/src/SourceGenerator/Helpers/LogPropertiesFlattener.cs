using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

/// <summary>
/// Flattens the readable properties of a <c>[LogProperties]</c> parameter into the tag entries the generated log
/// state carries.
/// </summary>
/// <remarks>
/// Without <c>Transitive</c> the parameter's own properties are flattened, which is what the generator has always
/// done. With <c>[LogProperties(Transitive = true)]</c> each property whose type is itself a complex object is
/// flattened in turn, producing dotted tag names such as <c>weather.Location.City</c>.
/// <para>
/// The walk mirrors <c>Microsoft.Gen.Logging</c>'s <c>Parser.LogProperties</c>: it takes public instance properties
/// with a public getter, de-duplicates a property hidden by a derived type, walks base types, skips indexers and
/// statics, honours <c>[LogPropertyIgnore]</c> at every level, and recurses only into complex reference types and
/// non-nullable structs. <see cref="MaxTransitiveDepth"/> matches that generator's
/// <c>MaxTypeTraversalDepth</c>.
/// </para>
/// <para>
/// Two deliberate divergences, both documented on <c>docs/wiki/Logging.md</c>: a type already on the visited chain
/// stops only that branch instead of abandoning the whole parameter, and a <c>Nullable&lt;T&gt;</c> property is
/// flattened no further because the extra <c>.Value</c> hop the accessor would need does not fit the generated
/// null-conditional chain.
/// </para>
/// </remarks>
static class LogPropertiesFlattener
{
	/// <summary>
	/// The number of types, including the parameter's own, that may appear on one property chain. A chain that would
	/// exceed it stops flattening, so a type graph deep enough to be rejected by the .NET 11 generator is not walked
	/// here either. A self-referencing or mutually-referencing graph stops at the first repeated type instead.
	/// </summary>
	internal const int MaxTransitiveDepth = 32;

	/// <summary>
	/// Known value-like types that are rendered with their own <c>ToString</c> rather than expanded. Expanding a
	/// <c>DateTime</c> into <c>Year</c>/<c>Month</c>/... would replace a useful value with noise.
	/// </summary>
	static readonly HashSet<string> StringifiableTypes = new(StringComparer.Ordinal)
	{
		"System.DateOnly",
		"System.DateTime",
		"System.DateTimeOffset",
		"System.Guid",
		"System.TimeOnly",
		"System.TimeSpan",
		"System.Uri",
		"System.Version",
	};

	public static EquatableArray<LogPropertiesParameterDetails> Flatten(
		ITypeSymbol parameterType,
		LogPropertiesAttributeData attribute,
		CancellationToken token
	)
	{
		var flattenedType = UnwrapNullable(parameterType);

		List<LogPropertiesParameterDetails> properties = [];
		HashSet<ITypeSymbol> chain = new(SymbolEqualityComparer.Default) { flattenedType };

		FlattenType(
			flattenedType,
			namePrefix: string.Empty,
			accessorPrefix: string.Empty,
			// The existing generator always writes `param?.Property`, which is only valid when the parameter can be
			// null: a reference type or a Nullable<T>.
			receiverIsNullable: IsNullableType(parameterType),
			transitive: attribute.Transitive,
			chain,
			properties,
			token
		);

		return new([.. properties]);
	}

	static void FlattenType(
		ITypeSymbol type,
		string namePrefix,
		string accessorPrefix,
		bool receiverIsNullable,
		bool transitive,
		HashSet<ITypeSymbol> chain,
		List<LogPropertiesParameterDetails> properties,
		CancellationToken token
	)
	{
		// A property declared on a base type that a derived type hides is logged once, as the derived member.
		HashSet<string> seenPropertyNames = new(StringComparer.Ordinal);

		var current = type as INamedTypeSymbol;
		while (current is not null && current.SpecialType != SpecialType.System_Object)
		{
			token.ThrowIfCancellationRequested();

			foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
			{
				token.ThrowIfCancellationRequested();

				if (property.IsIndexer || property.IsStatic)
					continue;

				if (!seenPropertyNames.Add(property.Name))
					continue;

				if (
					property.DeclaredAccessibility != Accessibility.Public
					|| property.GetMethod is not { DeclaredAccessibility: Accessibility.Public }
				)
					continue;

				if (
					Utilities.ContainsAttribute(
						property,
						TypeLibrary.Microsoft.Extensions.Logging.LogPropertyIgnoreAttribute,
						token
					)
				)
				{
					continue;
				}

				var propertyIsNullable = IsNullableType(property.Type);
				var propertyName = namePrefix + property.Name;
				var accessor = accessorPrefix + (receiverIsNullable ? "?." : ".") + property.Name;

				properties.Add(new(PropertyName: propertyName, IsNullable: propertyIsNullable, Accessor: accessor));

				if (!transitive)
					continue;

				var propertyType = UnwrapNullable(property.Type);
				if (!IsTransitivelyExpandable(propertyType))
					continue;

				// A cycle stops this branch only: the parameters already flattened stay, and the repeated type's own
				// properties are not walked again.
				if (chain.Contains(propertyType))
					continue;

				if (chain.Count >= MaxTransitiveDepth)
					continue;

				chain.Add(propertyType);
				FlattenType(
					propertyType,
					propertyName + ".",
					accessor,
					propertyIsNullable,
					transitive,
					chain,
					properties,
					token
				);
				chain.Remove(propertyType);
			}

			current = current.BaseType;
		}
	}

	static ITypeSymbol UnwrapNullable(ITypeSymbol type) =>
		type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T && type is INamedTypeSymbol named
			? named.TypeArguments[0]
			: type;

	/// <summary>
	/// Whether a value of this type can be null, and therefore whether the accessor chain needs a <c>?</c>. A
	/// non-nullable value type cannot take one, so a struct-typed parameter or property is accessed with a plain dot.
	/// </summary>
	static bool IsNullableType(ITypeSymbol type) =>
		type.IsReferenceType || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

	/// <summary>
	/// Whether the property's type is a complex object worth walking into. Primitives, enums, delegates, arrays,
	/// collections, ref structs, type parameters and the well-known value-like BCL types stay single tags whose value
	/// the logging provider renders with <c>ToString</c>.
	/// </summary>
	static bool IsTransitivelyExpandable(ITypeSymbol type)
	{
		if (type is not INamedTypeSymbol named)
			return false;

		if (named.TypeKind is not (TypeKind.Class or TypeKind.Struct or TypeKind.Interface))
			return false;

		if (named.IsStatic || named.IsRefLikeType)
			return false;

		if (named.SpecialType != SpecialType.None)
			return false;

		if (named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
			return false;

		if (StringifiableTypes.Contains(named.ToDisplayString()))
			return false;

		// A collection is one tag: expanding List<T> into Capacity/Count is not what a reader wants, and an array's
		// elements are not expanded by the .NET generator either.
		if (named.AllInterfaces.Any(static i => i.SpecialType == SpecialType.System_Collections_IEnumerable))
			return false;

		return true;
	}
}
