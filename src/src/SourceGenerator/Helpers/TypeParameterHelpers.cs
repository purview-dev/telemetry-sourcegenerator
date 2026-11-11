using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.SourceGeneratorFramework;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

/// <summary>
/// Projects a Roslyn type parameter list into the value-equatable <see cref="TypeParameterTarget"/> list a generated
/// method or class re-declares.
/// </summary>
/// <remarks>
/// The supported constraints are the ones C# lets a generated declaration repeat: <c>struct</c>, <c>class</c>,
/// <c>class?</c>, <c>notnull</c>, <c>unmanaged</c>, <c>new()</c>, and type-parameter or type constraints. The
/// <c>allows ref struct</c> anti-constraint cannot be expressed on the generated declaration nor satisfied by the
/// generated state, so a type parameter carrying it is reported as unsupported rather than silently dropped.
/// </remarks>
static class TypeParameterHelpers
{
	static readonly SymbolDisplayFormat ConstraintTypeFormat =
		SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
			SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
				| SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
		);

	public static EquatableArray<TypeParameterTarget> FromMethod(IMethodSymbol method) => From(method.TypeParameters);

	public static EquatableArray<TypeParameterTarget> FromNamedType(INamedTypeSymbol type) => From(type.TypeParameters);

	public static EquatableArray<TypeParameterTarget> From(ImmutableArray<ITypeParameterSymbol> typeParameters)
	{
		if (typeParameters.IsDefaultOrEmpty)
			return default;

		List<TypeParameterTarget> targets = new(typeParameters.Length);
		foreach (var typeParameter in typeParameters)
			targets.Add(From(typeParameter));

		return new(targets.ToImmutableArray());
	}

	static TypeParameterTarget From(ITypeParameterSymbol typeParameter)
	{
		List<string> constraints = [];

		if (typeParameter.HasReferenceTypeConstraint)
		{
			constraints.Add(
				typeParameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated
					? "class?"
					: "class"
			);
		}
		else if (typeParameter.HasValueTypeConstraint)
		{
			// `unmanaged` implies `struct`, and C# rejects them together.
			constraints.Add(typeParameter.HasUnmanagedTypeConstraint ? "unmanaged" : "struct");
		}
		else if (typeParameter.HasNotNullConstraint)
		{
			constraints.Add("notnull");
		}

		foreach (var constraintType in typeParameter.ConstraintTypes)
		{
			if (constraintType is IErrorTypeSymbol)
				continue;

			constraints.Add(constraintType.ToDisplayString(ConstraintTypeFormat));
		}

		// `new()` is implied by a value-type constraint and rejected alongside it.
		if (typeParameter.HasConstructorConstraint && !typeParameter.HasValueTypeConstraint)
			constraints.Add("new()");

		return new(typeParameter.Name, constraints.ToImmutableArray());
	}

	/// <summary>
	/// Whether every type parameter can be re-declared by the generator. The only shape it cannot express today is the
	/// <c>allows ref struct</c> anti-constraint, which neither the generated declaration nor the generated state can
	/// carry.
	/// </summary>
	public static bool AreSupported(ImmutableArray<ITypeParameterSymbol> typeParameters)
	{
		if (typeParameters.IsDefaultOrEmpty)
			return true;

		foreach (var typeParameter in typeParameters)
		{
			if (typeParameter.AllowsRefLikeType)
				return false;
		}

		return true;
	}

	public static ImmutableArray<GenericTypeParameterOptions> ToOptions(
		EquatableArray<TypeParameterTarget> typeParameters
	)
	{
		if (typeParameters.IsEmpty)
			return [];

		var builder = ImmutableArray.CreateBuilder<GenericTypeParameterOptions>(typeParameters.Count);

		foreach (var typeParameter in typeParameters)
		{
			builder.Add(
				new GenericTypeParameterOptions(typeParameter.Name)
				{
					Constraints = [.. typeParameter.Constraints.AsImmutableArray()],
				}
			);
		}

		return builder.MoveToImmutable();
	}
}
