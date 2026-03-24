using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Templates;

sealed record PurviewTypeInfo(
	string TypeName,
	string FullyQualifiedName,
	string? Namespace,
	string? SystemAlias,
	bool IsNullable,
	SpecialType SpecialType,
	EquatableArray<PurviewTypeInfo> GenericTypeArguments
) : IEquatable<PurviewTypeInfo>, IEquatable<ITypeSymbol>, IEquatable<string>
{
	public bool Equals(PurviewTypeInfo? other) =>
		other != null && other.FullyQualifiedName == FullyQualifiedName;

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

	public string ToString(bool includeGlobal)
	{
		var isNullableSuffix = IsNullable ? "?" : null;
		var result = (SystemAlias ?? (includeGlobal ? "global::" : null) + FullyQualifiedName);

		if (GenericTypeArguments.Length > 0)
		{
			result +=
				"<"
				+ string.Join(", ", GenericTypeArguments.Select(m => m.ToString(includeGlobal)))
				+ ">";
		}

		return result + isNullableSuffix;
	}

	public static implicit operator string(PurviewTypeInfo typeInfo) => typeInfo.ToString();
}
