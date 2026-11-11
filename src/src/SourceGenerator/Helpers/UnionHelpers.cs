using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

/// <summary>
/// Recognises union types used as telemetry parameters, and describes how the generated code should read a case value
/// out of one.
/// </summary>
/// <remarks>
/// Detection is symbol based, never target-framework based: a union is a plain type declaration, so the
/// compilation resolves it the same way on net48 as on net11.0. What differs is only where the case list comes
/// from, and whether the union declares the non-boxing access pattern.
/// <para>
/// Two unions are recognised, both by metadata name and namespace only — this assembly references neither package:
/// a type carrying <c>[System.Runtime.CompilerServices.Union]</c> (a <c>union</c> declaration or a hand-written
/// union), and a type carrying <c>[Purview.Unions.GenerateUnion]</c>, whose members are emitted by the Purview.Unions
/// generator. Roslyn generators never see another generator's output, so for the second kind the case list has to
/// come from what <em>is</em> visible in source: the attribute's <c>typeof</c> list, the union's partial static
/// factory signatures, a nested <c>IUnionMembers</c> provider, or its constructors.
/// </para>
/// </remarks>
static class UnionHelpers
{
	public const string UnionAttributeName = "UnionAttribute";
	public const string CompilerServicesNamespace = "System.Runtime.CompilerServices";

	/// <summary>
	/// <c>Purview.Unions.GenerateUnionAttribute</c>, matched by name so this generator needs no reference to the
	/// Purview.Unions package.
	/// </summary>
	public const string GenerateUnionAttributeName = "GenerateUnionAttribute";

	/// <summary>The namespace <c>GenerateUnionAttribute</c> lives in, matched by name for the same reason.</summary>
	public const string PurviewUnionsNamespace = "Purview.Unions";

	public const string HasValueMemberName = "HasValue";
	public const string TryGetValueMemberName = "TryGetValue";
	public const string ValueMemberName = "Value";
	public const string UnionMembersInterfaceName = "IUnionMembers";
	public const string UnionMembersCreateMethodName = "Create";

	static readonly SymbolDisplayFormat CaseIdentityFormat = SymbolDisplayFormat.FullyQualifiedFormat;

	/// <summary>
	/// Builds the <see cref="UnionTargetRecord"/> for a telemetry parameter, or <see langword="null"/> when
	/// the parameter's type is not a union, or is a <c>[GenerateUnion]</c> target whose case list is not visible in
	/// source.
	/// </summary>
	public static UnionTargetRecord? GetUnionTarget(ITypeSymbol parameterType, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		var isNullable = false;
		var unionType = parameterType;

		if (
			parameterType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
			&& parameterType is INamedTypeSymbol { TypeArguments.Length: 1 } nullable
		)
		{
			isNullable = true;
			unionType = nullable.TypeArguments[0];
		}
		else if (parameterType.NullableAnnotation == NullableAnnotation.Annotated)
		{
			isNullable = true;
		}

		if (!IsUnion(unionType))
			return null;

		if (unionType is not INamedTypeSymbol namedUnionType)
			return null;

		var isGeneratedUnion = HasAttribute(namedUnionType, GenerateUnionAttributeName, PurviewUnionsNamespace);

		var caseTypes = isGeneratedUnion
			? GetGeneratedUnionCaseTypes(namedUnionType, token)
			: GetCaseTypes(namedUnionType);

		// A [GenerateUnion] target whose case list is not visible in source is not treated as a union at all: the
		// unions generator reports its own UNI0003 for it, and recording a wrapper whose shape is unknown would be
		// worse than recording it as an ordinary value.
		if (caseTypes.Count == 0)
			return null;

		return new(
			UnionType: TypeReference.Create(parameterType),
			UnderlyingUnionType: TypeReference.Create(unionType),
			CaseTypes: new([.. caseTypes.Select(static caseType => TypeReference.Create(caseType))]),
			IsValueType: unionType.IsValueType,
			IsNullable: isNullable,
			// The unions generator emits `HasValue` and one `TryGetValue(out T)` per case for every [GenerateUnion]
			// surface, but emits them into generated output this generator never sees. The final compilation does see
			// them, so the accessor is emitted against them. This is deliberately not extended to a [Union] type: some
			// declare the pattern and some do not, so that stays a visible-member decision.
			UsesNonBoxingAccess: isGeneratedUnion || UsesNonBoxingAccess(unionType, caseTypes.Count)
		);
	}

