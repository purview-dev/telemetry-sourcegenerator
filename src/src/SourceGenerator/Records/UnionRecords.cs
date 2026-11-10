namespace Purview.Telemetry.SourceGenerator.Records;

/// <summary>
/// A union-typed telemetry parameter: the union's declared type, the case types it can hold, and how the
/// generated code reads a case value back out of it.
/// </summary>
/// <remarks>
/// A union is recorded as its case value rather than as the union struct: <c>SetTag</c>, a log value or a
/// metric tag has no meaning for a wrapper, and a default union holds no case at all. When the union
/// declares the non-boxing access pattern (a <c>bool HasValue</c> property plus a <c>TryGetValue(out T)</c>
/// overload per case type) the generated accessor calls those, so the union struct itself is never boxed
/// to read its contents. A union declared with the <c>union</c> keyword has no <c>TryGetValue</c> — the
/// language-generated form is always a struct holding <c>object?</c> — so its contents come from
/// <c>Value</c>, which is already the boxed case value.
/// </remarks>
/// <param name="UnionType">The parameter's declared type, nullability included.</param>
/// <param name="UnderlyingUnionType">The union type itself, without any nullable wrapper or annotation.</param>
/// <param name="CaseTypes">The case types, in declaration order.</param>
/// <param name="IsValueType">Whether the union is a struct (a compiler-generated union always is).</param>
/// <param name="IsNullable">Whether the parameter is a nullable union or a <c>Nullable&lt;T&gt;</c> of one.</param>
/// <param name="UsesNonBoxingAccess">Whether the union declares the non-boxing access pattern.</param>
sealed record UnionTargetRecord(
	TypeReference UnionType,
	TypeReference UnderlyingUnionType,
	EquatableArray<TypeReference> CaseTypes,
	bool IsValueType,
	bool IsNullable,
	bool UsesNonBoxingAccess
)
{
	/// <summary>
	/// The name of the generated <c>static object?</c> accessor that unwraps this union. It is derived from
	/// the union's metadata name, so two unions with the same short name in different namespaces do not
	/// collide, and it is stable across runs so the generated output stays deterministic.
	/// </summary>
	public string AccessorName { get; } = "UnwrapUnion_" + ToIdentifier(UnderlyingUnionType.Identity.MetadataFullName);

	/// <summary>
	/// The expression that reads the case value out of a parameter of this union: a call to the generated
	/// accessor, which returns the case value or <see langword="null"/> when the parameter holds no value.
	/// </summary>
	public string RenderValueExpression(string parameterName) => AccessorName + "(" + parameterName + ")";

	static string ToIdentifier(string metadataName)
	{
		// MetadataFullName renders generic arity as `1 and nested types with '+', neither of which is a
		// legal identifier character.
		System.Text.StringBuilder builder = new(metadataName.Length);

		foreach (var c in metadataName)
			builder.Append(char.IsLetterOrDigit(c) ? c : '_');

		return builder.ToString();
	}
}
