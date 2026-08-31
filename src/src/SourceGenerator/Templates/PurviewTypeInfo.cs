using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Purview.Telemetry.SourceGenerator.Templates;

sealed record PurviewTypeInfo(
	string TypeName,
	string FullyQualifiedName,
	string? Namespace,
	string? SystemAlias,
	bool IsNullable,
	bool IsValueType,
	SpecialType SpecialType,
	EquatableArray<PurviewTypeInfo> GenericTypeArguments
) : IEquatable<PurviewTypeInfo>, IEquatable<ITypeSymbol>, IEquatable<string>
{
	public bool Equals(PurviewTypeInfo? other) => other != null && other.FullyQualifiedName == FullyQualifiedName;

	public bool Equals(ITypeSymbol? typeSymbol)
	{
		if (typeSymbol == null)
			return false;

		var typeInfo = PurviewTypeFactory.Create(typeSymbol);
		return Equals(typeInfo);
	}

	public bool Equals(string? other)
	{
		if (other == null)
			return false;

		if (other.Length > 0 && other[other.Length - 1] == '?')
			other = other.Substring(0, other.Length - 1);

		return other == TypeName || other == FullyQualifiedName || other == SystemAlias;
	}

	public override int GetHashCode() => ToString().GetHashCode();

	public PurviewTypeInfo WithNullable(bool isNullable = true) =>
		new(
			TypeName,
			FullyQualifiedName,
			Namespace,
			SystemAlias,
			isNullable,
			IsValueType,
			SpecialType,
			GenericTypeArguments
		);

	public PurviewTypeInfo MakeGeneric(params PurviewTypeInfo[] types) =>
		new(
			TypeName,
			FullyQualifiedName,
			Namespace,
			SystemAlias,
			IsNullable,
			IsValueType,
			SpecialType,
			types.ToImmutableArray()
		);

	public string MakeGeneric(bool includeGlobal, params string[] types) =>
		(includeGlobal ? "global::" : null)
		+ FullyQualifiedName
		+ '<'
		+ string.Join(", ", types)
		+ '>'
		+ (IsNullable ? '?' : null);

	public override string ToString() => ToString(includeGlobal: true);

	public string ToString(bool includeGlobal) => ToString(includeGlobal, emitNullableAnnotations: true);

	public string ToString(bool includeGlobal, bool emitNullableAnnotations)
	{
		// Always emit ? for value types (Nullable<T>); reference type ? requires C# 8+.
		var isNullableSuffix = IsNullable && (IsValueType || emitNullableAnnotations) ? "?" : null;
		var result = SystemAlias ?? ((includeGlobal ? "global::" : null) + FullyQualifiedName);

		if (GenericTypeArguments.Count > 0)
		{
			result +=
				"<"
				+ string.Join(
					", ",
					GenericTypeArguments.Select(m => m.ToString(includeGlobal, emitNullableAnnotations))
				)
				+ ">";
		}

		return result + isNullableSuffix;
	}

	public static implicit operator string(PurviewTypeInfo typeInfo) => typeInfo.ToString();
}