	/// <summary>
	/// Whether the type is a union: it carries <c>[System.Runtime.CompilerServices.Union]</c> or
	/// <c>[Purview.Unions.GenerateUnion]</c>. Both are matched by metadata name and namespace, so this generator
	/// depends on neither package.
	/// </summary>
	public static bool IsUnion(ITypeSymbol type) =>
		HasAttribute(type, UnionAttributeName, CompilerServicesNamespace)
		|| HasAttribute(type, GenerateUnionAttributeName, PurviewUnionsNamespace);

	/// <summary>
	/// The case types a union can hold: the single-parameter <c>Create</c> members of a nested
	/// <c>IUnionMembers</c> provider when there is one, otherwise the union's own single-parameter
	/// constructors, which is what the compiler generates for a <c>union</c> declaration.
	/// </summary>
	static List<ITypeSymbol> GetCaseTypes(INamedTypeSymbol unionType)
	{
		List<ITypeSymbol> caseTypes = [];

		var provider = unionType.GetTypeMembers(UnionMembersInterfaceName).FirstOrDefault();
		if (provider is not null)
		{
			foreach (
				var method in provider
					.GetMembers(UnionMembersCreateMethodName)
					.OfType<IMethodSymbol>()
					.Where(static m => m.IsStatic && m.Parameters.Length == 1)
			)
				caseTypes.Add(method.Parameters[0].Type);
		}

		if (caseTypes.Count == 0)
		{
			foreach (
				var constructor in unionType.InstanceConstructors.Where(static c =>
					c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic && c.Parameters.Length == 1
				)
			)
				caseTypes.Add(constructor.Parameters[0].Type);
		}

		return caseTypes;
	}

	/// <summary>
	/// The case types of a <c>[GenerateUnion]</c> target, in the union generator's own order: the attribute's
	/// <c>typeof</c> list, the union's partial static factory signatures, a nested <c>IUnionMembers</c> provider, then
	/// any source-visible public single-parameter constructors. The first non-empty list wins, and every case is
	/// normalised the way that generator exposes it.
	/// </summary>
	static List<ITypeSymbol> GetGeneratedUnionCaseTypes(INamedTypeSymbol unionType, CancellationToken token)
	{
		var candidates = GetAttributeCaseTypes(unionType);
		if (candidates.Count == 0)
			candidates = GetFactoryCaseTypes(unionType, token);
		if (candidates.Count == 0)
			candidates = GetProviderCaseTypes(unionType, token);
		if (candidates.Count == 0)
			candidates = GetConstructorCaseTypes(unionType, token);

		List<ITypeSymbol> caseTypes = [];
		HashSet<string> seen = [with(StringComparer.Ordinal)];

		foreach (var candidate in candidates)
		{
			token.ThrowIfCancellationRequested();

			if (!IsUsableCaseType(candidate))
				continue;

			var normalized = NormalizeCaseType(candidate, unionType);
			if (normalized is null || !IsUsableCaseType(normalized))
				continue;

			// A duplicate case would emit a second, identical TryGetValue test. The unions generator reports
			// duplicates itself, so keeping the first occurrence keeps the accessor deterministic and warning-free.
			if (seen.Add(normalized.ToDisplayString(CaseIdentityFormat)))
				caseTypes.Add(normalized);
		}

		return caseTypes;
	}

	/// <summary>
	/// Whether a case can be named in generated code. A <c>[GenerateUnion]</c> whose case list names something that
	/// cannot be a case is a broken union: the unions generator reports it, and this generator must degrade to "not a
	/// union" rather than fail the whole build with <c>TSG1000</c>.
	/// </summary>
	static bool IsUsableCaseType(ITypeSymbol caseType) =>
		caseType.TypeKind is not (TypeKind.Error or TypeKind.Pointer or TypeKind.FunctionPointer or TypeKind.Dynamic)
		&& (caseType is ITypeParameterSymbol || TypeReference.TryCreate(caseType, out _));

	/// <summary>
	/// Reads the <c>[GenerateUnion(typeof(...))]</c> case list, in declaration order.
	/// </summary>
	static List<ITypeSymbol> GetAttributeCaseTypes(INamedTypeSymbol unionType)
	{
		foreach (var attribute in unionType.GetAttributes())
		{
			if (!IsAttribute(attribute.AttributeClass, GenerateUnionAttributeName, PurviewUnionsNamespace))
				continue;

			// The attribute's only constructor takes the case types as a params array.
			if (attribute.ConstructorArguments.Length != 1)
				return [];

			var argument = attribute.ConstructorArguments[0];
			if (argument.Kind != TypedConstantKind.Array || argument.IsNull)
				return [];

			List<ITypeSymbol> caseTypes = [];
			foreach (var value in argument.Values)
			{
				if (value.Kind == TypedConstantKind.Type && value.Value is ITypeSymbol caseType)
					caseTypes.Add(caseType);
			}

			return caseTypes;
		}

		return [];
	}

	/// <summary>
	/// Reads the case types from the union's own partial static factory signatures, in source order.
	/// </summary>
	static List<ITypeSymbol> GetFactoryCaseTypes(INamedTypeSymbol unionType, CancellationToken token)
	{
		List<ITypeSymbol> caseTypes = [];

		foreach (var method in OrderedFactoryMethods(unionType, token))
			caseTypes.Add(method.Parameters[0].Type);

		return caseTypes;
	}

	/// <summary>
	/// The partial static factory signatures to read cases from, in source order.
	/// </summary>
	/// <remarks>
	/// A factory is a static method with exactly one unrestricted parameter whose return type is the union, declared as
	/// a <c>partial</c> signature without a body — the same rule the union generator applies, so the list read here is
	/// the list it implements.
	/// </remarks>
	static IEnumerable<IMethodSymbol> OrderedFactoryMethods(INamedTypeSymbol unionType, CancellationToken token)
	{
		foreach (var member in OrderedMembers(unionType))
		{
			token.ThrowIfCancellationRequested();

			if (member is not IMethodSymbol { IsStatic: true, Parameters.Length: 1 } method)
				continue;

			if (method.Parameters[0].RefKind is not (RefKind.None or RefKind.In))
				continue;

			if (!SymbolEqualityComparer.Default.Equals(method.ReturnType, unionType))
				continue;

			if (!IsPartialMethodDefinition(method, token))
				continue;

			yield return method;
		}
	}

	/// <summary>
	/// Reads the case types from a nested <c>IUnionMembers</c> provider's static <c>Create</c> methods, in source order.
	/// </summary>
	static List<ITypeSymbol> GetProviderCaseTypes(INamedTypeSymbol unionType, CancellationToken token)
	{
		List<ITypeSymbol> caseTypes = [];

		var provider = unionType
			.GetTypeMembers(UnionMembersInterfaceName)
			.FirstOrDefault(static member => !member.IsImplicitlyDeclared);

		if (provider is null)
			return caseTypes;

		foreach (var member in OrderedMembers(provider))
		{
			token.ThrowIfCancellationRequested();

			if (
				member is IMethodSymbol { IsStatic: true, Parameters.Length: 1 } method
				&& method.Name == UnionMembersCreateMethodName
				&& method.Parameters[0].RefKind == RefKind.None
				&& SymbolEqualityComparer.Default.Equals(method.ReturnType, unionType)
			)
			{
				caseTypes.Add(method.Parameters[0].Type);
			}
		}

		return caseTypes;
	}

	/// <summary>
	/// Reads the case types from the union's source-visible public single-parameter constructors.
	/// </summary>
	static List<ITypeSymbol> GetConstructorCaseTypes(INamedTypeSymbol unionType, CancellationToken token)
	{
		List<ITypeSymbol> caseTypes = [];

		foreach (var constructor in unionType.InstanceConstructors)
		{
			token.ThrowIfCancellationRequested();

			if (
				constructor.DeclaredAccessibility != Accessibility.Public
				|| constructor.Parameters.Length != 1
				|| constructor.Parameters[0].RefKind is not (RefKind.None or RefKind.In)
			)
			{
				continue;
			}

			caseTypes.Add(constructor.Parameters[0].Type);
		}

		return caseTypes;
	}

	/// <summary>
	/// Normalises a case type exactly as the union generator exposes it: a nullable value type becomes its underlying
	/// type (<c>int?</c> → <c>int</c>), a nullable reference annotation is stripped, and constructed generic types and
	/// arrays are preserved as they are.
	/// </summary>
	/// <returns>
	/// The normalised case type, or <see langword="null"/> for a bare type parameter that is not one of the union's
	/// own, which the generated accessor could not name.
	/// </returns>
	static ITypeSymbol? NormalizeCaseType(ITypeSymbol caseType, INamedTypeSymbol unionType)
	{
		if (
			caseType is INamedTypeSymbol named
			&& named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
			&& named.TypeArguments.Length == 1
		)
		{
			return NormalizeCaseType(named.TypeArguments[0], unionType);
		}

		if (caseType is ITypeParameterSymbol typeParameter)
		{
			return unionType.TypeParameters.Contains(typeParameter, SymbolEqualityComparer.Default) ? caseType : null;
		}

		return caseType.NullableAnnotation == NullableAnnotation.Annotated
			? caseType.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
			: caseType;
	}

	static IEnumerable<ISymbol> OrderedMembers(INamedTypeSymbol type) =>
		type.GetMembers()
			.OrderBy(static member =>
				member.Locations.IsDefaultOrEmpty ? int.MaxValue : member.Locations[0].SourceSpan.Start
			);

	/// <summary>
	/// Determines whether a method is a <c>partial</c> signature without a body, which is what makes a factory
	/// declaration a case list rather than an implementation.
	/// </summary>
	static bool IsPartialMethodDefinition(IMethodSymbol method, CancellationToken token)
	{
		foreach (var reference in method.DeclaringSyntaxReferences)
		{
			if (
				reference.GetSyntax(token) is MethodDeclarationSyntax syntax
				&& syntax.Modifiers.Any(SyntaxKind.PartialKeyword)
				&& syntax.Body is null
				&& syntax.ExpressionBody is null
			)
			{
				return true;
			}
		}

		return false;
	}

	static bool HasAttribute(ITypeSymbol type, string attributeName, string attributeNamespace)
	{
		foreach (var attribute in type.GetAttributes())
		{
			if (IsAttribute(attribute.AttributeClass, attributeName, attributeNamespace))
				return true;
		}

		return false;
	}

	static bool IsAttribute(INamedTypeSymbol? attributeClass, string attributeName, string attributeNamespace) =>
		attributeClass is not null
		&& attributeClass.Name == attributeName
		&& attributeClass.ContainingNamespace?.ToDisplayString() == attributeNamespace;

	/// <summary>
	/// Whether the union declares the non-boxing access pattern: a <c>bool HasValue</c> property plus a
	/// <c>bool TryGetValue(out T)</c> overload for every case type. When it does, the generated accessor
	/// reads each case through <c>TryGetValue</c> and the compiler never boxes the union struct.
	/// </summary>
	static bool UsesNonBoxingAccess(ITypeSymbol unionType, int caseTypeCount)
	{
		// A union member provider declares the pattern on the nested interface the union implements.
		var provider = unionType.GetTypeMembers(UnionMembersInterfaceName).FirstOrDefault();
		var searchTargets = provider is null ? [unionType] : new ITypeSymbol[] { unionType, provider };

		var hasHasValue = false;
		var tryGetValueOverloads = 0;

		foreach (var target in searchTargets)
		{
			foreach (var member in target.GetMembers())
			{
				if (
					member is IPropertySymbol { IsStatic: false } property
					&& property.Name == HasValueMemberName
					&& property.Type.SpecialType == SpecialType.System_Boolean
				)
					hasHasValue = true;
				else if (
					member is IMethodSymbol { IsStatic: false, Parameters.Length: 1 } method
					&& method.Name == TryGetValueMemberName
					&& method.ReturnType.SpecialType == SpecialType.System_Boolean
					&& method.Parameters[0].RefKind == RefKind.Out
				)
					tryGetValueOverloads++;
			}
		}

		return hasHasValue && tryGetValueOverloads >= caseTypeCount;
	}

	/// <summary>
	/// The type a log state struct (or a <c>LoggerMessage.Define</c> generic argument) carries for a
	/// parameter. A union is logged as its case value, so the state holds <c>object?</c>; every other
	/// parameter keeps its declared type.
	/// </summary>
	public static string RenderLogStateType(LogParameterTarget parameter, bool useNullable) =>
		parameter.RequiresObjectState
			? (useNullable ? "object?" : "object")
			: parameter.ParameterType.RenderFullNameForNullable(useNullable);
}
